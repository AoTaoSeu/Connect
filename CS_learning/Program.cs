using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;//这是干哈的呀
using System.IO;

namespace CS_learning
{
    public class SimuDBCall
    {
        /*IMAGE通过数据链接库与外界进行通讯*/
        #region 通讯动态链接库：主要是IMAGE那边的，INCTRUCTOR那边用不着。
        //1.启动IMAGE与INTRUCTOR中的通讯程序*/
        //1.5 通讯初始化
        [DllImport("CommunicationDll.dll", EntryPoint = "Communicate_Init", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Communicate_Init();

        //1.6 关闭通讯
        [DllImport("CommunicationDll.dll", EntryPoint = "Communicate_End", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Communicate_End();


        //2. IMAGE与教练员站的通讯程序
        //2.1 初始化化系统，需要在读写值之前完成调用，创立监听线程相关。mode值定义当前模式，0为接收端，非0值为发送端。
        [DllImport("RCommunication.dll", EntryPoint = "StartSystem", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl)]
        public static extern int StartServer(int mode, string pathName);

        //2.2 系统运行结束，回收资源关闭网络连接
        [DllImport("RCommunication.dll", EntryPoint = "StopSystem", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl)]
        public static extern void StopServer();

        //2.3 根据IO名称来取值，成功返回0，失败返回负值，未找到点返回-2.
        [DllImport("RCommunication.dll", EntryPoint = "GetValueBool", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetValueBool(string IOName, ref bool arvale);

        //2.4 根据IO名称来取值，成功返回0，失败返回负值，未找到点返回-2.
        [DllImport("RCommunication.dll", EntryPoint = "GetValueFloat", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetValueFloat(string IOName, ref float arvale);

        //2.5 接收端：根据IO名称来写值，成功返回0，失败返回负值，未找到点返回-2 
        [DllImport("RCommunication.dll", EntryPoint = "WriteValueBool", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WriteValueBool(string IOName, bool arvale);

        //2.6 接收端：根据IO名称来写值，成功返回0，失败返回负值，未找到点返回-2
        [DllImport("RCommunication.dll", EntryPoint = "WriteValueFloat", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WriteValueFloat(string IOName, float arvale);

        //2.7 发送端：根据IO名称来写值，成功返回0，失败返回负值，未找到点返回-2
        [DllImport("RCommunication.dll", EntryPoint = "SendBool", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SendBool(string IOName, bool arvale);

        //2.8 发送端：根据IO名称来写值，成功返回0，失败返回负值，未找到点返回-2
        [DllImport("RCommunication.dll", EntryPoint = "SendFloat", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SendFloat(string IOName, float arvale);
        #endregion

    }
    /*通讯类*/
    public class ConnectTest
    {
        /*接口变量名、变量数组定义*/
        #region
        public static List<string> InsICAIs = new List<string>();
        public static float[] InsICAI;         //教练员站模拟量输入InsICAI***的数组形式 （数组维数必须 >= InsICDI***变量个数）

        public static List<string> InsICDIs = new List<string>();
        public static bool[] InsICDI;         //教练员站逻辑量输入InsICDI***的数组形式 （数组维数必须 >= InsICDI***变量个数）

        public static List<string> InsCmdAOs = new List<string>();
        public static float[] InsCmdAO;    //教练员站模拟量指令InsCmdAO***的数组形式 （数组维数必须 >= InsCmdAO***变量个数）

        public static List<string> InsCmdDOs = new List<string>();
        public static bool[] InsCmdDO;    //教练员站逻辑量指令InsCmdDO***的数组形式 （数组维数必须 >= InsCmdDO***变量个数）

        /*定义进行交换的数据变量*///TODO：后面进行数据结构更改的时候是要用的
        //
        public static float[] ImageToInsData;
        public static float[] InsToImageData;
        //



        public static string InsCmdSO001 = "选择的模型名";            //选择的模型名     
        public static bool replaying = false;   //是否正在工况重演的状态字（长信号）     由教练员站赋值    true:工况重演状态；  false：正常运行状态
        public static bool[] InsCmdDOLast;  //InsCmdDO的上一步值 （在IMAGE中使用，用于实现短信号的响应）
        public static List<string> startError = new List<string>();  //接口通讯启动过程出错信息记录（用于在IMAGE、教练员站的信息窗口显示）
                                          
        #endregion
        /*
        public Dictionary<string, float> Data = new Dictionary<string, float>();
        public int C = 0;
        public void  Me(int num)
        {
            for (int i = 0; i < num; i++)
            {
                Data[i.ToString()] = i;
            }
        }
        //可以直接将数据写入到当前对象的数据的结构中，
        public void Getvalue(string name, float value)
        {
            //直接将数据写入到相应的位置
            Data[name] = value;
        }
        */
        public static bool Start(string IOfilepath, string IOfullname, bool forInstructor)   //启动时，接口通讯程序需要做的事情
        {
            //string IOfilepath：初始化时读入的接口点表文件路径           （用于csv格式的多个接口点表文件）
            //string IOfullname：初始化时读入的接口点表带路径文件名   （用于excel格式的1个接口点表文件）
            //IOfilepath、IOfullname两者只有一个被使用（既定义IOfilepath又定义IOfullname，只是为了兼容性）

            //bool forInstructor： 切换控制字（true：适用于教练员站软件；   false：适用于模型软件 IMAGE ）

            //1. 读入接口点表、数组初始化
            Init(IOfilepath);
            //2. 通讯程序初始化
            if (SimuDBCall.Communicate_Init() != 0)
            {
                return false;
            }
            if (SimuDBCall.StartServer(0, IOfilepath) != 0)//TODO:这个地方是IMAGE中的接口，不知道在什么地方能遇到。也不知道有什么用
            {
                    return false;
            }
            return true;
        }
        private static void Init(string IOfilepath)
        {
            //string IOfilepath：初始化时读入的接口点表文件路径           （用于csv格式的多个接口点表文件）

            //1. 读取接口点表文件（获取接口变量名，文件格式是csv）
            ReadIOdata(IOfilepath);

            //2. 确定接口变量值数组维数
            InsICAI = new float[InsICAIs.Count];
            ////发生更改
            ImageToInsData = new float[InsICAIs.Count];//TODO:
            InsToImageData = new float[InsCmdAOs.Count];//TODO:
            ////
            //InsICDI = new bool[InsICDIs.Count];
            InsCmdAO = new float[InsCmdAOs.Count];
            //InsCmdDO = new bool[InsCmdDOs.Count];
            //InsCmdDOLast = new bool[InsCmdDO.Length];     //与InsCmdDO维数相同

        }
        private static void ReadIOdata(string filePath)        //读取接口点表文件（文件格式是csv）
        {
            //filePath : .cvs格式的接口点表文件的绝对路径 （不包括文件名）

            InsICAIs.Clear();
            InsICDIs.Clear();
            InsCmdAOs.Clear();
            InsCmdDOs.Clear();

            List<string[]> listStr = new List<string[]>();        //临时变量 记录读入的数据

            DirectoryInfo folder0 = new DirectoryInfo(filePath);
            foreach (FileInfo file in folder0.GetFiles("*.csv"))   //路径为filePath的文件夹中的全部.csv文件 （逐个文件读入）
            {
                //.csv格式的IO点表文件名
                string fileShortName = Path.GetFileNameWithoutExtension(file.FullName);

                //1. 读取文件file.FullName中的内容
                StreamReader reader = new StreamReader(file.FullName, System.Text.Encoding.Default);   //读取文件file.FullName中的内容
                string line = "";

                //2. 读入的数据记录到动态数组listStr
                line = reader.ReadLine();                       //csv格式点表文件第一行是各列的标题（名称），不是数据
                listStr.Clear();                                       //初始化
                while (true)
                {
                    line = reader.ReadLine();                  //获取从第二行开始的各行数据    
                    if ((line != null) && (line.Split(',').Length > 1))       //如果该行有数据（没有超出最后一行），并且不仅仅是只有第一列序号
                    {
                        listStr.Add(line.Split(','));              //读入的数据记录到动态数组listStr
                    }
                    else
                    {
                        break;   //到了结尾，  退出while (true)
                    }
                }

                //3. 把动态数组listStr的数据，赋给各接口变量
                string IOname = " ";                //专门用于“IO点变量名”的临时变量  
                string IOtype = " ";                 //用于读入接口变量类型（AI,AO,DI,DO）
                bool IOtypeOK = false;           //类型检查的临时变量
                for (int i = 0; i < listStr.Count; i++)
                {
                    //3.1 获取第二列【“接口变量名（I/O编号）”】的数据  （第一列 (listStr[i])[0]是序号）
                    if (listStr[i].Length > 1)   //防止越界
                    {
                        IOname = (listStr[i])[1].Trim();    //字符串整理：删除字符串开头以及尾巴可能存在的空格
                        if (IOname.Length == 0)
                        {
                            startError.Add("文件 " + fileShortName + "    行 " + i.ToString() + "    没有变量名");  //接口通讯启动过程出错信息记录
                            break;                                      //没有接口变量名，退出 for (int i=0; i<listStr.Count; i++) 赋值
                        }
                    }
                    else
                    {
                        startError.Add("文件 " + fileShortName + "    行 " + i.ToString() + "    没有变量名");
                        break;
                    }

                    //3.2 读入第五列【接口“类型”】的数据（AI, AO, DI, DO）   （第一列是序号）
                    if (listStr[i].Length > 4)   //防止越界
                    {
                        IOtype = (listStr[i])[4].Trim();            //读入接口类型（删除字符串开头以及尾巴可能存在的空格）
                        IOtypeOK = (IOtype == "AI") || (IOtype == "DI") || (IOtype == "AO") || (IOtype == "DO") || (IOtype == "SO"); //SO：模型名
                        if (!IOtypeOK)
                        {
                            startError.Add("文件 " + fileShortName + "    行 " + i.ToString() + "    变量类型错误");
                            break;                                           //没有接口类型，退出 for (int i=0; i<listStr.Count; i++) 赋值
                        }
                    }
                    else
                    {
                        startError.Add("文件 " + fileShortName + "    行 " + i.ToString() + "    没有变量类型");
                        break;
                    }

                    //4. 记录IO接口变量到各个动态数组之中
                    if (fileShortName.Contains("仿真机指令与响应")) //4.1 该文件名是“仿真机指令与响应”
                    {
                        if (IOtype == "AI")  //如果指令是模拟量输入（指令的响应，目前没有AI，此处添加只是以备后用）  
                        {
                            InsICAIs.Add(IOname);
                        }
                        else if (IOtype == "DI")  //如果指令是逻辑量输入（指令的响应） 
                        {
                            InsICDIs.Add(IOname);
                        }
                        else if (IOtype == "AO")  //如果指令是模拟量输出  
                        {
                            InsCmdAOs.Add(IOname);
                        }
                        else if (IOtype == "DO")   //如果指令是逻辑量输出
                        {
                            InsCmdDOs.Add(IOname);
                        }
                    }
                }
                reader.Close();    //释放资源
            }
        }
        public static void Stop(bool forInstructor)                    //主程序关闭时，接口通讯程序需要做的事情
        {
            //bool forInstructor： 切换控制字（true：放置在教练员站软件 Instructor 中调用；   false：放置在模型软件 IMAGE 中调用）

            if (forInstructor)//放置在教练员站软件中调用的内容
            {
                SimuDBCall.StopServer();                    //2. 模型关闭时，关闭 “ IMAGR与教练员站 ” 接口通讯程序  
            }
            else //放置在模型软件中调用的内容
            {
                SimuDBCall.Communicate_End();         //1. 模型关闭时，关闭 “ IMAGR与DCS操作员站 ” 接口通讯程序   
                SimuDBCall.StopServer();                    //2. 模型关闭时，关闭 “ IMAGR与教练员站 ” 接口通讯程序
            }
        }
        public static void GetValues(bool forInstructor)            //接口变量输入（正常定时调用时，接口通讯程序需要做的事情）
        { //功能：把外面其它程序的接口变量值，传输给对上面定义的某几类接口数组。

            //bool forInstructor： 切换控制字（true：放在教练员站软件 Instructor 中调用；   false：放在模型软件 IMAGE 中调用）

            if (forInstructor) //放置在教练员站软件中调用的内容
            {
                //1. 从模型IAMGE获取变量值
                ////需要更改,暂时只是写了一个。之后在进行增加
                ImageToInsData = InsICAI;//TODO:
                //GetValue(InsICAIs, InsICAI, false, true);   //TODO:  
                //GetValue(InsICDIs, InsICDI, false, true);

            }
            else //放置在模型软件中调用的内容
            {
                //1. 从教练员站获取变量值
                ////不需要更改
                GetValue(InsCmdAOs, InsCmdAO, false, false);
            }
        }
        private static void GetValue(List<string> IOnames, float[] IOvalues, bool connectDCS, bool forInstructor) //根据变量名IOnames，取值 float[] IOvalues
        {
            //bool connectDCS：   true：IMAGE（教练员站）与DCS通讯；      false：IMAGE与教练员站之间通讯
            //bool forInstructor： 切换控制字（true：放在教练员站软件 Instructor 中调用；   false：放在模型软件 IMAGE 中调用）
            // TODO：这个地方是直接将数据传到matlab

            for (int i = 0; i < IOnames.Count; i++)
            {
                if (forInstructor)  //1. 在教练员站调用
                {
                    //1.2 与IMAGE通讯
                    ////将IMAGE中的数据传入到INS
                    SimuDBCall.GetValueFloat(IOnames[i], ref IOvalues[i]);
                }
                else //2. 在IMAGE调用
                {
                    //2.2 与教练员站通讯
                    SimuDBCall.GetValueFloat(IOnames[i], ref IOvalues[i]);
                }
            }
        }
        public static void SetValues(bool forInstructor)            //接口变量输出（正常定时调用时，接口通讯程序需要做的事情）
        { //内容：把上面定义的某几类接口数组值，传给外面其它程序的接口变量。

            //bool forInstructor： 切换控制字（true：放在教练员站软件 Instructor 中调用；   false：放在模型软件 IMAGE 中调用）

            if (forInstructor) //放置在教练员站软件中调用的内容
            {
                //1. 教练员站指令发送给DCS 

                //2. 教练员站指令发送给模型
                InsCmdAO = InsToImageData;
                //SetValue(InsCmdAOs, InsCmdAO, false, true);
                //SetValue(InsCmdDOs, InsCmdDO, false, true);

            }
            else //放置在模型软件中调用的内容
            {


                //2. 模型把状态量发送给教练员站
                ////不需要更改
                SetValue(InsICAIs, InsICAI, false, false);//TODO:
                /////
                //SetValue(InsICDIs, InsICDI, false, false);//UNDONE:
            }
        }
        private static void SetValue(List<string> IOnames, float[] IOvalues, bool connectDCS, bool forInstructor)  //根据变量名IOnames，写值 float[] IOvalues
        {
            //bool connectDCS：   true：IMAGE（教练员站）与DCS通讯；      false：IMAGE与教练员站之间通讯
            //bool forInstructor： 切换控制字（true：放在教练员站软件 Instructor 中调用；   false：放在模型软件 IMAGE 中调用）
            //TODO: names 的来源，values的来源
            for (int i = 0; i < IOnames.Count; i++)
            {
                if (forInstructor)  //1. 在教练员站调用
                {                  //1.2 与IMAGE通讯
                    //SimuDBCall.SendFloat(IOnames[i], IOvalues[i]);//TODO：写入数据，但是要注意数据的格式
                }
                else //2. 在IMAGE调用
                {                  //2.2 与教练员站通讯
                    SimuDBCall.WriteValueFloat(IOnames[i], IOvalues[i]);
                }
            }
        }

    }
}
