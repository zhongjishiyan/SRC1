using System;
using System.Collections.Generic;
using System.Text;
using Doli.DoSANet;
using System.IO;

namespace PipesClientTest
{
    public class modMain
    {
        //public static int SysNum = 3;
        public static DoSA[] MeDoSA;
        public static DoSAall MeDoSAall;
        public static bool[] blnStartTest;
        public static bool[] blnPipeConnectOK;//pipe连接正常
        public static int[] intPipeErrorNum;//pipe连接没有接收数据的次数        

        public const int MAX_SENSORS = 16;
        public const int DoSAVersionEDC220 = 9;
        public const int DoSAVersionEDCi15 = 10;
        public const int ConnectToEdcAll = 10000;
        public const int ConnectToEdcFuncID = 10001;
        public const int CloseLink_FuncID = 10002;
        public const int CloseAll = 10003;
        public const int CloseLink_DoSAHdl = 10004;
        public const int TestPara_Name = 10005;

        //数据文件
        public static int[] EDC_STATE;//控制器联机状态
        public static float[] USED_BUFFER;  //缓冲区
        public static int[] CMD_BIT;
        public static long[] LineNumber;

        public static string[] ParaFile;     //参数文件名
        //参数文件名命名规则，蠕变后缀名为  .XXX_Creep_Para，其中XXX为设备号码，不足3位前面用0占位，例如1号设备为001；
        //1号设备蠕变参数文件后缀名为 .001_Creep_Para；持久参数后缀名为 .001_Ruptu_Para；；松弛参数后缀名为 .001_Relax_Para；
        public static string[] DataFile;     //数据文件名
        //1号设备蠕变数据文件后缀名为 .001_Creep_Data；持久参数后缀名为 .001_Ruptu_Data；；松弛参数后缀名为 .001_Relax_Data；
        public static string[] RecoFile;     //记录文件名
        public static string[] TempFile;     //
        ///试验参数变量
        ///48段试验段数，实际为12段，预留36段；单位参考public num CtlUnit
        public static int[,] PARA_CTRLn;   //n段的控制对象，1=负荷；0=位移；2=变形。
        public static int[,] PARA_MODEn;   //n段的控制模式，0=斜波；1=保持；2=余弦波；3=三角波；4=方波。

        public static float[,] PARA_Vn;  //n段的加载速率
        public static int[,] PARA_Vn_Unit;           //试验预负荷控制速度单位；
        public static float[,] PARA_Pn;       //n段的目标值；
        public static int[,] PARA_Pn_Unit;           //试验预负荷控制负荷值的单位；
        public static float[,] PARA_Tn;   //n段的保持时间
        public static int[,] PARA_Tn_Unit;           //试验预负荷保持时间的单位；

        public static float[,] PARA_OFFn; //n段的动态偏移量值
        public static int[,] PARA_OFFn_Unit; //n段的动态偏移量单位；
        public static float[,] PARA_AMPLn;  //n段的振幅
        public static int[,] PARA_AMPLn_Unit;  //n段的振幅单位
        public static float[,] PARA_FREQn;   //n段的频率；单位固定为Hz
        public static long[,] PARA_CYCLEn;   //n段的波形次数

        public static string strFree = "0"; //占位变量，留着以后专机用

        //12段的试验温度
        public static float[,] CREEP_TEMP;       //12段的试验温度目标值
        public static float[,] CREEP_TEMP_RAMP;       //12段的温度升温速度
        public static float[,] CREEP_TEMP_WAIT;       //12段的温度保温时间
        public static float[,] CREEP_TEMP_DELTA;       //12段的温度波动度
        public static float[,] CREEP_TEMP_END;       //12段的试验结束温度
        //5段保存数据条件设定
        public static int[] SaveTimeStep;    //设置保存数据条件的段数
        public static string[,] SaveTimeFrom;   //5段保存数据起始时间
        public static string[,] SaveTimeTo;     //5段保存数据结束时间
        public static int[,] SaveTimeFromToUnit;  //5段保存数据时间单位；0=秒；1=分钟；2=小时。
        public static string[,] SaveTimeInv;        //5段保存数据间隔时间
        public static int[,] SaveTimeInvUnit;     //5段保存数据间隔时间单位；0=秒；1=分钟；2=小时。

        public static int[] TestType;           //试验类型；0=持久；1=蠕变；2=松弛；
        public static float[] TestLoadRange;     //负荷传感器量程
        public static float[] TestDefRange;      //变形传感器量程
        public static string[] TestStandard;     //试验标准名称
        public static float[] TestYingLi;        //试验应力，单位是MPa
        public static float[] TestTotalLoad;        //试验总负荷
        public static float[] TestMainLoad;        //试验主负荷
        public static int[] PARA_CREEP_STEPS;           //试验加载段数；最多可设置12段
        public static double[] PARA_CREEP_TEST_TIME;        //试验总时间，单位为小时；
        public static long[] PARA_LOOPS;        //试验12段的循环次数

        //预负荷；单位参考public num CtlUnit
        public static int[] PARA_IS_CTRL0;           //试验是否有预负荷；0=无预负荷；1=有预负荷；
        public static int[] PARA_CTRL0;           //试验预负荷控制方式；固定为0=位移；
        public static int[] PARA_MODE0;           //试验预负荷控制模式；固定为0=斜波；
        public static float[] PARA_V0;           //试验预负荷控制速度值；
        public static int[] PARA_V0_Unit;           //试验预负荷控制速度单位；
        public static float[] PARA_P0;           //试验预负荷控制负荷值；
        public static int[] PARA_P0_Unit;           //试验预负荷控制负荷值的单位；固定为0=N；
        public static float[] PARA_T0;           //试验预负荷保持时间值；
        public static int[] PARA_T0_Unit;           //试验预负荷保持时间的单位；Second = 0,        min = 1,

        public static float[] CREEP_EXTENSION_LIMIT;           //变形报警极限；单位固定为mm
        public static float[] CREEP_REF_TIME;           //动态校准时间；单位固定为min
        //试验总时间到达后的动作，固定为位移控制
        public static int[] RETURN_ACTION;           //返回动作；参考public enum Return_Action
        public static float[] VRETURN_ACTION;           //返回的速度；
        public static int[] VRETURN_ACTION_Unit;           //返回的速度的单位；单位参考public num CtlUnit
        //'EDC数据采集条件
        public static float[] PRINT_TIME;         //时间间隔；单位固定为秒
        public static float[] PRINT_DS;           //位移步长；单位固定为mm
        public static float[] PRINT_DF;           //负荷步长；单位固定为N
        public static float[] PRINT_DE;           //变形步长；单位固定为mm

        //试验停止条件及操作
        public static int[] END_SENSOR;           //检测的传感器；1=负荷；0=位移；2=变形。
        public static int[] END_MODE;           //检测的模式；1=小于（低于控制量）；0=大于（高于控制量）；
        public static float[] END_VALUE;           //检测的值；
        public static int[] END_VALUE_Unit;           //检测值的单位；单位参考public num CtlUnit
        public static int[] END_ACTION;           //停止后的动作；参考public enum Return_Action
        public static float[] VEND_ACTION;           //停止后返回的速度；
        public static int[] VEND_ACTION_Unit;           //停止后返回的速度的单位；单位参考public num CtlUnit

        //极限保护(保护上限和保护下限)
        public static int[] LIMIT_SENSOR;           //检测的传感器；1=负荷；0=位移；2=变形。
        public static float[] LIMIT_PP;           //检测的上限值；
        public static int[] LIMIT_PP_Unit;           //检测的上限值的单位；单位参考public num CtlUnit
        public static float[] LIMIT_MM;           //检测的下限值；
        public static int[] LIMIT_MM_Unit;           //检测的下限值的单位；单位参考public num CtlUnit
        public static int[] LIMIT_ACTION;           //停止后的动作；参考public enum Return_Action
        public static float[] VLIMIT_ACTION;           //停止后返回的速度；
        public static int[] VLIMIT_ACTION_Unit;           //停止后返回的速度的单位；单位参考public num CtlUnit

        //软件监测连续多少个负荷波形的峰值小于设定值，停止试验。
        public static int[] SOFT_SENSOR;           //检测的传感器；固定=1=负荷；255=不监测；
        public static int[] SOFT_MODE;           //检测的模式；固定=1；   1=小于；
        public static float[] SOFT_VALUE;           //检测的值；
        public static int[] SOFT_VALUE_Unit;           //检测值的单位；负荷单位参考public num CtlUnit
        public static int[] SOFT_ACTION;           //停止后的动作；参考public enum Return_Action
        public static float[] VSOFT_ACTION;           //停止后返回的速度；
        public static int[] VSOFT_ACTION_Unit;           //停止后返回的速度的单位；单位参考public num CtlUnit
        public static long[] SOFT_CycleNum;           //连续波形的次数
        
        //软件监测温度波动大于设定值，停止试验。
        public static int[] SOFT_TEMP_SENSOR;           //检测的传感器；固定=1=温度传感器；0=不监测；
        public static int[] SOFT_TEMP_MODE;           //检测的模式；固定=0；   0=大于；
        public static float[] SOFT_TEMP_VALUE;           //检测的值；
        public static int[] SOFT_TEMP_VALUE_Unit;           //检测值的单位；固定为摄氏度=0；
        public static int[] SOFT_TEMP_ACTION;           //停止后的动作；参考public enum Return_Action
        public static float[] SOFT_VTEMP_ACTION;           //停止后返回的速度；
        public static int[] SOFT_VTEMP_ACTION_Unit;           //停止后返回的速度的单位；单位参考public num CtlUnit

        //软件监测采集值是否超差，界面上显示数据的控件或者单元格背景红色报警但是不停止试验。
        public static int[] SOFT_IS_WARN;           //软件是否启动报警功能；0=不启动；1=启动；
        public static float[] SOFT_Load_WARN;           //控制负荷波动百分比，单位是百分号；
        public static float[] SOFT_Position_WARN;           //控制位移波动值，单位mm；
        public static float[] SOFT_Extension_WARN;          //控制变形波动值，单位是mm；
        public static float[] SOFT_TEMP_Fluct_WARN;          //控制温度波动值，单位是摄氏度；
        public static float[] SOFT_TEMP_Grad_WARN;          //温度梯度值，单位是摄氏度；

        public static int[] KeepTest;           //启动试验的方式；0=新启动试验（如果数据文件有数据则清空数据）；1=恢复试验；
        public static int[] SaveMode;           //数据存盘的方式；0=按照5段时间设置存盘；1=全部存储；2=每隔设定的波形存储一个完整波形；2=每隔设定的循环存储一个完整循环
        public static long[] InvCycle;           //间隔的波形次数；
        public static long[] InvLoop;           //间隔的循环次数；

        public static int[] Sample_Shape;           //试样形状；0=圆形棒材；1=矩形板材；2=弧形；3=组合；
        public static float[] Sample_Diameter;           //圆形试样的直径
        public static int[] Sample_Diameter_Unit;           //圆形试样的直径的单位；0=mm；1=英寸inch
        public static float[] Sample_Width;           //矩形试样的宽度
        public static int[] Sample_Width_Unit;           //矩形试样的宽度的单位；0=mm；1=英寸inch
        public static float[] Sample_Thickness;           //矩形试样的厚度
        public static int[] Sample_Thickness_Unit;           //矩形试样的厚度的单位；0=mm；1=英寸inch
        public static float[] Sample_Gauge_Length;           //试样的标距



        public static void initValue(int machineNum)
        {
            MeDoSAall = new DoSAall();
            MeDoSA = new DoSA[machineNum];
            blnStartTest = new bool[machineNum];
            blnPipeConnectOK = new bool[machineNum];
            intPipeErrorNum = new int[machineNum];
            
            EDC_STATE = new int[machineNum];
            USED_BUFFER = new float[machineNum];
            CMD_BIT = new int[machineNum];
            LineNumber = new long[machineNum];
            
            ParaFile = new string[machineNum];
            DataFile = new string[machineNum];
            RecoFile = new string[machineNum];
            TempFile = new string[machineNum];
            ///48段试验段数，实际为12段，预留36段
            PARA_CTRLn = new int[machineNum,48];   //12段的控制对象，1=负荷；0=位移；2=变形。
            PARA_MODEn = new int[machineNum, 48];   //12段的控制模式，0=斜波；1=保持；2=余弦波；3=三角波；4=方波。

            PARA_Pn = new float[machineNum, 48];       //12段的目标值
            PARA_Pn_Unit = new int[machineNum, 48];
            PARA_Tn = new float[machineNum, 48];   //12段的保持时间
            PARA_Tn_Unit = new int[machineNum, 48];
            PARA_Vn = new float[machineNum, 48];  //12段的加载速率
            PARA_Vn_Unit = new int[machineNum, 48];

            PARA_OFFn = new float[machineNum, 48]; //12段的动态偏移量
            PARA_OFFn_Unit = new int[machineNum, 48];
            PARA_AMPLn = new float[machineNum, 48];  //12段的振幅
            PARA_AMPLn_Unit = new int[machineNum, 48];
            PARA_FREQn = new float[machineNum, 48];   //12段的频率
            PARA_CYCLEn = new long[machineNum, 48];   //12段的波形次数

            //12段的试验温度
            CREEP_TEMP = new float[machineNum, 12];       //12段的试验温度目标值
            CREEP_TEMP_RAMP = new float[machineNum, 12];       //12段的温度升温速度
            CREEP_TEMP_WAIT = new float[machineNum, 12];       //12段的温度保温时间
            CREEP_TEMP_DELTA = new float[machineNum, 12];       //12段的温度波动度
            CREEP_TEMP_END = new float[machineNum, 12];       //12段的试验结束温度

            //5段保存数据条件设定
            SaveTimeStep = new int[machineNum];    //设置保存数据条件的段数
            SaveTimeFrom = new string[machineNum, 5];   //5段保存数据起始时间
            SaveTimeTo = new string[machineNum, 5];     //5段保存数据结束时间
            SaveTimeFromToUnit = new int[machineNum, 5];  //5段保存数据时间单位；0=秒；1=分钟；2=小时。
            SaveTimeInv = new string[machineNum, 5];        //5段保存数据间隔时间
            SaveTimeInvUnit = new int[machineNum, 5];     //5段保存数据间隔时间单位；0=秒；1=分钟；2=小时。

            TestType = new int[machineNum];
            TestLoadRange = new float[machineNum];
            TestDefRange = new float[machineNum];
            TestStandard = new string[machineNum];
            TestYingLi = new float[machineNum];
            TestTotalLoad = new float[machineNum];
            TestMainLoad = new float[machineNum];
            PARA_CREEP_STEPS = new int[machineNum];
            PARA_CREEP_TEST_TIME = new double[machineNum];
            PARA_LOOPS = new long[machineNum];

            PARA_IS_CTRL0 = new int[machineNum];
            PARA_CTRL0 = new int[machineNum];
            PARA_MODE0 = new int[machineNum];
            PARA_V0 = new float[machineNum];
            PARA_V0_Unit = new int[machineNum];
            PARA_P0 = new float[machineNum];
            PARA_P0_Unit = new int[machineNum];
            PARA_T0 = new float[machineNum];
            PARA_T0_Unit = new int[machineNum];

            CREEP_EXTENSION_LIMIT = new float[machineNum];
            CREEP_REF_TIME = new float[machineNum];
            RETURN_ACTION = new int[machineNum];
            VRETURN_ACTION = new float[machineNum];
            VRETURN_ACTION_Unit = new int[machineNum];
            PRINT_TIME = new float[machineNum];
            PRINT_DS = new float[machineNum];
            PRINT_DF = new float[machineNum];
            PRINT_DE = new float[machineNum];
           
            END_SENSOR = new int[machineNum];
            END_MODE = new int[machineNum];
            END_VALUE = new float[machineNum];
            END_VALUE_Unit = new int[machineNum];
            END_ACTION = new int[machineNum];
            VEND_ACTION = new float[machineNum];
            VEND_ACTION_Unit = new int[machineNum];
           
            LIMIT_SENSOR = new int[machineNum];
            LIMIT_PP = new float[machineNum];
            LIMIT_PP_Unit = new int[machineNum];
            LIMIT_MM = new float[machineNum];
            LIMIT_MM_Unit = new int[machineNum];
            LIMIT_ACTION = new int[machineNum];
            VLIMIT_ACTION = new float[machineNum];
            VLIMIT_ACTION_Unit = new int[machineNum];
            
            SOFT_SENSOR = new int[machineNum];
            SOFT_MODE = new int[machineNum];
            SOFT_VALUE = new float[machineNum];
            SOFT_VALUE_Unit = new int[machineNum];
            SOFT_ACTION = new int[machineNum];
            VSOFT_ACTION = new float[machineNum];
            VSOFT_ACTION_Unit = new int[machineNum];
            SOFT_CycleNum = new long[machineNum];
            
            SOFT_TEMP_SENSOR = new int[machineNum];
            SOFT_TEMP_MODE = new int[machineNum];
            SOFT_TEMP_VALUE = new float[machineNum];
            SOFT_TEMP_VALUE_Unit = new int[machineNum];
            SOFT_TEMP_ACTION = new int[machineNum];
            SOFT_VTEMP_ACTION = new float[machineNum];
            SOFT_VTEMP_ACTION_Unit = new int[machineNum];
            
            SOFT_IS_WARN = new int[machineNum];
            SOFT_Load_WARN = new float[machineNum];
            SOFT_Position_WARN = new float[machineNum];
            SOFT_Extension_WARN = new float[machineNum];
            SOFT_TEMP_Fluct_WARN = new float[machineNum];
            SOFT_TEMP_Grad_WARN = new float[machineNum];
            
            KeepTest = new int[machineNum];
            SaveMode = new int[machineNum];
            InvCycle = new long[machineNum];
            InvLoop = new long[machineNum];

            Sample_Shape = new int[machineNum];
            Sample_Diameter = new float[machineNum];
            Sample_Diameter_Unit = new int[machineNum];
            Sample_Width = new float[machineNum];
            Sample_Width_Unit = new int[machineNum];
            Sample_Thickness = new float[machineNum];
            Sample_Thickness_Unit = new int[machineNum];
            Sample_Gauge_Length = new float[machineNum];


            for (int sysNo = 0; sysNo < machineNum; sysNo++)
            {
                MeDoSA[sysNo]= new DoSA();
                blnStartTest[sysNo] = false;
                blnPipeConnectOK[sysNo] = false;
                intPipeErrorNum[sysNo] = 0;
                EDC_STATE[sysNo] = 0;
                USED_BUFFER[sysNo] = 0;
                CMD_BIT[sysNo] = 0;               
                LineNumber[sysNo] = 0;
            }
        }

        public static void WriteLog(string strLog)
        {
            //string sFilePath = "d:\\" + DateTime.Now.ToString("yyyyMM");
            string sFilePath = "d:" ;
            string sFileName =  "rizhi" + DateTime.Now.ToString("yyyyMMdd") + ".log";
            sFileName = sFilePath + "\\" + sFileName; //文件的绝对路径
            if (!Directory.Exists(sFilePath))    //验证路径是否存在
            {
                Directory.CreateDirectory(sFilePath);
                //不存在则创建
            }
            FileStream fs;
            StreamWriter sw;
            if (File.Exists(sFileName))
            //验证文件是否存在，有则追加，无则创建
            {
                fs = new FileStream(sFileName, FileMode.Append, FileAccess.Write);
            }
            else
            {
                fs = new FileStream(sFileName, FileMode.Create, FileAccess.Write);
            }
            sw = new StreamWriter(fs);
            //sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + "   ---   " + strLog);
            sw.WriteLine(strLog);
            sw.Close();
            fs.Close();
        }

        public static void WriteDataFile(string sFileName,string strLog)
        {
            //string sFilePath = "d:\\" + DateTime.Now.ToString("yyyyMM");
            string sFilePath = "d:";
            //string sFileName = "rizhi" + DateTime.Now.ToString("yyyyMMdd") + ".log";
            //sFileName = sFilePath + "\\" + sFileName; //文件的绝对路径
            if (!Directory.Exists(sFilePath))    //验证路径是否存在
            {
                Directory.CreateDirectory(sFilePath);
                //不存在则创建
            }
            FileStream fs;
            StreamWriter sw;
            if (File.Exists(sFileName))
            //验证文件是否存在，有则追加，无则创建
            {
                fs = new FileStream(sFileName, FileMode.Append, FileAccess.Write);
            }
            else
            {
                fs = new FileStream(sFileName, FileMode.Create, FileAccess.Write);
            }
            sw = new StreamWriter(fs);
            //sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "   ---   " + strLog);
            sw.WriteLine(strLog);
            sw.Close();
            fs.Close();
        }

        public enum CtlerType
        {
            SIM = 0,
            EDCi15 = 1,
            EDC220 = 2,
            TMC = 3,         
        }

        public enum SENSOR
        {
            SENSOR_S = 0,
            SENSOR_F = 1,
            SENSOR_E = 2,
            SENSOR_D = 3,
            SENSOR_4 = 4,
            SENSOR_5 = 5,
            SENSOR_6 = 6,
            SENSOR_7 = 7,
            SENSOR_8 = 8,
            SENSOR_9 = 9,
            SENSOR_10 = 10,
            SENSOR_11 = 11,
            SENSOR_12 = 12,
            SENSOR_13 = 13,
            SENSOR_14 = 14,
            SENSOR_15 = 15,
            SENSOR_No = 255,
        }

        public enum CtlMode
        {
            Ramp = 0,
            Halt = 1,
            Cosine = 2,
            Triangle = 3,
            Rectangle = 4,            
        }

        public enum CtlUnit//控制量的单位
        {
            No_Unit = -1,//无单位

            mm = 0,
            um = 1,

            mm_min = 0,
            um_min=1,
            mm_s=2,
            um_s = 3,
            mm_h=4,
            um_h=5,

            N = 0,

            N_S=0,

            Second = 0,
            min = 1,
            
            Hz=0,

            C=0,
            C_min=1,
        }

        public enum cmdType
        {
            Write = 0,
            Read = 1,
        }

        public enum Return_Action
        {
            s_halt=0,//停于位置
            return_le = 1,//初始状态
            return_p0=2,//预负荷
            f_halt=3,//停于负荷
            e_halt=4,//停于变形
            drive_off=5,//关断驱动
        }

        public enum EndMode
        {
            above=0,
            below=1,
            delta_of_max=2,
            rate=3,
        }
        
    }
}
