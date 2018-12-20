using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Pipes;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;

namespace PipesClientTest
{
    public sealed class GlobeVal
    {
        public static ConfigFile myconfigfile;
    }

    [Serializable]
    public class ConfigFile
    {
        public int machinecount;
        public int mode;

        public int[] SimulationMode;

        public ConfigFile()
        {
            SimulationMode = new int[100];
        }

        public void SerializeNow(string filename)
        {

            FileStream fileStream =
            new FileStream(filename, FileMode.Create);
            BinaryFormatter b = new BinaryFormatter();
            b.Serialize(fileStream, this);

            fileStream.Close();
        }

        public ConfigFile DeSerializeNow(string filename)
        {
            ConfigFile c = new PipesClientTest.ConfigFile ();
            try
            {


                using (FileStream fileStream =
                 new FileStream(filename,
                 FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    BinaryFormatter b = new BinaryFormatter();

                    c = b.Deserialize(fileStream) as ConfigFile;

                
                    fileStream.Close();



                   }
            }
            catch (Exception e1)
            {
                c = new PipesClientTest.ConfigFile();

                 MessageBox.Show(e1.Message, "读取文件");
            }
            finally
            {

            }
            return (c);
         }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TransferCmd
    {
        public int FuncID;//Variable0
        public int cmdName;//Variable1
        public int cmdRead;//Variable2
        public double cmdValue;//Variable3
        public int cmdUnit;//Variable4

        public string strPara_FileName;//Variable5参数文件名
        public float Free6;//Variable6
        public float Free7;//Variable7
        public float Free8;//Variable8
        public Int64 Free9;//Variable9
        public Int64 Free10;//Variable10
        public Int64 Free11;//Variable11
        public string Free12;//Variable12
        public string Free13;//Variable13
        public string Free14;//Variable14
        public float Free15;//Variable15
        public float Free16;//Variable16
        public float Free17;//Variable17
        public float Free18;//Variable18

        public ulong tcount;//Variable19
    }

   
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TransferData
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100, ArraySubType = UnmanagedType.R8)]
        public double[] TOTAL_TIME;//EXT_CMD_PARA_CREEP_PRINT_V00  启动试验的总时间
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100, ArraySubType = UnmanagedType.R8)]
        public double[] TEST_TIME;//EXT_CMD_PARA_CREEP_PRINT_V01   加载开始的试验时间
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100, ArraySubType = UnmanagedType.R4)]
        public float[] CHANNEL_S;//EXT_CMD_PARA_CREEP_PRINT_V02  位移
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100, ArraySubType = UnmanagedType.R4)]
        public float[] CHANNEL_F;//EXT_CMD_PARA_CREEP_PRINT_V03  负荷        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100, ArraySubType = UnmanagedType.R4)]
        public float[] CHANNEL_E;//EXT_CMD_PARA_CREEP_PRINT_V04  平均变形
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100, ArraySubType = UnmanagedType.R4)]
        public float[] CHANNEL_4;//EXT_CMD_PARA_CREEP_PRINT_V05  变形1
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100, ArraySubType = UnmanagedType.R4)]
        public float[] CHANNEL_5;//EXT_CMD_PARA_CREEP_PRINT_V06  变形2
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100, ArraySubType = UnmanagedType.R4)]
        public float[] CHANNEL_7;//EXT_CMD_PARA_CREEP_PRINT_V07  温度上
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100, ArraySubType = UnmanagedType.R4)]
        public float[] CHANNEL_8;//EXT_CMD_PARA_CREEP_PRINT_V08  温度中
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100, ArraySubType = UnmanagedType.R4)]
        public float[] CHANNEL_9;//EXT_CMD_PARA_CREEP_PRINT_V09  温度下
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100, ArraySubType = UnmanagedType.I8)]
        public Int64[] CYCLE_COUNT;//EXT_CMD_PARA_CREEP_PRINT_V10  波形个数
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100, ArraySubType = UnmanagedType.I8)]
        public Int64[] LOOP_COUNT; //EXT_CMD_PARA_CREEP_PRINT_V11  循环次数 
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100, ArraySubType = UnmanagedType.I2)]
        public Int16[] TEST_STEP;//EXT_CMD_PARA_CREEP_PRINT_V12   试验段数
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100, ArraySubType = UnmanagedType.R4)]
        public float[] Free_13;//EXT_CMD_PARA_CREEP_PRINT_V13  空闲变量13
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100, ArraySubType = UnmanagedType.R4)]
        public float[] Free_14;//EXT_CMD_PARA_CREEP_PRINT_V14  空闲变量14
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100, ArraySubType = UnmanagedType.R4)]
        public float[] Free_15;//EXT_CMD_PARA_CREEP_PRINT_V15  空闲变量15
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100, ArraySubType = UnmanagedType.R4)]
        public float[] Free_16;//EXT_CMD_PARA_CREEP_PRINT_V16  空闲变量16
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100, ArraySubType = UnmanagedType.R4)]
        public float[] Free_17;//EXT_CMD_PARA_CREEP_PRINT_V17  空闲变量17
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100, ArraySubType = UnmanagedType.R4)]
        public float[] Free_18;//EXT_CMD_PARA_CREEP_PRINT_V18  空闲变量18
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100, ArraySubType = UnmanagedType.R4)]
        public float[] Free_19;//EXT_CMD_PARA_CREEP_PRINT_V19  空闲变量19
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100, ArraySubType = UnmanagedType.I2)]
        public Int16[] FuncID;//EXT_CMD_PARA_CREEP_PRINT_V20  控制器FuncID
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100, ArraySubType = UnmanagedType.R4)]
        public float[] ControlValue;//EXT_CMD_PARA_CREEP_PRINT_V21  控制值
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100, ArraySubType = UnmanagedType.R4)]
        public float[] Unbalancedness;//EXT_CMD_PARA_CREEP_PRINT_V22  左右变形不平衡度
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100, ArraySubType = UnmanagedType.R4)]
        public float[] TemperatureControl;//EXT_CMD_PARA_CREEP_PRINT_V23  试验温度设定值
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100, ArraySubType = UnmanagedType.R4)]
        public float[] TemperatureGradient;//EXT_CMD_PARA_CREEP_PRINT_V24  温度梯度

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100, ArraySubType = UnmanagedType.I2)]
        public Int16[] EDC_STATE;//EXT_CMD_PARA_CREEP_PRINT_V25  控制器状态
                                 //'/*0*/ EDC_STATE_NOT_READY,                     /* EDC is not ready         */
                                 //'/*1*/ EDC_STATE_OFF,                           /* EDC is OFF               */
                                 //'/*2*/ EDC_STATE_ON,                            /* EDC is ON                */
                                 //'/*3*/ EDC_STATE_TEST,                          /* EDC is in TEST mode      */      
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100, ArraySubType = UnmanagedType.I2)]
        public Int16[] CMD_BIT;//EXT_CMD_PARA_CREEP_PRINT_V26  位报警  将十进制转为2进制，第0位是结束报警1是有报警0为无报警，第1位是变形累加报警1是有报警0为无报警。
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100, ArraySubType = UnmanagedType.R4)]
        public float[] USED_BUFFER;//EXT_CMD_PARA_CREEP_PRINT_V27  控制器缓冲区大小
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100, ArraySubType = UnmanagedType.I2)]
        public Int16[] TEST_END;//EXT_CMD_PARA_CREEP_PRINT_V28  控制器结束状态  0=不是结束试验信号，>0是结束试验信号
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100, ArraySubType = UnmanagedType.R4)]
        public float[] Free_29;//EXT_CMD_PARA_CREEP_PRINT_V29  空闲变量29

        public ulong tcount;

        public void init()
        {
            int size = 100;
            if (GlobeVal.myconfigfile.machinecount > 0)
            {
                size = GlobeVal.myconfigfile.machinecount;
            }
            TOTAL_TIME = new double[size];
            TEST_TIME = new double[size];
            CHANNEL_S = new float[size];
            CHANNEL_F = new float[size];
            CHANNEL_E = new float[size];
            CHANNEL_4 = new float[size];
            CHANNEL_5 = new float[size];
            CHANNEL_7 = new float[size];
            CHANNEL_8 = new float[size];
            CHANNEL_9 = new float[size];
            CYCLE_COUNT = new Int64[size];
            LOOP_COUNT = new Int64[size];
            TEST_STEP = new Int16[size];
            Free_13 = new float[size];
            Free_14 = new float[size];
            Free_15 = new float[size];
            Free_16 = new float[size];
            Free_17 = new float[size];
            Free_18 = new float[size];
            Free_19 = new float[size];
            FuncID = new Int16[size];
            ControlValue = new float[size];
            Unbalancedness = new float[size];
            TemperatureControl = new float[size];
            TemperatureGradient = new float[size];
            EDC_STATE = new Int16[size];
            CMD_BIT = new Int16[size];
            USED_BUFFER = new float[size];
            TEST_END = new Int16[size];
            Free_29 = new float[size];
        }
    }

    class PipeClient
    {

        NamedPipeClientStream pipeStream;

        public void Send(byte[] SendStr, string PipeName, int count, int TimeOut = 1000)
        {
            try
            {
                NamedPipeClientStream pipeStream = new NamedPipeClientStream(".", PipeName, PipeDirection.Out, PipeOptions.Asynchronous);
                // The connect function will indefinitely wait for the pipe to become available
                // If that is not acceptable specify a maximum waiting time (in ms)
                pipeStream.Connect(TimeOut);
                Debug.WriteLine("[Client] Pipe connection established");
                pipeStream.BeginWrite(SendStr, 0, count, AsyncSend, pipeStream);
            }
            catch (TimeoutException oEX)
            {
                Debug.WriteLine(oEX.Message);
            }
        }

        private void AsyncSend(IAsyncResult iar)
        {
            try
            {
                // Get the pipe
                NamedPipeClientStream pipeStream = (NamedPipeClientStream)iar.AsyncState;

                // End the write
                pipeStream.EndWrite(iar);
                pipeStream.Flush();
                pipeStream.Close();
                pipeStream.Dispose();
            }
            catch (Exception oEX)
            {
                Debug.WriteLine(oEX.Message);
            }
        }

    }

    class PipeServer
    {
        public delegate void DelegateMessage(byte[] Reply);
        public TransferCmd _TransferCmd;
        public event DelegateMessage PipeMessage;
        string _pipeName;

        public void Listen(string PipeName)
        {
            try
            {
                // Set to class level var so we can re-use in the async callback method
                _pipeName = PipeName;
                NamedPipeServerStream pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                // Wait for a connection
                pipeServer.BeginWaitForConnection(new AsyncCallback(WaitForConnectionCallBack), pipeServer);
            }
            catch (Exception oEX)
            {
                Debug.WriteLine(oEX.Message);
            }
        }

        private void WaitForConnectionCallBack(IAsyncResult iar)
        {
            try
            {
                // Get the pipe
                NamedPipeServerStream pipeServer = (NamedPipeServerStream)iar.AsyncState;
                // End waiting for the connection
                pipeServer.EndWaitForConnection(iar);
                int l = Marshal.SizeOf(_TransferCmd);

                byte[] buffer = new byte[l];

                // Read the incoming message
                pipeServer.Read(buffer, 0, l);

                // Convert byte buffer to string
                //  string stringData = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
                // Debug.WriteLine(stringData + Environment.NewLine);

                // Pass message back to calling form

                // char[] stringData = Encoding.UTF8.GetChars(buffer, 0,l); 
                PipeMessage.Invoke(buffer);

                // Kill original sever and create new wait server
                pipeServer.Close();
                pipeServer = null;
                pipeServer = new NamedPipeServerStream(_pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

                // Recursively wait for the connection again and again....
                pipeServer.BeginWaitForConnection(new AsyncCallback(WaitForConnectionCallBack), pipeServer);
            }
            catch
            {
                return;
            }
        }
    }
}
