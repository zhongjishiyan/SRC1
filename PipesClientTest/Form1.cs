using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Globalization;
using System.Runtime.InteropServices;
using PipesClientTest;
using System.IO;
using Doli.DoSANet;

namespace PipesClientTest
{
    public partial class Form1 : Form
    {
        private TransferData _TransferData = new TransferData();
        private Random _rd = new Random(1);
        private PipeClient _pipeClient;
        private PipeServer _pipeServer;
        private ulong _ctr = 0;
        private byte[] _vchar;
        bool running = true;
        private int intervalMs;                     // interval in mimliseccond;
        private long clockFrequency;            // result of QueryPerformanceFrequency()         

        public int Interval
        {
            get { return intervalMs; }
            set
            {
                intervalMs = value;
                intevalTicks = (long)((double)value * (double)clockFrequency / (double)1000);
            }
        }
        private long intevalTicks;
        private long nextTriggerTime;               // the time when next task will be executed

        Thread thread;
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

        /// 
        /// Pointer to a variable that receives the current performance-counter frequency, 
        /// in counts per second. 
        /// If the installed hardware does not support a high-resolution performance counter, 
        /// this parameter can be zero. 
        /// 
        /// 
        /// If the installed hardware supports a high-resolution performance counter, 
        /// the return value is nonzero.
        /// 
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long lpFrequency);

        public bool GetTick(out long currentTickCount)
        {
            if (QueryPerformanceCounter(out currentTickCount) == false)
                throw new Win32Exception("QueryPerformanceCounter() failed!");
            else
                return true;
        }

        public TransferCmd ByteToTransferCmd<TransferCmd>(byte[] dataBuffer)
        {
            object structure = null;
            int size = Marshal.SizeOf(typeof(TransferCmd));
            IntPtr allocIntPtr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(dataBuffer, 0, allocIntPtr, size);
                structure = Marshal.PtrToStructure(allocIntPtr, typeof(TransferCmd));
            }
            finally
            {
                Marshal.FreeHGlobal(allocIntPtr);
            }
            return (TransferCmd)structure;
        }

        public TransferData ByteToTransferData<TransferData>(byte[] dataBuffer)
        {
            object structure = null;
            int size = Marshal.SizeOf(typeof(TransferData));
            IntPtr allocIntPtr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(dataBuffer, 0, allocIntPtr, size);
                structure = Marshal.PtrToStructure(allocIntPtr, typeof(TransferData));
            }
            finally
            {
                Marshal.FreeHGlobal(allocIntPtr);
            }
            return (TransferData)structure;
        }

        /// <summary>
        /// 将结构转换为字节数组
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public byte[] StructTOBytes(object obj)
        {
            int size = Marshal.SizeOf(obj);
            //创建byte数组
            byte[] bytes = new byte[size];
            IntPtr structPtr = Marshal.AllocHGlobal(size);
            //将结构体拷贝到分配好的内存空间
            Marshal.StructureToPtr(obj, structPtr, false);
            //从内存空间拷贝到byte数组
            Marshal.Copy(structPtr, bytes, 0, size);
            //释放内存空间
            Marshal.FreeHGlobal(structPtr);
            return bytes;
        }

        public void DelayS(double t)
        {
            double m = Environment.TickCount;
            while ((Environment.TickCount - m) / 1000 <= t)
            {
                Application.DoEvents();
            }
        }

        private void ThreadProc()
        {
            long currTime;
            GetTick(out currTime);
            nextTriggerTime = currTime + intevalTicks;
            while (running)
            {
                while (currTime < nextTriggerTime)
                {
                    GetTick(out currTime);
                }   // wailt an interval
                nextTriggerTime = currTime + intevalTicks;
                _vchar = StructTOBytes(_TransferData);
                int l = Marshal.SizeOf(_TransferData);
                // _pipeClient.Send(_TransferData,"TestPipe",10000);
                _pipeClient.Send(_vchar, "TestPipe", l, 10000);
                _ctr++;
                _TransferData.tcount = _ctr;
            }
        }

        private void PipesMessageHandler(byte[] message)
        {
            string strEDC = "";
            string buf = "";
            _pipeServer._TransferCmd = ByteToTransferCmd<TransferCmd>(message);
            this.toolStripStatusLabel2.Text = "接收：" + _pipeServer._TransferCmd.tcount.ToString() + "        ";
            int sysNo = _pipeServer._TransferCmd.FuncID - 1;
            //Display("FuncID:" + _pipeServer._TransferCmd.FuncID.ToString()+
            Display("cmdName:" + _pipeServer._TransferCmd.cmdName.ToString());
            //    "cmdRead:" + _pipeServer._TransferCmd.cmdRead.ToString()+
            //    "cmdValue:" + _pipeServer._TransferCmd.cmdValue.ToString()+
            //    "cmdUnit:" + _pipeServer._TransferCmd.cmdUnit.ToString());

            //modMain.blnPipeConnectOK[_pipeServer._TransferCmd.FuncID - 1] = true;
            //modMain.intPipeErrorNum[_pipeServer._TransferCmd.FuncID - 1] = 0
            switch (_pipeServer._TransferCmd.cmdName)
            {
                case modMain.ConnectToEdcAll://软件退出后接到前台命令重新启动。
                    //ReadConfigFileAndInit();
                    ConnectToEdcAll();
                    //ReadTestFileAndContinue();
                    break;
                case modMain.ConnectToEdcFuncID:
                    ConnectToEdcFuncID(_pipeServer._TransferCmd.FuncID);
                    strEDC = modMain.strEDCfile + DateTime.Now.ToString("yyyy_MM_dd") + "." + "EDC";
                    buf = "FuncID=" + string.Format("{0:000}", sysNo + 1) + "," + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "," + "ConnectToEdcFuncID";
                    writeFile(sysNo, strEDC, buf, (int)modMain.FileType.EDCiFile);
                    break;
                case modMain.CloseLink_FuncID:
                    CloseLinkToEdcFuncID(_pipeServer._TransferCmd.FuncID);
                    strEDC = modMain.strEDCfile + DateTime.Now.ToString("yyyy_MM_dd") + "." + "EDC";
                    buf = "FuncID=" + string.Format("{0:000}", sysNo + 1) + "," + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "," + "CloseLinkToEdcFuncID";
                    writeFile(sysNo, strEDC, buf, (int)modMain.FileType.EDCiFile);
                    break;
                case (int)DoSA.DoSA_EXT_CMD.EXT_CMD_EDC_ON:
                    DriveOn();
                    strEDC = modMain.strEDCfile + DateTime.Now.ToString("yyyy_MM_dd") + "." + "EDC";
                    buf = "FuncID=" + string.Format("{0:000}", sysNo + 1) + "," + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "," + "EXT_CMD_EDC_ON";
                    writeFile(sysNo, strEDC, buf, (int)modMain.FileType.EDCiFile);
                    break;
                case (int)DoSA.DoSA_EXT_CMD.EXT_CMD_EDC_OFF:
                    DriveOff();
                    strEDC = modMain.strEDCfile + DateTime.Now.ToString("yyyy_MM_dd") + "." + "EDC";
                    buf = "FuncID=" + string.Format("{0:000}", sysNo + 1) + "," + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "," + "EXT_CMD_EDC_OFF";
                    writeFile(sysNo, strEDC, buf, (int)modMain.FileType.EDCiFile);
                    break;
                case (int)DoSA.DoSA_EXT_CMD.EXT_CMD_START:
                    TestStart();
                    strEDC = modMain.strEDCfile + DateTime.Now.ToString("yyyy_MM_dd") + "." + "EDC";
                    buf = "FuncID=" + string.Format("{0:000}", sysNo + 1) + "," + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "," + "EXT_CMD_START";
                    writeFile(sysNo, strEDC, buf, (int)modMain.FileType.EDCiFile);
                    modMain.blnStartTest[_pipeServer._TransferCmd.FuncID - 1] = true;
                    break;
                case (int)DoSA.DoSA_EXT_CMD.EXT_CMD_STOP:
                    TestEnd();
                    strEDC = modMain.strEDCfile + DateTime.Now.ToString("yyyy_MM_dd") + "." + "EDC";
                    buf = "FuncID=" + string.Format("{0:000}", sysNo + 1) + "," + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "," + "EXT_CMD_STOP";
                    writeFile(sysNo, strEDC, buf, (int)modMain.FileType.EDCiFile);
                    break;
                case (int)DoSA.DoSA_EXT_CMD.EXT_CMD_HALT:
                    TestHalt();
                    strEDC = modMain.strEDCfile + DateTime.Now.ToString("yyyy_MM_dd") + "." + "EDC";
                    buf = "FuncID=" + string.Format("{0:000}", sysNo + 1) + "," + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "," + "EXT_CMD_HALT";
                    writeFile(sysNo, strEDC, buf, (int)modMain.FileType.EDCiFile);
                    break;
                case (int)DoSA.DoSA_EXT_CMD.EXT_CMD_CONTINUE:
                    TestContinue();
                    strEDC = modMain.strEDCfile + DateTime.Now.ToString("yyyy_MM_dd") + "." + "EDC";
                    buf = "FuncID=" + string.Format("{0:000}", sysNo + 1) + "," + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "," + "EXT_CMD_CONTINUE";
                    writeFile(sysNo, strEDC, buf, (int)modMain.FileType.EDCiFile);
                    break;
                case (int)DoSA.DoSA_EXT_CMD.EXT_CMD_GET_EDC_STATE:
                    GET_STATE();
                    break;
                case (int)DoSA.DoSA_EXT_CMD.EXT_CMD_GET_USED_BUFFER:
                    GET_BUFFER();
                    break;
                case (int)DoSA.DoSA_EXT_CMD.EXT_CMD_BIT:
                    GET_CMD_BIT();
                    break;
                case (int)modMain.TestPara_Name:
                    ReadParaAndWriteEDC(_pipeServer._TransferCmd.FuncID, _pipeServer._TransferCmd.strPara_FileName, 1);
                    break;
                case (int)modMain.Config_File:
                    ReadConfigFileAndInit();
                    break;
                case (int)modMain.Test_Recovery://恢复试验
                    //ReadConfigFileAndInit();
                    break;
            }
        }

        ///----------------------------------------------------------------------
        /// <summary>Display debug text</summary>
        ///----------------------------------------------------------------------
        private void Display(string Text)
        {
            //this.richTextBox1.AppendText(Text );
            ////this.richTextBox1.AppendText(Text + "\r\n");
            //this.richTextBox1.Refresh();
            this.toolStripStatusLabel3.Text = Text;
        }

        ///----------------------------------------------------------------------
        /// <summary>
        /// Send command string to EDC
        /// 
        /// <para>Command string: "C;R;P;U"</para>
        /// <para>C = command number</para>
        /// <para>R = 0/1 (write/read)</para>
        /// <para>P = Parameter value (int or double value)</para>
        /// <para>U = Unit (number)</para>
        /// </summary>
        /// 
        /// <param name="Text">Command string: "C;R;P;U"</param>
        /// <param name="Show">true/false to show string in debug window</param>
        /// 
        /// <returns>Error constant (DoSA.ERROR.xxxx)</returns>
        ///----------------------------------------------------------------------
        private DoSA.ERROR SendExtCmdStr(string Text, bool Show = true)
        {
            // show debug text
            if (Show)
                Display("PC->EDC: " + Text);
            // send command string to EDC
            if (modMain.MeDoSA[_pipeServer._TransferCmd.FuncID - 1] != null)
            {
                if (modMain.MeDoSA[_pipeServer._TransferCmd.FuncID - 1].DoSAHdl.ToInt32() != 0)
                {
                    return modMain.MeDoSA[_pipeServer._TransferCmd.FuncID - 1].WriteMessage(Text);
                }
            }
            return DoSA.ERROR.OFFLINE;
        }

        ///----------------------------------------------------------------------
        /// <summary>Send command to EDC (int value without unit)</summary>
        ///----------------------------------------------------------------------
        private DoSA.ERROR SendExtCmd(DoSA.DoSA_EXT_CMD Cmd, int Read, int Value, bool Show = true)
        {
            return SendExtCmdStr(string.Format("{0:d};{1};{2};0", Cmd, Read, Value), Show);
        }

        ///----------------------------------------------------------------------
        /// <summary>Send command to EDC (double value with unit)</summary>
        ///----------------------------------------------------------------------
        private DoSA.ERROR SendExtCmd(DoSA.DoSA_EXT_CMD Cmd, int Read, double Value, int Unit, bool Show = true)
        {
            // set decimal point and group sepeartor for double value
            NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;
            nfi.NumberDecimalSeparator = ".";
            nfi.NumberGroupSeparator = "";
            return SendExtCmdStr(string.Format("{0:d};{1};{2};{3}", Cmd, Read, Value.ToString(nfi), Unit), Show);
        }

        ///----------------------------------------------------------------------
        /// <summary>Send command to poll data from EDC</summary>
        ///----------------------------------------------------------------------
        private DoSA.ERROR SendExtCmdGetData()
        {
            return SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_GET_DATA, 1, 0, false);
        }

        ///----------------------------------------------------------------------
        /// 保存前台软件的试验参数文件。   共100行，每行变量的个数固定为10个用逗号隔开
        ///----------------------------------------------------------------------
        private void SaveParaFile(int sysNo)
        {
            using (StreamWriter wf = File.AppendText(_pipeServer._TransferCmd.strPara_FileName))
            {
                string strline = "0,0,0,0,0,0,0,0,0,0";
                //1到48行   48段试验
                for (int intStep = 0; intStep < 48; intStep++)//48段试验段数，实际为12段，预留36段
                {
                    if (modMain.PARA_MODEn[sysNo - 1, intStep] == 0 || modMain.PARA_MODEn[sysNo - 1, intStep] == 1)
                    {
                        strline = modMain.PARA_CTRLn[sysNo - 1, intStep].ToString() + "," +
                                    modMain.PARA_MODEn[sysNo - 1, intStep].ToString() + "," +
                                    modMain.PARA_Pn[sysNo - 1, intStep].ToString() + "," +
                                    modMain.PARA_Pn_Unit[sysNo - 1, intStep].ToString() + "," +
                                    modMain.PARA_Tn[sysNo - 1, intStep].ToString() + "," +
                                    modMain.PARA_Tn_Unit[sysNo - 1, intStep].ToString() + "," +
                                    modMain.PARA_Vn[sysNo - 1, intStep].ToString() + "," +
                                    modMain.PARA_Vn_Unit[sysNo - 1, intStep].ToString() + "," +
                                    modMain.strFree.ToString() + "," +
                                    modMain.strFree.ToString();
                    }
                    else
                    {
                        strline = modMain.PARA_CTRLn[sysNo - 1, intStep].ToString() + "," +
                                    modMain.PARA_MODEn[sysNo - 1, intStep].ToString() + "," +
                                    modMain.PARA_OFFn[sysNo - 1, intStep].ToString() + "," +
                                    modMain.PARA_OFFn_Unit[sysNo - 1, intStep].ToString() + "," +
                                    modMain.PARA_AMPLn[sysNo - 1, intStep].ToString() + "," +
                                    modMain.PARA_AMPLn_Unit[sysNo - 1, intStep].ToString() + "," +
                                    modMain.PARA_FREQn[sysNo - 1, intStep].ToString() + "," +
                                    modMain.PARA_CYCLEn[sysNo - 1, intStep].ToString() + "," +
                                    modMain.strFree.ToString() + "," +
                                    modMain.strFree.ToString();
                    }
                    wf.WriteLine(strline);
                }

                //49到60行   12段温度
                for (int intStep = 0; intStep < 12; intStep++)//12段温度段数，把试验温度和保温时间和温度波动度和试验结束温度写入到第1段中，其它11段留用
                {
                    strline = modMain.CREEP_TEMP[sysNo - 1, intStep].ToString() + "," +
                                    modMain.CREEP_TEMP_RAMP[sysNo - 1, intStep].ToString() + "," +
                                    modMain.CREEP_TEMP_WAIT[sysNo - 1, intStep].ToString() + "," +
                                    modMain.CREEP_TEMP_DELTA[sysNo - 1, intStep].ToString() + "," +
                                    modMain.CREEP_TEMP_END[sysNo - 1, intStep].ToString() + "," +
                                    modMain.strFree.ToString() + "," +
                                    modMain.strFree.ToString() + "," +
                                    modMain.strFree.ToString() + "," +
                                    modMain.strFree.ToString() + "," +
                                    modMain.strFree.ToString();
                    wf.WriteLine(strline);
                }

                //61到65行   5段保存数据条件的设置
                for (int intStep = 0; intStep < 5; intStep++)//5段保存数据条件的设置
                {
                    strline = modMain.SaveTimeStep[sysNo - 1].ToString() + "," +
                                    modMain.SaveTimeFrom[sysNo - 1, intStep].ToString() + "," +
                                    modMain.SaveTimeTo[sysNo - 1, intStep].ToString() + "," +
                                    modMain.SaveTimeFromToUnit[sysNo - 1, intStep].ToString() + "," +
                                    modMain.SaveTimeInv[sysNo - 1, intStep].ToString() + "," +
                                    modMain.SaveTimeInvUnit[sysNo - 1, intStep].ToString() + "," +
                                    modMain.strFree.ToString() + "," +
                                    modMain.strFree.ToString() + "," +
                                    modMain.strFree.ToString() + "," +
                                    modMain.strFree.ToString();
                    wf.WriteLine(strline);
                }

                //66
                strline = modMain.TestType[sysNo - 1].ToString() + "," +
                                    modMain.TestLoadRange[sysNo - 1].ToString() + "," +
                                    modMain.TestDefRange[sysNo - 1].ToString() + "," +
                                    modMain.TestStandard[sysNo - 1].ToString() + "," +
                                    modMain.TestYingLi[sysNo - 1].ToString() + "," +
                                    modMain.TestTotalLoad[sysNo - 1].ToString() + "," +
                                    modMain.TestMainLoad[sysNo - 1].ToString() + "," +
                                    modMain.PARA_CREEP_STEPS[sysNo - 1].ToString() + "," +
                                    modMain.PARA_CREEP_TEST_TIME[sysNo - 1].ToString() + "," +
                                    modMain.PARA_LOOPS[sysNo - 1].ToString();
                wf.WriteLine(strline);

                //67
                strline = modMain.PARA_IS_CTRL0[sysNo - 1].ToString() + "," +
                                    modMain.PARA_CTRL0[sysNo - 1].ToString() + "," +
                                    modMain.PARA_MODE0[sysNo - 1].ToString() + "," +
                                    modMain.PARA_V0[sysNo - 1].ToString() + "," +
                                    modMain.PARA_V0_Unit[sysNo - 1].ToString() + "," +
                                    modMain.PARA_P0[sysNo - 1].ToString() + "," +
                                    modMain.PARA_P0_Unit[sysNo - 1].ToString() + "," +
                                    modMain.PARA_T0[sysNo - 1].ToString() + "," +
                                    modMain.PARA_T0_Unit[sysNo - 1].ToString() + "," +
                                    modMain.strFree.ToString();
                wf.WriteLine(strline);

                //68               
                strline = modMain.CREEP_EXTENSION_LIMIT[sysNo - 1].ToString() + "," +
                                    modMain.CREEP_REF_TIME[sysNo - 1].ToString() + "," +
                                    modMain.RETURN_ACTION[sysNo - 1].ToString() + "," +
                                    modMain.VRETURN_ACTION[sysNo - 1].ToString() + "," +
                                    modMain.VRETURN_ACTION_Unit[sysNo - 1].ToString() + "," +
                                    modMain.PRINT_TIME[sysNo - 1].ToString() + "," +
                                    modMain.PRINT_DS[sysNo - 1].ToString() + "," +
                                    modMain.PRINT_DF[sysNo - 1].ToString() + "," +
                                    modMain.PRINT_DE[sysNo - 1].ToString() + "," +
                                    modMain.strFree.ToString();
                wf.WriteLine(strline);

                //69              
                strline = modMain.END_SENSOR[sysNo - 1].ToString() + "," +
                                    modMain.END_MODE[sysNo - 1].ToString() + "," +
                                    modMain.END_VALUE[sysNo - 1].ToString() + "," +
                                    modMain.END_VALUE_Unit[sysNo - 1].ToString() + "," +
                                    modMain.END_ACTION[sysNo - 1].ToString() + "," +
                                    modMain.VEND_ACTION[sysNo - 1].ToString() + "," +
                                    modMain.VEND_ACTION_Unit[sysNo - 1].ToString() + "," +
                                    modMain.strFree.ToString() + "," +
                                    modMain.strFree.ToString() + "," +
                                    modMain.strFree.ToString();
                wf.WriteLine(strline);

                //70
                strline = modMain.LIMIT_SENSOR[sysNo - 1].ToString() + "," +
                                    modMain.LIMIT_PP[sysNo - 1].ToString() + "," +
                                    modMain.LIMIT_PP_Unit[sysNo - 1].ToString() + "," +
                                    modMain.LIMIT_MM[sysNo - 1].ToString() + "," +
                                    modMain.LIMIT_MM_Unit[sysNo - 1].ToString() + "," +
                                    modMain.LIMIT_ACTION[sysNo - 1].ToString() + "," +
                                    modMain.VLIMIT_ACTION[sysNo - 1].ToString() + "," +
                                    modMain.VLIMIT_ACTION_Unit[sysNo - 1].ToString() + "," +
                                    modMain.strFree.ToString() + "," +
                                    modMain.strFree.ToString();
                wf.WriteLine(strline);

                //71
                strline = modMain.SOFT_SENSOR[sysNo - 1].ToString() + "," +
                                    modMain.SOFT_MODE[sysNo - 1].ToString() + "," +
                                    modMain.SOFT_VALUE[sysNo - 1].ToString() + "," +
                                    modMain.SOFT_VALUE_Unit[sysNo - 1].ToString() + "," +
                                    modMain.SOFT_ACTION[sysNo - 1].ToString() + "," +
                                    modMain.VSOFT_ACTION[sysNo - 1].ToString() + "," +
                                    modMain.VSOFT_ACTION_Unit[sysNo - 1].ToString() + "," +
                                    modMain.SOFT_CycleNum[sysNo - 1].ToString() + "," +
                                    modMain.strFree.ToString() + "," +
                                    modMain.strFree.ToString();
                wf.WriteLine(strline);

                //72
                strline = modMain.SOFT_TEMP_SENSOR[sysNo - 1].ToString() + "," +
                                    modMain.SOFT_TEMP_MODE[sysNo - 1].ToString() + "," +
                                    modMain.SOFT_TEMP_VALUE[sysNo - 1].ToString() + "," +
                                    modMain.SOFT_TEMP_VALUE_Unit[sysNo - 1].ToString() + "," +
                                    modMain.SOFT_TEMP_ACTION[sysNo - 1].ToString() + "," +
                                    modMain.SOFT_VTEMP_ACTION[sysNo - 1].ToString() + "," +
                                    modMain.SOFT_VTEMP_ACTION_Unit[sysNo - 1].ToString() + "," +
                                    modMain.strFree.ToString() + "," +
                                    modMain.strFree.ToString() + "," +
                                    modMain.strFree.ToString();
                wf.WriteLine(strline);

                //73
                strline = modMain.SOFT_IS_WARN[sysNo - 1].ToString() + "," +
                                    modMain.SOFT_Load_WARN[sysNo - 1].ToString() + "," +
                                    modMain.SOFT_Position_WARN[sysNo - 1].ToString() + "," +
                                    modMain.SOFT_Extension_WARN[sysNo - 1].ToString() + "," +
                                    modMain.SOFT_TEMP_Fluct_WARN[sysNo - 1].ToString() + "," +
                                    modMain.SOFT_TEMP_Grad_WARN[sysNo - 1].ToString() + "," +
                                    modMain.strFree.ToString() + "," +
                                    modMain.strFree.ToString() + "," +
                                    modMain.strFree.ToString() + "," +
                                    modMain.strFree.ToString();
                wf.WriteLine(strline);

                //74
                strline = modMain.KeepTest[sysNo - 1].ToString() + "," +
                                    modMain.SaveMode[sysNo - 1].ToString() + "," +
                                    modMain.InvCycle[sysNo - 1].ToString() + "," +
                                    modMain.InvLoop[sysNo - 1].ToString() + "," +
                                    modMain.strFree.ToString() + "," +
                                    modMain.strFree.ToString() + "," +
                                    modMain.strFree.ToString() + "," +
                                    modMain.strFree.ToString() + "," +
                                    modMain.strFree.ToString() + "," +
                                    modMain.strFree.ToString();
                wf.WriteLine(strline);

                //75
                strline = modMain.Sample_Shape[sysNo - 1].ToString() + "," +
                                    modMain.Sample_Diameter[sysNo - 1].ToString() + "," +
                                    modMain.Sample_Diameter_Unit[sysNo - 1].ToString() + "," +
                                    modMain.Sample_Width[sysNo - 1].ToString() + "," +
                                    modMain.Sample_Width_Unit[sysNo - 1].ToString() + "," +
                                    modMain.Sample_Thickness[sysNo - 1].ToString() + "," +
                                    modMain.Sample_Thickness_Unit[sysNo - 1].ToString() + "," +
                                    modMain.Sample_Gauge_Length[sysNo - 1].ToString() + "," +
                                    modMain.strFree.ToString() + "," +
                                    modMain.strFree.ToString();
                wf.WriteLine(strline);

                //76
                strline = modMain.ADD_TOTAL_TIME[sysNo - 1].ToString() + "," +
                                    modMain.ADD_TEST_TIME[sysNo - 1].ToString() + "," +
                                    modMain.ADD_CHANNEL_S[sysNo - 1].ToString() + "," +
                                    modMain.ADD_CHANNEL_F[sysNo - 1].ToString() + "," +
                                    modMain.ADD_CHANNEL_E[sysNo - 1].ToString() + "," +
                                    modMain.ADD_CHANNEL_4[sysNo - 1].ToString() + "," +
                                    modMain.ADD_CHANNEL_5[sysNo - 1].ToString() + "," +
                                    modMain.ADD_CHANNEL_7[sysNo - 1].ToString() + "," +
                                    modMain.ADD_CHANNEL_8[sysNo - 1].ToString() + "," +
                                    modMain.ADD_CHANNEL_9[sysNo - 1].ToString();
                wf.WriteLine(strline);

                //77
                strline = modMain.ADD_CYCLE_COUNT[sysNo - 1].ToString() + "," +
                                    modMain.ADD_LOOP_COUNT[sysNo - 1].ToString() + "," +
                                    modMain.ADD_TEST_STEP[sysNo - 1].ToString() + "," +
                                    modMain.ADD_Free_13[sysNo - 1].ToString() + "," +
                                    modMain.ADD_Free_14[sysNo - 1].ToString() + "," +
                                    modMain.ADD_Free_15[sysNo - 1].ToString() + "," +
                                    modMain.strFree.ToString() + "," +
                                    modMain.strFree.ToString() + "," +
                                    modMain.strFree.ToString() + "," +
                                    modMain.strFree.ToString();
                wf.WriteLine(strline);

                //78到100行
                for (int intHang = 78; intHang <= 100; intHang++)
                {
                    strline = modMain.strFree.ToString() + "," +
                                    modMain.strFree.ToString() + "," +
                                    modMain.strFree.ToString() + "," +
                                    modMain.strFree.ToString() + "," +
                                    modMain.strFree.ToString() + "," +
                                    modMain.strFree.ToString() + "," +
                                    modMain.strFree.ToString() + "," +
                                    modMain.strFree.ToString() + "," +
                                    modMain.strFree.ToString() + "," +
                                    modMain.strFree.ToString();
                    wf.WriteLine(strline);
                }
            }

        }

        ///----------------------------------------------------------------------
        /// 读取前台软件的试验参数，写入到控制器中   共100行，每行变量的个数固定为10个用逗号隔开。intReadWrite=0为不写入控制器，intReadWrite=1时是写入控制器。
        ///----------------------------------------------------------------------
        private void ReadParaAndWriteEDC(int sysNo, string strFileName, int intReadWrite)
        {
            bool blnFileExist = false;//参数文件是否存在
            int intArrNo = 0;
            modMain.ParaFile[sysNo - 1] = strFileName;// _pipeServer._TransferCmd.strPara_FileName;
            modMain.DataFile[sysNo - 1] = modMain.ParaFile[sysNo - 1].Substring(0, modMain.ParaFile[sysNo - 1].Length - 4) + "Data";
            modMain.RecoFile[sysNo - 1] = modMain.ParaFile[sysNo - 1].Substring(0, modMain.ParaFile[sysNo - 1].Length - 4) + "Reco";
            modMain.TempFile[sysNo - 1] = modMain.ParaFile[sysNo - 1].Substring(0, modMain.ParaFile[sysNo - 1].Length - 4) + "Temp";
            try
            {
                if (File.Exists(strFileName))
                {
                    string[] lines = File.ReadAllLines(strFileName);
                    int row = lines.GetLength(0); //行数
                    string[] cols = lines[0].Split(',');
                    int col = cols.GetLength(0);  //每行数据的个数  
                    string[,] arrParas = new string[row, col]; //数组
                    for (int i = 0; i < row; i++)  //读入数据并赋予数组
                    {
                        string[] data = lines[i].Split(',');
                        for (int j = 0; j < col; j++)
                        {
                            arrParas[i, j] = (data[j]);
                        }
                    }

                    //int.TryParse(string s, out int result)
                    //该方式也是将数字内容的字符串转换为int类型，但是该方式有比int.Parse 优越的地方，就是它不会出现异常，最后一个参数result是输出值，如果转换成功则输出相应的值，转换失败则输出0。　
                    //1到48行   48段试验
                    intArrNo = 0;
                    for (int intStep = 0; intStep < 48; intStep++)//48段试验段数，实际为12段，预留36段
                    {
                        modMain.PARA_CTRLn[sysNo - 1, intStep] = int.Parse(arrParas[intStep + intArrNo, 0]);
                        modMain.PARA_MODEn[sysNo - 1, intStep] = int.Parse(arrParas[intStep + intArrNo, 1]);
                        if (modMain.PARA_MODEn[sysNo - 1, intStep] == 0 || modMain.PARA_MODEn[sysNo - 1, intStep] == 1)
                        {
                            modMain.PARA_Pn[sysNo - 1, intStep] = float.Parse(arrParas[intStep + intArrNo, 2]);
                            modMain.PARA_Pn_Unit[sysNo - 1, intStep] = int.Parse(arrParas[intStep + intArrNo, 3]);
                            modMain.PARA_Tn[sysNo - 1, intStep] = float.Parse(arrParas[intStep + intArrNo, 4]);
                            modMain.PARA_Tn_Unit[sysNo - 1, intStep] = int.Parse(arrParas[intStep + intArrNo, 5]);
                            modMain.PARA_Vn[sysNo - 1, intStep] = float.Parse(arrParas[intStep + intArrNo, 6]);
                            modMain.PARA_Vn_Unit[sysNo - 1, intStep] = int.Parse(arrParas[intStep + intArrNo, 7]);
                        }
                        else
                        {
                            modMain.PARA_OFFn[sysNo - 1, intStep] = float.Parse(arrParas[intStep + intArrNo, 2]);
                            modMain.PARA_OFFn_Unit[sysNo - 1, intStep] = int.Parse(arrParas[intStep + intArrNo, 3]);
                            modMain.PARA_AMPLn[sysNo - 1, intStep] = float.Parse(arrParas[intStep + intArrNo, 4]);
                            modMain.PARA_AMPLn_Unit[sysNo - 1, intStep] = int.Parse(arrParas[intStep + intArrNo, 5]);
                            modMain.PARA_FREQn[sysNo - 1, intStep] = float.Parse(arrParas[intStep + intArrNo, 6]);
                            modMain.PARA_CYCLEn[sysNo - 1, intStep] = long.Parse(arrParas[intStep + intArrNo, 7]);
                        }
                    }

                    //49到60行   12段温度
                    intArrNo = 48;
                    for (int intStep = 0; intStep < 12; intStep++)//12段温度段数，把试验温度和保温时间和温度波动度和试验结束温度写入到第1段中，其它11段留用
                    {
                        modMain.CREEP_TEMP[sysNo - 1, intStep] = float.Parse(arrParas[intStep + intArrNo, 0]);
                        modMain.CREEP_TEMP_RAMP[sysNo - 1, intStep] = float.Parse(arrParas[intStep + intArrNo, 1]);
                        modMain.CREEP_TEMP_WAIT[sysNo - 1, intStep] = float.Parse(arrParas[intStep + intArrNo, 2]);
                        modMain.CREEP_TEMP_DELTA[sysNo - 1, intStep] = float.Parse(arrParas[intStep + intArrNo, 3]);
                        modMain.CREEP_TEMP_END[sysNo - 1, intStep] = float.Parse(arrParas[intStep + intArrNo, 4]);
                    }

                    //61到65行   5段保存数据条件的设置
                    intArrNo = 60;
                    for (int intStep = 0; intStep < 5; intStep++)//5段保存数据条件的设置
                    {
                        modMain.SaveTimeStep[sysNo - 1] = int.Parse(arrParas[intStep + intArrNo, 0]);
                        modMain.SaveTimeFrom[sysNo - 1, intStep] = arrParas[intStep + intArrNo, 1];
                        modMain.SaveTimeTo[sysNo - 1, intStep] = arrParas[intStep + intArrNo, 2];
                        modMain.SaveTimeFromToUnit[sysNo - 1, intStep] = int.Parse(arrParas[intStep + intArrNo, 3]);
                        modMain.SaveTimeInv[sysNo - 1, intStep] = arrParas[intStep + intArrNo, 4];
                        modMain.SaveTimeInvUnit[sysNo - 1, intStep] = int.Parse(arrParas[intStep + intArrNo, 5]);
                    }

                    //66
                    intArrNo = 65;
                    modMain.TestType[sysNo - 1] = int.Parse(arrParas[intArrNo, 0]);
                    modMain.TestLoadRange[sysNo - 1] = float.Parse(arrParas[intArrNo, 1]);
                    modMain.TestDefRange[sysNo - 1] = float.Parse(arrParas[intArrNo, 2]);
                    modMain.TestStandard[sysNo - 1] = arrParas[intArrNo, 3];
                    modMain.TestYingLi[sysNo - 1] = float.Parse(arrParas[intArrNo, 4]);
                    modMain.TestTotalLoad[sysNo - 1] = float.Parse(arrParas[intArrNo, 5]);
                    modMain.TestMainLoad[sysNo - 1] = float.Parse(arrParas[intArrNo, 6]);
                    modMain.PARA_CREEP_STEPS[sysNo - 1] = int.Parse(arrParas[intArrNo, 7]);
                    modMain.PARA_CREEP_TEST_TIME[sysNo - 1] = double.Parse(arrParas[intArrNo, 8]);
                    modMain.PARA_LOOPS[sysNo - 1] = long.Parse(arrParas[intArrNo, 9]);

                    //67
                    intArrNo = 66;
                    modMain.PARA_IS_CTRL0[sysNo - 1] = int.Parse(arrParas[intArrNo, 0]);
                    modMain.PARA_CTRL0[sysNo - 1] = int.Parse(arrParas[intArrNo, 1]);
                    modMain.PARA_MODE0[sysNo - 1] = int.Parse(arrParas[intArrNo, 2]);
                    modMain.PARA_V0[sysNo - 1] = float.Parse(arrParas[intArrNo, 3]);
                    modMain.PARA_V0_Unit[sysNo - 1] = int.Parse(arrParas[intArrNo, 4]);
                    modMain.PARA_P0[sysNo - 1] = float.Parse(arrParas[intArrNo, 5]);
                    modMain.PARA_P0_Unit[sysNo - 1] = int.Parse(arrParas[intArrNo, 6]);
                    modMain.PARA_T0[sysNo - 1] = float.Parse(arrParas[intArrNo, 7]);
                    modMain.PARA_T0_Unit[sysNo - 1] = int.Parse(arrParas[intArrNo, 8]);

                    //68
                    intArrNo = 67;
                    modMain.CREEP_EXTENSION_LIMIT[sysNo - 1] = float.Parse(arrParas[intArrNo, 0]);
                    modMain.CREEP_REF_TIME[sysNo - 1] = float.Parse(arrParas[intArrNo, 1]);
                    modMain.RETURN_ACTION[sysNo - 1] = int.Parse(arrParas[intArrNo, 2]);
                    modMain.VRETURN_ACTION[sysNo - 1] = float.Parse(arrParas[intArrNo, 3]);
                    modMain.VRETURN_ACTION_Unit[sysNo - 1] = int.Parse(arrParas[intArrNo, 4]);
                    modMain.PRINT_TIME[sysNo - 1] = float.Parse(arrParas[intArrNo, 5]);
                    modMain.PRINT_DS[sysNo - 1] = float.Parse(arrParas[intArrNo, 6]);
                    modMain.PRINT_DF[sysNo - 1] = float.Parse(arrParas[intArrNo, 7]);
                    modMain.PRINT_DE[sysNo - 1] = float.Parse(arrParas[intArrNo, 8]);

                    //69
                    intArrNo = 68;
                    modMain.END_SENSOR[sysNo - 1] = int.Parse(arrParas[intArrNo, 0]);
                    modMain.END_MODE[sysNo - 1] = int.Parse(arrParas[intArrNo, 1]);
                    modMain.END_VALUE[sysNo - 1] = float.Parse(arrParas[intArrNo, 2]);
                    modMain.END_VALUE_Unit[sysNo - 1] = int.Parse(arrParas[intArrNo, 3]);
                    modMain.END_ACTION[sysNo - 1] = int.Parse(arrParas[intArrNo, 4]);
                    modMain.VEND_ACTION[sysNo - 1] = float.Parse(arrParas[intArrNo, 5]);
                    modMain.VEND_ACTION_Unit[sysNo - 1] = int.Parse(arrParas[intArrNo, 6]);

                    //70
                    intArrNo = 69;
                    modMain.LIMIT_SENSOR[sysNo - 1] = int.Parse(arrParas[intArrNo, 0]);
                    modMain.LIMIT_PP[sysNo - 1] = float.Parse(arrParas[intArrNo, 1]);
                    modMain.LIMIT_PP_Unit[sysNo - 1] = int.Parse(arrParas[intArrNo, 2]);
                    modMain.LIMIT_MM[sysNo - 1] = float.Parse(arrParas[intArrNo, 3]);
                    modMain.LIMIT_MM_Unit[sysNo - 1] = int.Parse(arrParas[intArrNo, 4]);
                    modMain.LIMIT_ACTION[sysNo - 1] = int.Parse(arrParas[intArrNo, 5]);
                    modMain.VLIMIT_ACTION[sysNo - 1] = float.Parse(arrParas[intArrNo, 6]);
                    modMain.VLIMIT_ACTION_Unit[sysNo - 1] = int.Parse(arrParas[intArrNo, 7]);

                    //71
                    intArrNo = 70;
                    modMain.SOFT_SENSOR[sysNo - 1] = int.Parse(arrParas[intArrNo, 0]);
                    modMain.SOFT_MODE[sysNo - 1] = int.Parse(arrParas[intArrNo, 1]);
                    modMain.SOFT_VALUE[sysNo - 1] = float.Parse(arrParas[intArrNo, 2]);
                    modMain.SOFT_VALUE_Unit[sysNo - 1] = int.Parse(arrParas[intArrNo, 3]);
                    modMain.SOFT_ACTION[sysNo - 1] = int.Parse(arrParas[intArrNo, 4]);
                    modMain.VSOFT_ACTION[sysNo - 1] = float.Parse(arrParas[intArrNo, 5]);
                    modMain.VSOFT_ACTION_Unit[sysNo - 1] = int.Parse(arrParas[intArrNo, 6]);
                    modMain.SOFT_CycleNum[sysNo - 1] = long.Parse(arrParas[intArrNo, 7]);

                    //72
                    intArrNo = 71;
                    modMain.SOFT_TEMP_SENSOR[sysNo - 1] = int.Parse(arrParas[intArrNo, 0]);
                    modMain.SOFT_TEMP_MODE[sysNo - 1] = int.Parse(arrParas[intArrNo, 1]);
                    modMain.SOFT_TEMP_VALUE[sysNo - 1] = float.Parse(arrParas[intArrNo, 2]);
                    modMain.SOFT_TEMP_VALUE_Unit[sysNo - 1] = int.Parse(arrParas[intArrNo, 3]);
                    modMain.SOFT_TEMP_ACTION[sysNo - 1] = int.Parse(arrParas[intArrNo, 4]);
                    modMain.SOFT_VTEMP_ACTION[sysNo - 1] = float.Parse(arrParas[intArrNo, 5]);
                    modMain.SOFT_VTEMP_ACTION_Unit[sysNo - 1] = int.Parse(arrParas[intArrNo, 6]);

                    //73
                    intArrNo = 72;
                    modMain.SOFT_IS_WARN[sysNo - 1] = int.Parse(arrParas[intArrNo, 0]);
                    modMain.SOFT_Load_WARN[sysNo - 1] = float.Parse(arrParas[intArrNo, 1]);
                    modMain.SOFT_Position_WARN[sysNo - 1] = float.Parse(arrParas[intArrNo, 2]);
                    modMain.SOFT_Extension_WARN[sysNo - 1] = float.Parse(arrParas[intArrNo, 3]);
                    modMain.SOFT_TEMP_Fluct_WARN[sysNo - 1] = float.Parse(arrParas[intArrNo, 4]);
                    modMain.SOFT_TEMP_Grad_WARN[sysNo - 1] = float.Parse(arrParas[intArrNo, 5]);

                    //74
                    intArrNo = 73;
                    modMain.KeepTest[sysNo - 1] = int.Parse(arrParas[intArrNo, 0]);
                    modMain.SaveMode[sysNo - 1] = int.Parse(arrParas[intArrNo, 1]);
                    modMain.InvCycle[sysNo - 1] = long.Parse(arrParas[intArrNo, 2]);
                    modMain.InvLoop[sysNo - 1] = long.Parse(arrParas[intArrNo, 3]);

                    //75
                    intArrNo = 74;
                    modMain.Sample_Shape[sysNo - 1] = int.Parse(arrParas[intArrNo, 0]);
                    modMain.Sample_Diameter[sysNo - 1] = float.Parse(arrParas[intArrNo, 1]);
                    modMain.Sample_Diameter_Unit[sysNo - 1] = int.Parse(arrParas[intArrNo, 2]);
                    modMain.Sample_Width[sysNo - 1] = float.Parse(arrParas[intArrNo, 3]);
                    modMain.Sample_Width_Unit[sysNo - 1] = int.Parse(arrParas[intArrNo, 4]);
                    modMain.Sample_Thickness[sysNo - 1] = float.Parse(arrParas[intArrNo, 5]);
                    modMain.Sample_Thickness_Unit[sysNo - 1] = int.Parse(arrParas[intArrNo, 6]);
                    modMain.Sample_Gauge_Length[sysNo - 1] = float.Parse(arrParas[intArrNo, 7]);

                    //76
                    intArrNo = 75;
                    modMain.ADD_TOTAL_TIME[sysNo - 1] = double.Parse(arrParas[intArrNo, 0]);
                    modMain.ADD_TEST_TIME[sysNo - 1] = double.Parse(arrParas[intArrNo, 1]);
                    modMain.ADD_CHANNEL_S[sysNo - 1] = float.Parse(arrParas[intArrNo, 2]);
                    modMain.ADD_CHANNEL_F[sysNo - 1] = float.Parse(arrParas[intArrNo, 3]);
                    modMain.ADD_CHANNEL_E[sysNo - 1] = float.Parse(arrParas[intArrNo, 4]);
                    modMain.ADD_CHANNEL_4[sysNo - 1] = float.Parse(arrParas[intArrNo, 5]);
                    modMain.ADD_CHANNEL_5[sysNo - 1] = float.Parse(arrParas[intArrNo, 6]);
                    modMain.ADD_CHANNEL_7[sysNo - 1] = float.Parse(arrParas[intArrNo, 7]);
                    modMain.ADD_CHANNEL_8[sysNo - 1] = float.Parse(arrParas[intArrNo, 8]);
                    modMain.ADD_CHANNEL_9[sysNo - 1] = float.Parse(arrParas[intArrNo, 9]);

                    //77
                    intArrNo = 76;
                    modMain.ADD_CYCLE_COUNT[sysNo - 1] = long.Parse(arrParas[intArrNo, 0]);
                    modMain.ADD_LOOP_COUNT[sysNo - 1] = long.Parse(arrParas[intArrNo, 1]);
                    modMain.ADD_TEST_STEP[sysNo - 1] = int.Parse(arrParas[intArrNo, 2]);
                    modMain.ADD_Free_13[sysNo - 1] = float.Parse(arrParas[intArrNo, 3]);
                    modMain.ADD_Free_14[sysNo - 1] = float.Parse(arrParas[intArrNo, 4]);
                    modMain.ADD_Free_15[sysNo - 1] = float.Parse(arrParas[intArrNo, 5]);

                    //78到100行
                    intArrNo = 77;
                    //for (int intHang = 78; intHang <= 100; intHang++)
                    //{
                    //    strline = modMain.strFree.ToString() + "," +
                    //                    modMain.strFree.ToString() + "," +
                    //                    modMain.strFree.ToString() + "," +
                    //                    modMain.strFree.ToString() + "," +
                    //                    modMain.strFree.ToString() + "," +
                    //                    modMain.strFree.ToString() + "," +
                    //                    modMain.strFree.ToString() + "," +
                    //                    modMain.strFree.ToString() + "," +
                    //                    modMain.strFree.ToString() + "," +
                    //                    modMain.strFree.ToString();
                    //}
                    blnFileExist = true;
                }
                else
                {
                    //MessageBox.Show("文件不存在");
                    blnFileExist = false;
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);

            }

            if (blnFileExist && intReadWrite == 1)
            {
                //SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_START, 0, 0);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_STEPS, (int)modMain.cmdType.Write, modMain.PARA_CREEP_STEPS[sysNo - 1], (int)modMain.CtlUnit.No_Unit);

                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CTRL0, (int)modMain.cmdType.Write, (double)modMain.SENSOR.SENSOR_S, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_MODE0, (int)modMain.cmdType.Write, (double)modMain.CtlMode.Ramp, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_V0, (int)modMain.cmdType.Write, modMain.PARA_V0[sysNo - 1], (int)modMain.PARA_V0_Unit[sysNo - 1]);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_P0, (int)modMain.cmdType.Write, modMain.PARA_P0[sysNo - 1], (int)modMain.PARA_P0_Unit[sysNo - 1]);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_T0, (int)modMain.cmdType.Write, modMain.PARA_T0[sysNo - 1], (int)modMain.PARA_T0_Unit[sysNo - 1]);

                for (int intStep = 0; intStep < modMain.PARA_CREEP_STEPS[sysNo - 1]; intStep++)//
                {
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CTRL0 + intStep + 1, (int)modMain.cmdType.Write, modMain.PARA_CTRLn[sysNo - 1, intStep], (int)modMain.CtlUnit.No_Unit);
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_MODE0 + intStep + 1, (int)modMain.cmdType.Write, modMain.PARA_MODEn[sysNo - 1, intStep], (int)modMain.CtlUnit.No_Unit);
                    if (modMain.PARA_MODEn[sysNo - 1, intStep] == 0 || modMain.PARA_MODEn[sysNo - 1, intStep] == 1)
                    {
                        SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_V0 + intStep + 1, (int)modMain.cmdType.Write, modMain.PARA_Vn[sysNo - 1, intStep], modMain.PARA_Vn_Unit[sysNo - 1, intStep]);
                        SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_P0 + intStep + 1, (int)modMain.cmdType.Write, modMain.PARA_Pn[sysNo - 1, intStep], modMain.PARA_Pn_Unit[sysNo - 1, intStep]);
                        SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_T0 + intStep + 1, (int)modMain.cmdType.Write, modMain.PARA_Tn[sysNo - 1, intStep], modMain.PARA_Tn_Unit[sysNo - 1, intStep]);
                    }
                    else
                    {
                        SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_OFF0 + intStep + 1, (int)modMain.cmdType.Write, modMain.PARA_OFFn[sysNo - 1, intStep], modMain.PARA_OFFn_Unit[sysNo - 1, intStep]);
                        SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_AMPL0 + intStep + 1, (int)modMain.cmdType.Write, modMain.PARA_AMPLn[sysNo - 1, intStep], modMain.PARA_AMPLn_Unit[sysNo - 1, intStep]);
                        SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_FREQ0 + intStep + 1, (int)modMain.cmdType.Write, modMain.PARA_FREQn[sysNo - 1, intStep], (int)modMain.CtlUnit.Hz);
                        SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CYCLE0 + intStep + 1, (int)modMain.cmdType.Write, modMain.PARA_CYCLEn[sysNo - 1, intStep], (int)modMain.CtlUnit.No_Unit);
                    }
                }

                if (modMain.CREEP_TEMP[sysNo - 1, 0] < -900)
                {
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_TEMP_SENSOR, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_NOT_ACTIVE, (int)modMain.CtlUnit.No_Unit);
                }
                else
                {
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_TEMP_SENSOR, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CHANNEL_8, (int)modMain.CtlUnit.No_Unit);
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_TEMP0, (int)modMain.cmdType.Write, modMain.CREEP_TEMP[sysNo - 1, 0] - 20, (int)modMain.CtlUnit.C);
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_TEMP_RAMP0, (int)modMain.cmdType.Write, modMain.CREEP_TEMP_RAMP[sysNo - 1, 0], (int)modMain.CtlUnit.C_min);
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_TEMP_WAIT0, (int)modMain.cmdType.Write, 10, (int)modMain.CtlUnit.min);
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_TEMP, (int)modMain.cmdType.Write, modMain.CREEP_TEMP[sysNo - 1, 0], (int)modMain.CtlUnit.C);
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_TEMP_RAMP, (int)modMain.cmdType.Write, modMain.CREEP_TEMP_RAMP[sysNo - 1, 0], (int)modMain.CtlUnit.C_min);
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_TEMP_WAIT, (int)modMain.cmdType.Write, modMain.CREEP_TEMP_WAIT[sysNo - 1, 0], (int)modMain.CtlUnit.min);
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_TEMP_END, (int)modMain.cmdType.Write, modMain.CREEP_TEMP_END[sysNo - 1, 0], (int)modMain.CtlUnit.C);
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_TEMP_DELTA, (int)modMain.cmdType.Write, modMain.CREEP_TEMP_DELTA[sysNo - 1, 0], (int)modMain.CtlUnit.C);
                }

                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_LOOPS, (int)modMain.cmdType.Write, modMain.PARA_LOOPS[sysNo - 1], (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_TEST_TIME, (int)modMain.cmdType.Write, modMain.PARA_CREEP_TEST_TIME[sysNo - 1] * 3600, (int)modMain.CtlUnit.Second);//最大10000天    0*60*60
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_EXTENSION_LIMIT, (int)modMain.cmdType.Write, modMain.CREEP_EXTENSION_LIMIT[sysNo - 1], (int)modMain.CtlUnit.mm);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_REF_TIME, (int)modMain.cmdType.Write, modMain.CREEP_REF_TIME[sysNo - 1], (int)modMain.CtlUnit.min);

                //'时间到，正常结束，返回P0
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_VRETURN_ACTION, (int)modMain.cmdType.Write, modMain.VRETURN_ACTION[sysNo - 1], modMain.VRETURN_ACTION_Unit[sysNo - 1]);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_RETURN_ACTION, (int)modMain.cmdType.Write, modMain.RETURN_ACTION[sysNo - 1], (int)modMain.CtlUnit.No_Unit);

                //'数据采集条件
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_PRINT_TIME_DOUBLE, (int)modMain.cmdType.Write, modMain.PRINT_TIME[sysNo - 1], (int)modMain.CtlUnit.Second);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_PRINT_DS, (int)modMain.cmdType.Write, modMain.PRINT_DS[sysNo - 1], (int)modMain.CtlUnit.mm);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_PRINT_DF, (int)modMain.cmdType.Write, modMain.PRINT_DF[sysNo - 1], (int)modMain.CtlUnit.N);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_PRINT_DE, (int)modMain.cmdType.Write, modMain.PRINT_DE[sysNo - 1], (int)modMain.CtlUnit.mm);

                //'数据显示顺序位置
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_PRINT_V00, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CREEP_TOTAL_TIME, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_PRINT_V01, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CREEP_TEST_TIME, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_PRINT_V02, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CHANNEL_S, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_PRINT_V03, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CHANNEL_F, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_PRINT_V04, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CHANNEL_E, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_PRINT_V05, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CHANNEL_4, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_PRINT_V06, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CHANNEL_5, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_PRINT_V07, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CHANNEL_7, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_PRINT_V08, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CHANNEL_8, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_PRINT_V09, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CHANNEL_9, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_PRINT_V10, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CYCLE_COUNT, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_PRINT_V11, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_LOOP_COUNT, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_PRINT_V12, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CREEP_TEST_STEP, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_PRINT_V13, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CTRLSTATE1, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_PRINT_V14, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_NOT_ACTIVE, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_PRINT_V15, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_NOT_ACTIVE, (int)modMain.CtlUnit.No_Unit);

                //'试验停止返回状态
                if (modMain.END_SENSOR[sysNo - 1] == 255)
                {
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_END_SENSOR, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_NOT_ACTIVE, (int)modMain.CtlUnit.No_Unit);
                }
                else
                {
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_END_SENSOR, (int)modMain.cmdType.Write, modMain.END_SENSOR[sysNo - 1] + (int)DoSA.DoSA_VARIABLE.VAR_CHANNEL_S, (int)modMain.CtlUnit.No_Unit);
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_END_MODE, (int)modMain.cmdType.Write, modMain.END_MODE[sysNo - 1], (int)modMain.CtlUnit.No_Unit);
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_END_VALUE, (int)modMain.cmdType.Write, modMain.END_VALUE[sysNo - 1], modMain.END_VALUE_Unit[sysNo - 1]);
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_END_ACTION, (int)modMain.cmdType.Write, modMain.END_ACTION[sysNo - 1], (int)modMain.CtlUnit.No_Unit);
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_VEND_ACTION, (int)modMain.cmdType.Write, modMain.VEND_ACTION[sysNo - 1], modMain.VEND_ACTION_Unit[sysNo - 1]);
                }

                //'极限保护  +-5000N
                if (modMain.LIMIT_SENSOR[sysNo - 1] == 255)
                {
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_LIMIT_SENSOR, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_NOT_ACTIVE, (int)modMain.CtlUnit.No_Unit);
                }
                else
                {
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_LIMIT_SENSOR, (int)modMain.cmdType.Write, modMain.LIMIT_SENSOR[sysNo - 1] + (int)DoSA.DoSA_VARIABLE.VAR_CHANNEL_S, (int)modMain.CtlUnit.No_Unit);
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_LIMIT_PP, (int)modMain.cmdType.Write, modMain.LIMIT_PP[sysNo - 1], modMain.LIMIT_PP_Unit[sysNo - 1]);
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_LIMIT_MM, (int)modMain.cmdType.Write, modMain.LIMIT_MM[sysNo - 1], modMain.LIMIT_MM_Unit[sysNo - 1]);
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_LIMIT_ACTION, (int)modMain.cmdType.Write, modMain.LIMIT_ACTION[sysNo - 1], (int)modMain.CtlUnit.No_Unit);
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_VLIMIT_ACTION, (int)modMain.cmdType.Write, modMain.VLIMIT_ACTION[sysNo - 1], modMain.VLIMIT_ACTION_Unit[sysNo - 1]);
                }

                //'试验时间显示方式 递增
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_MODE_T, (int)modMain.cmdType.Write, 0, (int)modMain.CtlUnit.No_Unit);

                //'在EDC上显示的变量
                if (modMain.TestType[sysNo - 1] == 0)
                {
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_DSP_SENS1, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CHANNEL_F, (int)modMain.CtlUnit.No_Unit);
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_DSP_SENS2, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CHANNEL_S, (int)modMain.CtlUnit.No_Unit);
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_DSP_SENS3, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CHANNEL_7, (int)modMain.CtlUnit.No_Unit);
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_DSP_SENS4, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CHANNEL_9, (int)modMain.CtlUnit.No_Unit);
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_DSP_SENS5, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CHANNEL_E, (int)modMain.CtlUnit.No_Unit);
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_DSP_SENS6, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CHANNEL_8, (int)modMain.CtlUnit.No_Unit);
                }
                else
                {
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_DSP_SENS1, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CHANNEL_F, (int)modMain.CtlUnit.No_Unit);
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_DSP_SENS2, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CHANNEL_S, (int)modMain.CtlUnit.No_Unit);
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_DSP_SENS3, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CHANNEL_4, (int)modMain.CtlUnit.No_Unit);
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_DSP_SENS4, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CHANNEL_5, (int)modMain.CtlUnit.No_Unit);
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_DSP_SENS5, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CHANNEL_E, (int)modMain.CtlUnit.No_Unit);
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_DSP_SENS6, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CHANNEL_8, (int)modMain.CtlUnit.No_Unit);
                }
            }//if (1=1)
            else if (modMain.KeepTest[sysNo - 1] == 100)
            {
                //SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_START, 0, 0);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_STEPS, (int)modMain.cmdType.Write, 1, (int)modMain.CtlUnit.No_Unit);

                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CTRL0, (int)modMain.cmdType.Write, (double)modMain.SENSOR.SENSOR_S, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_MODE0, (int)modMain.cmdType.Write, (double)modMain.CtlMode.Ramp, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_V0, (int)modMain.cmdType.Write, 1, (int)modMain.CtlUnit.mm_min);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_P0, (int)modMain.cmdType.Write, 500, (int)modMain.CtlUnit.N);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_T0, (int)modMain.cmdType.Write, 10, (int)modMain.CtlUnit.Second);

                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CTRL0 + 1, (int)modMain.cmdType.Write, (double)modMain.SENSOR.SENSOR_F, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_MODE0 + 1, (int)modMain.cmdType.Write, (double)modMain.CtlMode.Ramp, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_V0 + 1, (int)modMain.cmdType.Write, 10, (int)modMain.CtlUnit.N_S);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_P0 + 1, (int)modMain.cmdType.Write, 1000, (int)modMain.CtlUnit.N);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_T0 + 1, (int)modMain.cmdType.Write, 10, (int)modMain.CtlUnit.Second);

                //SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CTRL0 + 2, (int)modMain.cmdType.Write, (double)modMain.SENSOR.SENSOR_F, (int)modMain.CtlUnit.No_Unit);
                //SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_MODE0 + 2, (int)modMain.cmdType.Write, (double)modMain.CtlMode.Ramp, (int)modMain.CtlUnit.No_Unit);
                //SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_V0 + 2, (int)modMain.cmdType.Write, 10, (int)modMain.CtlUnit.N_S);
                //SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_P0 + 2, (int)modMain.cmdType.Write, 400, (int)modMain.CtlUnit.N);
                //SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_T0 + 2, (int)modMain.cmdType.Write, 60, (int)modMain.CtlUnit.Second);

                if (blnFileExist == false)
                {
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_TEMP_SENSOR, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_NOT_ACTIVE, (int)modMain.CtlUnit.No_Unit);
                }
                else
                {
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_TEMP_SENSOR, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CHANNEL_8, (int)modMain.CtlUnit.No_Unit);
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_TEMP0, (int)modMain.cmdType.Write, 80, (int)modMain.CtlUnit.C);
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_TEMP_RAMP0, (int)modMain.cmdType.Write, 10, (int)modMain.CtlUnit.C_min);
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_TEMP_WAIT0, (int)modMain.cmdType.Write, 10, (int)modMain.CtlUnit.min);
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_TEMP, (int)modMain.cmdType.Write, 100, (int)modMain.CtlUnit.C);
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_TEMP_RAMP, (int)modMain.cmdType.Write, 20, (int)modMain.CtlUnit.C_min);
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_TEMP_WAIT, (int)modMain.cmdType.Write, 60, (int)modMain.CtlUnit.min);
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_TEMP_END, (int)modMain.cmdType.Write, 30, (int)modMain.CtlUnit.C);
                    SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_TEMP_DELTA, (int)modMain.cmdType.Write, 3, (int)modMain.CtlUnit.C);
                }

                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_LOOPS, (int)modMain.cmdType.Write, 1, (int)modMain.CtlUnit.No_Unit);

                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_TEST_TIME, (int)modMain.cmdType.Write, 10, (int)modMain.CtlUnit.Second);//最大10000天     0*60*60

                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_EXTENSION_LIMIT, (int)modMain.cmdType.Write, 10, (int)modMain.CtlUnit.mm);

                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_REF_TIME, (int)modMain.cmdType.Write, 30, (int)modMain.CtlUnit.min);

                //'时间到，正常结束，返回P0
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_VRETURN_ACTION, (int)modMain.cmdType.Write, 1, (int)modMain.CtlUnit.mm_min);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_RETURN_ACTION, (int)modMain.cmdType.Write, (double)modMain.Return_Action.return_p0, (int)modMain.CtlUnit.No_Unit);

                //'数据采集条件
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_PRINT_TIME_DOUBLE, (int)modMain.cmdType.Write, 1, (int)modMain.CtlUnit.Second);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_PRINT_DS, (int)modMain.cmdType.Write, 2, (int)modMain.CtlUnit.mm);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_PRINT_DF, (int)modMain.cmdType.Write, 300, (int)modMain.CtlUnit.N);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_PRINT_DE, (int)modMain.cmdType.Write, 4, (int)modMain.CtlUnit.mm);

                //'数据显示顺序位置
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_PRINT_V00, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CREEP_TOTAL_TIME, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_PRINT_V01, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CREEP_TEST_TIME, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_PRINT_V02, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CHANNEL_S, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_PRINT_V03, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CHANNEL_F, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_PRINT_V04, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CHANNEL_E, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_PRINT_V05, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CHANNEL_4, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_PRINT_V06, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CHANNEL_5, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_PRINT_V07, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CHANNEL_7, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_PRINT_V08, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CHANNEL_8, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_PRINT_V09, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CHANNEL_9, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_PRINT_V10, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CYCLE_COUNT, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_PRINT_V11, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_LOOP_COUNT, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_PRINT_V12, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CREEP_TEST_STEP, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_PRINT_V13, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CTRLSTATE1, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_PRINT_V14, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_NOT_ACTIVE, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_PRINT_V15, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_NOT_ACTIVE, (int)modMain.CtlUnit.No_Unit);

                //'试验停止返回状态
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_END_SENSOR, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CHANNEL_F, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_END_MODE, (int)modMain.cmdType.Write, (double)modMain.EndMode.below, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_END_VALUE, (int)modMain.cmdType.Write, 400, (int)modMain.CtlUnit.N);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_END_ACTION, (int)modMain.cmdType.Write, (double)modMain.Return_Action.drive_off, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_VEND_ACTION, (int)modMain.cmdType.Write, 1.1, (int)modMain.CtlUnit.mm_min);

                //'极限保护  +-5000N
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_LIMIT_SENSOR, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CHANNEL_F, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_LIMIT_PP, (int)modMain.cmdType.Write, 5000, (int)modMain.CtlUnit.N);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_LIMIT_MM, (int)modMain.cmdType.Write, -5000, (int)modMain.CtlUnit.N);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_LIMIT_ACTION, (int)modMain.cmdType.Write, (double)modMain.Return_Action.return_p0, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_VLIMIT_ACTION, (int)modMain.cmdType.Write, 1.2, (int)modMain.CtlUnit.mm_min);

                //'试验时间显示方式 递增
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_MODE_T, (int)modMain.cmdType.Write, 0, (int)modMain.CtlUnit.No_Unit);

                //'在EDC上显示的变量
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_DSP_SENS1, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CHANNEL_F, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_DSP_SENS2, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CHANNEL_S, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_DSP_SENS3, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CHANNEL_4, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_DSP_SENS4, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CHANNEL_5, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_DSP_SENS5, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CHANNEL_E, (int)modMain.CtlUnit.No_Unit);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_CREEP_DSP_SENS6, (int)modMain.cmdType.Write, (double)DoSA.DoSA_VARIABLE.VAR_CHANNEL_8, (int)modMain.CtlUnit.No_Unit);
            }//if (1=1)
        }

        ///----------------------------------------------------------------------
        /// <summary>Connect to EDC</summary>
        ///----------------------------------------------------------------------
        private void ConnectToEdcFuncID(int sysNo)
        {
            DoSA.ERROR Error = DoSA.ERROR.INTERNAL;
            //int sysNo = _pipeServer._TransferCmd.FuncID ;

            //Cursor.Current = Cursors.WaitCursor;
            Display("Searching for EDCs. Please wait...");

            // make a new myDoSAall class
            //modMain.MeDoSAall = new DoSAall();

            DoSA.DoSAState MeState = new DoSA.DoSAState();
            if (modMain.MeDoSA[sysNo - 1] != null)
            {//DoSAHdl自动赋值
                Error = modMain.MeDoSA[sysNo - 1].GetState(ref MeState);
                if (Error == DoSA.ERROR.NOERROR)
                {
                    if (MeState.ComState == (int)DoSA.DoSA_STATE.DoSA_STATE_ONLINE)
                    {
                        modMain.MeDoSA[sysNo - 1].CloseLink();
                    }
                }
            }
            else
            {
                modMain.MeDoSA[sysNo - 1] = new DoSA();
            }
            if (GlobeVal.myconfigfile.mode == (int)modMain.CtlerType.EDC220)
            {
                Error = modMain.MeDoSA[sysNo - 1].OpenFunctionID(sysNo, modMain.DoSAVersionEDC220);
            }
            else if (GlobeVal.myconfigfile.mode == (int)modMain.CtlerType.EDCi15)
            {
                Error = modMain.MeDoSA[sysNo - 1].OpenFunctionID(sysNo, modMain.DoSAVersionEDCi15);
            }

            if (Error == DoSA.ERROR.NOERROR)
            {
                Display("FunctionID:" + sysNo.ToString());
                //myDoSA = modMain.MeDoSA[sysNo - 1];//DoSAHdl已经赋值
            }
            else
            { // ERROR: no EDC found
              // make a new myDoSA class
                modMain.MeDoSA[sysNo - 1] = new DoSA();
                //myDoSA = new DoSA();
                // show error
                Display(string.Format("ERROR {0}: {1}", (int)Error, Error));
            }
            //Cursor.Current = Cursors.Default;
        }

        ///----------------------------------------------------------------------
        /// <summary>Connect to EDC</summary>
        ///----------------------------------------------------------------------
        private void ConnectToEdcAll()
        {
            DoSA.ERROR Error = DoSA.ERROR.INTERNAL;
            //Cursor.Current = Cursors.WaitCursor;
            Display("Searching for EDCs. Please wait...");
            // make a new myDoSAall class
            //myDoSAall = new DoSAall();

            for (int sysNo = 0; sysNo < GlobeVal.myconfigfile.machinecount; sysNo++)
            {
                if (modMain.MeDoSA[sysNo].DoSAHdl.ToInt32() != 0)
                {
                    Error = modMain.MeDoSAall.CloseLink(ref modMain.MeDoSA[sysNo].DoSAHdl);
                    Display(string.Format("ERROR {0}: {1}", (int)Error, Error));
                    modMain.MeDoSA[sysNo] = new DoSA();
                }
            }

            // search for all connected USB/LAN EDCs (DoSA API Version)
            if (GlobeVal.myconfigfile.mode == (int)modMain.CtlerType.EDC220)
            {
                Error = modMain.MeDoSAall.OpenAll(modMain.DoSAVersionEDC220);
            }
            else if (GlobeVal.myconfigfile.mode == (int)modMain.CtlerType.EDCi15)
            {
                Error = modMain.MeDoSAall.OpenAll(modMain.DoSAVersionEDCi15);
            }
            if (Error == DoSA.ERROR.NOERROR)
            { // EDCs found
                for (int i = 0; i < modMain.MeDoSAall.InfoTableValidEntries; i++)
                { // show module info of connected EDCs
                    Display(string.Format("    {0}  DeviceID:{1:X8}  FunctionID:{2}  Serial:{3}  State:{4}",
                            modMain.MeDoSAall.InfoTable[i].ModuleInfo.Name, modMain.MeDoSAall.InfoTable[i].ModuleInfo.DeviceID,
                            modMain.MeDoSAall.InfoTable[i].ModuleInfo.FunctionID,
                            modMain.MeDoSAall.InfoTable[i].ModuleInfo.SerNr, modMain.MeDoSAall.InfoTable[i].ModuleInfo.Status));
                    for (int sysNo = 0; sysNo < GlobeVal.myconfigfile.machinecount; sysNo++)
                    {
                        if (modMain.MeDoSAall.InfoTable[i].ModuleInfo.FunctionID == sysNo + 1)
                        {
                            modMain.MeDoSA[sysNo] = new DoSA(modMain.MeDoSAall.InfoTable[i].DoSAHdl);
                            Display("Using found EDC FunctionID:" + modMain.MeDoSAall.InfoTable[i].ModuleInfo.FunctionID);
                            //myDoSA = modMain.MeDoSA[sysNo];
                        }
                    }
                }

                // make a new myDoSA class and use DoSAHdl of first found EDC
                //myDoSA = new DoSA(myDoSAall.InfoTable[0].DoSAHdl);
                //Display("Using first found EDC");
            }
            else
            { // ERROR: no EDC found
              // make a new myDoSA class
              //myDoSA = new DoSA();
              // show error
                Display(string.Format("ERROR {0}: {1}", (int)Error, Error));
            }
            //Cursor.Current = Cursors.Default;
        }

        private void CloseLinkToEdcFuncID(int sysNo)
        {
            DoSA.ERROR Error = DoSA.ERROR.INTERNAL;
            //int sysNo = _pipeServer._TransferCmd.FuncID;

            //Cursor.Current = Cursors.WaitCursor;
            Display("Searching for EDCs. Please wait...");

            // make a new myDoSAall class
            //modMain.MeDoSAall = new DoSAall();

            DoSA.DoSAState MeState = new DoSA.DoSAState();
            if (modMain.MeDoSA[sysNo - 1] != null)
            {//DoSAHdl自动赋值
                Error = modMain.MeDoSA[sysNo - 1].GetState(ref MeState);
                if (Error == DoSA.ERROR.NOERROR)
                {
                    if (MeState.ComState == (int)DoSA.DoSA_STATE.DoSA_STATE_ONLINE)
                    {
                        modMain.MeDoSA[sysNo - 1].CloseLink();
                    }
                }
            }
            else
            {
                modMain.MeDoSA[sysNo - 1] = new DoSA();
            }
            if (modMain.blnStartTest[sysNo - 1] == true)//试验脱机
            {
                modMain.blnStartTest[sysNo - 1] = false;
            }
        }

        ///----------------------------------------------------------------------
        /// <summary>Send command EXT_CMD_EDC_ON to EDC</summary>
        ///----------------------------------------------------------------------
        private void DriveOn()
        {
            if (GlobeVal.myconfigfile.mode == (int)modMain.CtlerType.EDC220 || GlobeVal.myconfigfile.mode == (int)modMain.CtlerType.EDCi15)
            {
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_EDC_ON, 0, 0);
            }
        }

        ///----------------------------------------------------------------------
        /// <summary>Send command EXT_CMD_EDC_OFF to EDC</summary>
        ///----------------------------------------------------------------------
        private void DriveOff()
        {
            if (GlobeVal.myconfigfile.mode == (int)modMain.CtlerType.EDC220 || GlobeVal.myconfigfile.mode == (int)modMain.CtlerType.EDCi15)
            {
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_EDC_OFF, 0, 0);
            }
        }

        ///----------------------------------------------------------------------
        /// <summary>Send command EXT_CMD_START to EDC</summary>
        ///----------------------------------------------------------------------
        private void TestStart()
        {
            modMain.TimePass[_pipeServer._TransferCmd.FuncID - 1] = 0;
            string strName;
            //strName = modMain.ParaFile[_pipeServer._TransferCmd.FuncID - 1];
            //if (File.Exists(strName))
            //{
            //    File.Move(strName, strName + "_" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss"));
            //}
            strName = modMain.DataFile[_pipeServer._TransferCmd.FuncID - 1];
            if (File.Exists(strName))
            {
                File.Move(strName, strName + "_" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss"));
            }
            strName = modMain.RecoFile[_pipeServer._TransferCmd.FuncID - 1];
            if (File.Exists(strName))
            {
                File.Move(strName, strName + "_" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss"));
            }
            strName = modMain.TempFile[_pipeServer._TransferCmd.FuncID - 1];
            if (File.Exists(strName))
            {
                File.Move(strName, strName + "_" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss"));
            }
            if (GlobeVal.myconfigfile.mode == (int)modMain.CtlerType.EDC220 || GlobeVal.myconfigfile.mode == (int)modMain.CtlerType.EDCi15)
            {
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_CLEAR_BUFFER, 0, 0);
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_START, 0, 0);
            }
            else if (GlobeVal.myconfigfile.mode == (int)modMain.CtlerType.SIM)
            {
                Demo.readdemo(Application.StartupPath + @"\demo\计算演示1.txt");

            }
            modMain.KeepTest[_pipeServer._TransferCmd.FuncID - 1] = 1;
            writeFile(0, modMain.strKeepfile, "", (int)modMain.FileType.KeepFile);
        }

        ///----------------------------------------------------------------------
        /// <summary>Send command EXT_CMD_RETURN to EDC</summary>
        ///----------------------------------------------------------------------
        private void TestEnd()
        {
            modMain.KeepTest[_pipeServer._TransferCmd.FuncID - 1] = 0;
            writeFile(0, modMain.strKeepfile, "", (int)modMain.FileType.KeepFile);
            if (GlobeVal.myconfigfile.mode == (int)modMain.CtlerType.EDC220 || GlobeVal.myconfigfile.mode == (int)modMain.CtlerType.EDCi15)
            {
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_STOP, 0, 0);
            }
            else if (GlobeVal.myconfigfile.mode == (int)modMain.CtlerType.SIM)
            {
                Demo.mdemoline[_pipeServer._TransferCmd.FuncID - 1] = 0;
                Demo.mdemo[_pipeServer._TransferCmd.FuncID - 1] = false;

            }
        }

        private void TestHalt()
        {
            if (GlobeVal.myconfigfile.mode == (int)modMain.CtlerType.EDC220 || GlobeVal.myconfigfile.mode == (int)modMain.CtlerType.EDCi15)
            {
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_HALT, (int)modMain.cmdType.Write, 0);
            }
        }

        private void TestContinue()
        {
            modMain.TimePass[_pipeServer._TransferCmd.FuncID - 1] = 0;
            if (GlobeVal.myconfigfile.mode == (int)modMain.CtlerType.EDC220 || GlobeVal.myconfigfile.mode == (int)modMain.CtlerType.EDCi15)
            {
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_CONTINUE, (int)modMain.cmdType.Write, 0);
            }
        }

        private void GET_STATE()
        {
            if (GlobeVal.myconfigfile.mode == (int)modMain.CtlerType.EDC220 || GlobeVal.myconfigfile.mode == (int)modMain.CtlerType.EDCi15)
            {
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_GET_EDC_STATE, (int)modMain.cmdType.Read, 0);
            }
        }

        private void GET_BUFFER()
        {
            if (GlobeVal.myconfigfile.mode == (int)modMain.CtlerType.EDC220 || GlobeVal.myconfigfile.mode == (int)modMain.CtlerType.EDCi15)
            {
                SendExtCmd(DoSA.DoSA_EXT_CMD.EXT_CMD_GET_USED_BUFFER, (int)modMain.cmdType.Read, 0);
            }
        }

        private void GET_CMD_BIT()
        {
            if (GlobeVal.myconfigfile.mode == (int)modMain.CtlerType.EDC220 || GlobeVal.myconfigfile.mode == (int)modMain.CtlerType.EDCi15)
            {
                //' Cmd;Read;device_0;set_0;reset_0;flash_0
                string CmdStr = "109;1;0;0x0000;0x0000;0x0000";
                Display("EXT_CMD_BIT: " + CmdStr);
                if (modMain.MeDoSA[_pipeServer._TransferCmd.FuncID - 1] != null)
                {
                    if (modMain.MeDoSA[_pipeServer._TransferCmd.FuncID - 1].DoSAHdl.ToInt32() != 0)
                    {
                        // send command string to EDC
                        modMain.MeDoSA[_pipeServer._TransferCmd.FuncID - 1].WriteMessage(CmdStr);
                    }
                }
            }
        }

        public Form1()
        {
            InitializeComponent();
            _ctr = 1;
            _pipeClient = new PipeClient();
            _pipeServer = new PipeServer();
            _pipeServer.PipeMessage += new PipeServer.DelegateMessage(PipesMessageHandler);
            if (QueryPerformanceFrequency(out clockFrequency) == false)
            {
                // Frequency not supported
                throw new Win32Exception("QueryPerformanceFrequency() function is not supported");
            }
            GlobeVal.myconfigfile = new ConfigFile();
            GlobeVal.myconfigfile = GlobeVal.myconfigfile.DeSerializeNow(Application.StartupPath + @"\sys\系统设置.ini");
            _TransferData.init();
            Interval = 10;
            thread = new Thread(new ThreadStart(ThreadProc));
            thread.Name = "HighAccuracyTimer";
            thread.Priority = ThreadPriority.AboveNormal;
        }

        const int WM_SYSCOMMAND = 0x112;
        const int SC_CLOSE = 0xF060;
        const int SC_MINIMIZE = 0xF020;
        const int SC_MAXIMIZE = 0xF030;
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_SYSCOMMAND)
            {
                if (m.WParam.ToInt32() == SC_MINIMIZE)
                {
                    this.ShowInTaskbar = false;
                    this.notifyIcon1.Icon = this.Icon;
                    this.Hide();
                    return;
                }
            }
            base.WndProc(ref m);
        }

        private void cmdSend_Click(object sender, EventArgs e)
        {
            //timer1.Enabled = true;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)//当用户点击窗体右上角X按钮或(Alt + F4)时 发生          
            {
                e.Cancel = true;
                this.ShowInTaskbar = false;
                this.notifyIcon1.Icon = this.Icon;
                this.Hide();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (Directory.Exists(modMain.strEDCfile) == false)//如果不存在就创建文件夹
            {
                Directory.CreateDirectory(modMain.strEDCfile);
            }
            //GlobeVal.myconfigfile = new ConfigFile();
            //GlobeVal.myconfigfile = GlobeVal.myconfigfile.DeSerializeNow(Application.StartupPath + @"\sys\系统设置.ini");            
            ReadConfigFileAndInit();
            ReadTestFileAndContinue();
            _pipeServer.Listen("TestPipe1");
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            //Application.DoEvents();
            if (GlobeVal.myconfigfile.mode == (int)modMain.CtlerType.SIM)
            {
                for (int i = 0; i < GlobeVal.myconfigfile.machinecount; i++)
                {
                    if ( Demo.mdemo[i] == true )
                    {
                        if (GlobeVal.myconfigfile.SimulationMode[i] == 2)
                        {
                            _TransferData.FuncID[i] = Convert.ToInt16(dataGridView1.Rows[1].Cells[i + 1].Value);

                            _TransferData.EDC_STATE[i] = Convert.ToInt16(dataGridView1.Rows[2].Cells[i + 1].Value);

                            _TransferData.ControlValue[i] = Convert.ToInt16(dataGridView1.Rows[3].Cells[i + 1].Value);

                            _TransferData.CHANNEL_F[i] = Demo.mdemosindata[Demo.mdemoline[i]].load;

                            _TransferData.CHANNEL_S[i] = Demo.mdemosindata[Demo.mdemoline[i]].pos;

                            _TransferData.CHANNEL_4[i] = 0;

                            _TransferData.CHANNEL_5[i] = 0;

                            _TransferData.CHANNEL_E[i] = Demo.mdemosindata[Demo.mdemoline[i]].ext;

                            _TransferData.Unbalancedness[i] = Convert.ToSingle(dataGridView1.Rows[9].Cells[i + 1].Value);

                            _TransferData.TemperatureControl[i] = Convert.ToSingle(dataGridView1.Rows[10].Cells[i + 1].Value);

                            _TransferData.CHANNEL_7[i] = Convert.ToSingle(dataGridView1.Rows[11].Cells[i + 1].Value);

                            _TransferData.CHANNEL_8[i] = Convert.ToSingle(dataGridView1.Rows[12].Cells[i + 1].Value);

                            _TransferData.CHANNEL_9[i] = Convert.ToSingle(dataGridView1.Rows[13].Cells[i + 1].Value);

                            _TransferData.TemperatureGradient[i] = Convert.ToSingle(dataGridView1.Rows[14].Cells[i + 1].Value);

                            _TransferData.TOTAL_TIME[i] = Convert.ToSingle(dataGridView1.Rows[15].Cells[i + 1].Value);

                            _TransferData.CYCLE_COUNT[i] =Demo.mdemocount[i];

                            _TransferData.LOOP_COUNT[i] = Convert.ToInt64(dataGridView1.Rows[17].Cells[i + 1].Value);

                            _TransferData.TOTAL_TIME[i] = Environment.TickCount / 1000.0;
                            _TransferData.TEST_TIME[i] = Environment.TickCount / 1000.0;
                            if (Environment.TickCount / 1000.0 - Demo.mdemotime[i] >= Demo.mdemosindata[Demo.mdemoline[i]].time)
                            {
                                Demo.mdemoline[i] = Demo.mdemoline[i] + 1;

                                if (Demo.mdemoline[i] > Demo.mdemosindata.Count - 1)
                                {
                                    Demo.mdemoline[i] = 0;
                                    Demo.mdemocount[i] = Demo.mdemocount[i] + 1;

                                }

                                Demo.mdemotime[i] = Environment.TickCount / 1000.0;
                            }

                        }

                        if (GlobeVal.myconfigfile.SimulationMode[i] == 0)
                        {
                            _TransferData.FuncID[i] = Convert.ToInt16(dataGridView1.Rows[1].Cells[i + 1].Value);

                            _TransferData.EDC_STATE[i] = Convert.ToInt16(dataGridView1.Rows[2].Cells[i + 1].Value);

                            _TransferData.ControlValue[i] = Convert.ToInt16(dataGridView1.Rows[3].Cells[i + 1].Value);

                            _TransferData.CHANNEL_F[i] = Demo.mdemodata[Demo.mdemoline[i]].load;

                            _TransferData.CHANNEL_S[i] = Demo.mdemodata[Demo.mdemoline[i]].pos;

                            _TransferData.CHANNEL_4[i] = Demo.mdemodata[Demo.mdemoline[i]].ext;

                            _TransferData.CHANNEL_5[i] = Demo.mdemodata[Demo.mdemoline[i]].ext;

                            _TransferData.CHANNEL_E[i] = Demo.mdemodata[Demo.mdemoline[i]].ext;

                            _TransferData.Unbalancedness[i] = Convert.ToSingle(dataGridView1.Rows[9].Cells[i + 1].Value);

                            _TransferData.TemperatureControl[i] = Convert.ToSingle(dataGridView1.Rows[10].Cells[i + 1].Value);

                            _TransferData.CHANNEL_7[i] = Convert.ToSingle(dataGridView1.Rows[11].Cells[i + 1].Value);

                            _TransferData.CHANNEL_8[i] = Convert.ToSingle(dataGridView1.Rows[12].Cells[i + 1].Value);

                            _TransferData.CHANNEL_9[i] = Convert.ToSingle(dataGridView1.Rows[13].Cells[i + 1].Value);

                            _TransferData.TemperatureGradient[i] = Convert.ToSingle(dataGridView1.Rows[14].Cells[i + 1].Value);

                            _TransferData.TOTAL_TIME[i] = Convert.ToSingle(dataGridView1.Rows[15].Cells[i + 1].Value);

                            _TransferData.CYCLE_COUNT[i] = Convert.ToInt64(dataGridView1.Rows[16].Cells[i + 1].Value);

                            _TransferData.LOOP_COUNT[i] = Convert.ToInt64(dataGridView1.Rows[17].Cells[i + 1].Value);

                            _TransferData.TOTAL_TIME[i] = Environment.TickCount / 1000.0;
                            _TransferData.TEST_TIME[i] = Demo.mdemodata[Demo.mdemoline[i]].time;
                            if (Environment.TickCount / 1000.0 - Demo.mdemotime[i] >= Demo.mdemodata[Demo.mdemoline[i]].time)
                            {
                                Demo.mdemoline[i] = Demo.mdemoline[i] + 1;
                            }
                            if (Demo.mdemoline[i] > Demo.mdemodata.Count - 1)
                            {
                                Demo.mdemo[i] = false;
                            }
                        }
                    }


                    else
                    {

                        _TransferData.FuncID[i] = Convert.ToInt16(dataGridView1.Rows[1].Cells[i + 1].Value);

                        _TransferData.EDC_STATE[i] = Convert.ToInt16(dataGridView1.Rows[2].Cells[i + 1].Value);

                        _TransferData.ControlValue[i] = Convert.ToInt16(dataGridView1.Rows[3].Cells[i + 1].Value);

                        _TransferData.CHANNEL_F[i] = Convert.ToSingle(dataGridView1.Rows[4].Cells[i + 1].Value) + Convert.ToSingle(_rd.NextDouble()/10.0);

                        _TransferData.CHANNEL_S[i] = Convert.ToSingle(dataGridView1.Rows[5].Cells[i + 1].Value) + Convert.ToSingle(_rd.NextDouble()/10.0);

                        _TransferData.CHANNEL_4[i] = Convert.ToSingle(dataGridView1.Rows[6].Cells[i + 1].Value) + Convert.ToSingle(_rd.NextDouble()/10.0);

                        _TransferData.CHANNEL_5[i] = Convert.ToSingle(dataGridView1.Rows[7].Cells[i + 1].Value) + Convert.ToSingle(_rd.NextDouble()/10.0);

                        _TransferData.CHANNEL_E[i] = Convert.ToSingle(dataGridView1.Rows[8].Cells[i + 1].Value) + Convert.ToSingle(_rd.NextDouble()/10.0);

                        _TransferData.Unbalancedness[i] = Convert.ToSingle(dataGridView1.Rows[9].Cells[i + 1].Value);

                        _TransferData.TemperatureControl[i] = Convert.ToSingle(dataGridView1.Rows[10].Cells[i + 1].Value);

                        _TransferData.CHANNEL_7[i] = Convert.ToSingle(dataGridView1.Rows[11].Cells[i + 1].Value);

                        _TransferData.CHANNEL_8[i] = Convert.ToSingle(dataGridView1.Rows[12].Cells[i + 1].Value);

                        _TransferData.CHANNEL_9[i] = Convert.ToSingle(dataGridView1.Rows[13].Cells[i + 1].Value);

                        _TransferData.TemperatureGradient[i] = Convert.ToSingle(dataGridView1.Rows[14].Cells[i + 1].Value);

                        _TransferData.TOTAL_TIME[i] = Convert.ToSingle(dataGridView1.Rows[15].Cells[i + 1].Value);

                        _TransferData.CYCLE_COUNT[i] = Convert.ToInt64(dataGridView1.Rows[16].Cells[i + 1].Value);

                        _TransferData.LOOP_COUNT[i] = Convert.ToInt64(dataGridView1.Rows[17].Cells[i + 1].Value);

                        _TransferData.TOTAL_TIME[i] = Environment.TickCount / 1000.0;
                        _TransferData.TEST_TIME[i] = Environment.TickCount / 1000.0;

                    }



                }



            }
            else if (GlobeVal.myconfigfile.mode == (int)modMain.CtlerType.EDCi15 || GlobeVal.myconfigfile.mode == (int)modMain.CtlerType.EDC220)
            {
                string CmdStr;
                for (int sysNo = 0; sysNo < GlobeVal.myconfigfile.machinecount; sysNo++)
                {
                    if (modMain.MeDoSA[sysNo] != null)
                    {
                        if (modMain.MeDoSA[sysNo].DoSAHdl.ToInt32() != 0)
                        {
                            CmdStr = "107;1;0;0";//GET_USED_BUFFER
                            modMain.MeDoSA[sysNo].WriteMessage(CmdStr);
                            CmdStr = "102;1;0;0";//GET_EDC_STATE
                            modMain.MeDoSA[sysNo].WriteMessage(CmdStr);
                            CmdStr = "109;1;0;0x0000;0x0000;0x0000";//EXT_CMD_BIT
                            modMain.MeDoSA[sysNo].WriteMessage(CmdStr);

                        }//if (modMain.MeDoSA[sysNo].DoSAHdl.ToInt32() != 0)
                    }//if (modMain.MeDoSA[sysNo - 1] != null)
                }
            }

        }

        private void _测试(object sender, EventArgs e)
        {
            _TransferData.CHANNEL_F[0] = 3;

            byte[] a = StructTOBytes(_TransferData);

            _TransferData = ByteToTransferData<TransferData>(a);

            MessageBox.Show(_TransferData.CHANNEL_F[0].ToString());

        }

        private void Form1_MinimumSizeChanged(object sender, EventArgs e)
        {

        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.Visible = true;
                this.WindowState = FormWindowState.Normal;
                this.BringToFront();
            }
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定要退出程序?", "安全提示",
               System.Windows.Forms.MessageBoxButtons.YesNo,
               System.Windows.Forms.MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.Yes)
            {
                notifyIcon1.Visible = false;  //设置图标不可见
                thread.Abort();
                this.Close();                 //关闭窗体
                this.Dispose();               //释放资源
                Application.Exit();           //关闭应用程序窗体
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            _pipeClient = null;
            _pipeServer.PipeMessage -= new PipeServer.DelegateMessage(PipesMessageHandler);
            _pipeServer = null;
            running = false;
            thread.Abort();
            Application.Exit();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            this.toolStripStatusLabel1.Image = imageList2.Images[1];
            thread.Start();
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            Form2 f = new Form2();
            f.ShowDialog();

            if (GlobeVal.myconfigfile.mode == (int)modMain.CtlerType.EDC220 || GlobeVal.myconfigfile.mode == (int)modMain.CtlerType.EDCi15)
            {
                tmrEDC.Enabled = true;
            }

            dataGridView1.Columns.Clear();
            DataGridViewColumn b = new DataGridViewColumn();
            b.Name = "项目";
            b.HeaderText = "项目";
            DataGridViewCell dgvcell = new DataGridViewTextBoxCell();
            b.CellTemplate = dgvcell;
            b.Frozen = true;
            dataGridView1.Columns.Add(b);
            for (int i = 0; i < GlobeVal.myconfigfile.machinecount; i++)
            {
                b = new DataGridViewColumn();
                b.HeaderText = "主机" + (i + 1).ToString().Trim();
                dgvcell = new DataGridViewTextBoxCell();
                b.CellTemplate = dgvcell;
                dataGridView1.Columns.Add(b);
            }

            string[] sname = new string[20];
            sname[0] = "编号";
            sname[1] = "类型";
            sname[2] = "状态";
            sname[3] = "控制值";
            sname[4] = "负荷[N]";
            sname[5] = "位移[mm]";
            sname[6] = "变形A[mm]";
            sname[7] = "变形B[mm]";
            sname[8] = "平均变形[mm]";
            sname[9] = "不平衡度[%]";
            sname[10] = "控温[℃]";
            sname[11] = "上段温度";
            sname[12] = "中段温度";
            sname[13] = "下段温度";
            sname[14] = "温度梯度";
            sname[15] = "试验时间[s]";
            sname[16] = "波形个数";
            sname[17] = "循环次数";
            dataGridView1.Rows.Clear();
            for (int i = 0; i <= 17; i++)
            {
                string[] mt = new string[GlobeVal.myconfigfile.machinecount + 1];
                mt[0] = sname[i];
                mt[1] = (i + 1).ToString();
                mt[2] = (i + 3).ToString();
                if ((i==4) || (i==5)||(i==6)||(i==7)||(i==8))
                {
                    mt[1] = "0";
                    mt[2] = "0";
                }
                dataGridView1.Rows.Add(mt);
            }
            timer1.Enabled = true;
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {

        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {

        }

        private void tmrEDC_Tick(object sender, EventArgs e)
        {
            //Application.DoEvents();
            //int sysNo1 = 0;
            //string strLog = "0.5,1.1,0.011,0.013,0.012,500.1,500.2,500.3,0.001,1.001";            
            //string strName = "D:\\" + "specimen" +"_" + (sysNo1 + 1).ToString() + "_" + (modMain.LineNumber[sysNo1] / 1000).ToString() + "." + string.Format("{0:000}", sysNo1 + 1) + "D";//100行一个文件
            //modMain.WriteDataFile(strName, modMain.LineNumber[sysNo1].ToString() + "," + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "," + strLog);
            //modMain.LineNumber[sysNo1]++;

            //if (processMessage() == DoSA.ERROR.NOERROR)
            //{ // message received -> set short timer interval to increase poll speed
            //    tmrEDC.Interval = 10;
            //}
            //else
            //{ // no message received -> set long timer interval
            //    //tmrEDC.Interval = 200;
            //}
            // send command to EDC to get new data
            //SendExtCmdGetData();
            string CmdStr = "106;1;0;0";
            //Display("GetData: " + CmdStr);
            // send command string to EDC
            for (int sysNo = 0; sysNo < GlobeVal.myconfigfile.machinecount; sysNo++)
            {
                //判断多长时间连接中断，中断后不采集数据，10秒
                modMain.intPipeErrorNum[sysNo]++;
                if (modMain.intPipeErrorNum[sysNo] > 1000)
                {
                    modMain.intPipeErrorNum[sysNo] = 1000;
                    modMain.blnPipeConnectOK[sysNo] = false;
                }

                if (modMain.MeDoSA[sysNo] != null)
                {
                    if (modMain.MeDoSA[sysNo].DoSAHdl.ToInt32() != 0)
                    {
                        if (modMain.blnStartTest[sysNo])
                        {
                            if (processMessage(sysNo) == DoSA.ERROR.NOERROR)
                            {

                            }
                            //send command to EDC to get new data
                            modMain.MeDoSA[sysNo].WriteMessage(CmdStr);
                        }
                    }//if (modMain.MeDoSA[sysNo].DoSAHdl.ToInt32() != 0)
                }//if (modMain.MeDoSA[sysNo - 1] != null)
            }
        }

        ///----------------------------------------------------------------------
        /// <summary>Read message from EDC and process received message</summary>
        ///----------------------------------------------------------------------
        private DoSA.ERROR processMessage(int sysNo)//for (int sysNo = 0; sysNo < GlobeVal.myconfigfile.machinecount; sysNo++)
        {
            string Text = "";
            string buf = "";
            string strEDC = "";
            double[] ReadEDCData = new double[modMain.MAX_SENSORS];
            DoSA.ERROR Error = modMain.MeDoSA[sysNo].ReadMessage(ref Text);
            if (Error == DoSA.ERROR.NOERROR)
            { // process received message
                string[] items = Text.Split(new char[] { ';' }, StringSplitOptions.None);
                if (items.Length >= 1)
                {
                    int cmd = Convert.ToInt32(items[0]);
                    switch ((DoSA.DoSA_EXT_CMD)cmd)
                    {
                        case DoSA.DoSA_EXT_CMD.EXT_CMD_GET_EDC_STATE:
                            //'/*0*/ EDC_STATE_NOT_READY,                     /* EDC is not ready         */
                            //'/*1*/ EDC_STATE_OFF,                           /* EDC is OFF               */
                            //'/*2*/ EDC_STATE_ON,                            /* EDC is ON                */
                            //'/*3*/ EDC_STATE_TEST,                          /* EDC is in TEST mode      */            
                            //'/*8*/ EDC_STATE_AlertDef,                      /* EDC is AlertDef          */
                            Display("EXT_CMD_GET_EDC_STATE: " + Text);
                            if (items[2] != "")
                            {
                                modMain.EDC_STATE[sysNo] = Convert.ToInt32(items[2]);
                            }
                            else
                            {
                                modMain.EDC_STATE[sysNo] = 0;
                            }
                            _TransferData.FuncID[sysNo] = Convert.ToInt16(sysNo + 1);
                            _TransferData.EDC_STATE[sysNo] = Convert.ToInt16(modMain.EDC_STATE[sysNo]);
                            break;
                        case DoSA.DoSA_EXT_CMD.EXT_CMD_BIT:
                            Display("EXT_CMD_BIT: " + Text);
                            if (items[2] != "")
                            {
                                modMain.CMD_BIT[sysNo] = Convert.ToInt32(items[2]);
                            }
                            else
                            {
                                modMain.CMD_BIT[sysNo] = 0;
                            }
                            _TransferData.FuncID[sysNo] = Convert.ToInt16(sysNo + 1);
                            _TransferData.CMD_BIT[sysNo] = Convert.ToInt16(modMain.CMD_BIT[sysNo]);
                            break;
                        //AlertDefChao(SysIndex) = Mid(DecimalToBinary(DStr), 15, 1) '变形         
                        //AlertTestStop(SysIndex) = Mid(DecimalToBinary(DStr), 16, 1) '结束
                        case DoSA.DoSA_EXT_CMD.EXT_CMD_PARA_VRETURN_ACTION:
                            Display("EXT_CMD_PARA_VRETURN_ACTION: " + Text);

                            strEDC = modMain.strEDCfile + DateTime.Now.ToString("yyyy_MM_dd") + "." + "EDC";
                            buf = "FuncID=" + string.Format("{0:000}", sysNo + 1) + "," + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "," + Text;
                            writeFile(sysNo, strEDC, buf, (int)modMain.FileType.EDCiFile);
                            break;
                        case DoSA.DoSA_EXT_CMD.EXT_CMD_GET_USED_BUFFER:
                            Display("EXT_CMD_GET_USED_BUFFER: " + Text);
                            if (items[2] != "")
                            {
                                modMain.USED_BUFFER[sysNo] = Convert.ToInt32(items[2]);
                            }
                            else
                            {
                                modMain.USED_BUFFER[sysNo] = 0;
                            }
                            _TransferData.FuncID[sysNo] = Convert.ToInt16(sysNo + 1);
                            _TransferData.USED_BUFFER[sysNo] = Convert.ToSingle(modMain.USED_BUFFER[sysNo]);
                            break;
                        // process new data from EDC
                        case DoSA.DoSA_EXT_CMD.EXT_CMD_NEW_DATA:
                            // show first 5 values
                            int count;
                            count = modMain.MAX_SENSORS;
                            int idx = Convert.ToInt32(items[1]);
                            buf = "";

                            switch ((DoSA.DoSA_EXT_CMD_IDX)idx)
                            {
                                case DoSA.DoSA_EXT_CMD_IDX.EXT_CMD_IDX_SPECIMEN:
                                    //'new specimen
                                    //  ClearData SysIndex
                                    Display(string.Format("Speciment:{0}  TestNo:{1}", items[2], items[3]));
                                    break;
                                case DoSA.DoSA_EXT_CMD_IDX.EXT_CMD_IDX_TEST_NAME:
                                    Display(string.Format("TestName:{0}", items[2]));
                                    break;

                                case DoSA.DoSA_EXT_CMD_IDX.EXT_CMD_IDX_DATA_NAME:
                                    buf = "";
                                    for (int i = 0; i < count; i++)
                                    {
                                        buf += string.Format("DataName{0}:{1}  ", i, items[2 + i]);
                                        modMain.SensorName[sysNo, i] = items[2 + i];
                                    }
                                    Display(buf);
                                    break;

                                case DoSA.DoSA_EXT_CMD_IDX.EXT_CMD_IDX_DATA_UNIT:
                                    buf = "";
                                    for (int i = 0; i < count; i++)
                                    {
                                        buf += string.Format("DataUnit{0}:{1}  ", i, items[2 + i]);
                                    }
                                    Display(buf);
                                    break;

                                case DoSA.DoSA_EXT_CMD_IDX.EXT_CMD_IDX_DATA_VALUE:
                                    double[] Add_Value = new double[16];
                                    for (int i = 0; i < 16; i++)
                                    {
                                        Add_Value[i] = 0;
                                    }
                                    Add_Value[0] = modMain.ADD_TOTAL_TIME[sysNo];
                                    Add_Value[1] = modMain.ADD_TEST_TIME[sysNo];
                                    Add_Value[2] = modMain.ADD_CHANNEL_S[sysNo];
                                    Add_Value[3] = modMain.ADD_CHANNEL_F[sysNo];
                                    Add_Value[4] = modMain.ADD_CHANNEL_E[sysNo];
                                    Add_Value[5] = modMain.ADD_CHANNEL_4[sysNo];
                                    Add_Value[6] = modMain.ADD_CHANNEL_5[sysNo];
                                    Add_Value[7] = modMain.ADD_CHANNEL_7[sysNo];
                                    Add_Value[8] = modMain.ADD_CHANNEL_8[sysNo];
                                    Add_Value[9] = modMain.ADD_CHANNEL_9[sysNo];
                                    Add_Value[10] = modMain.ADD_CYCLE_COUNT[sysNo];
                                    Add_Value[11] = modMain.ADD_LOOP_COUNT[sysNo];
                                    Add_Value[12] = modMain.ADD_TEST_STEP[sysNo];
                                    Add_Value[13] = modMain.ADD_Free_13[sysNo];
                                    Add_Value[14] = modMain.ADD_Free_14[sysNo];
                                    Add_Value[15] = modMain.ADD_Free_15[sysNo];
                                    string strLog = "";
                                    buf = "";
                                    for (int i = 0; i < count; i++)
                                    {
                                        buf += string.Format("DataValue{0}:{1}  ", i, items[2 + i]);

                                        if (items[2 + i] != "")
                                        {
                                            ReadEDCData[i] = Convert.ToDouble(items[2 + i]);
                                        }
                                        else
                                        {
                                            ReadEDCData[i] = 0.0;
                                        }
                                        ReadEDCData[i] = ReadEDCData[i] + Add_Value[i];
                                        //if (i < count - 1)
                                        //{
                                        strLog += ReadEDCData[i].ToString() + ",";
                                        //}
                                        //else
                                        //{
                                        //    strLog += ReadEDCData[i].ToString();
                                        //}
                                    }

                                    //calculate
                                    //_TransferData.ControlValue[sysNo] = Convert.ToInt16(dataGridView1.Rows[3].Cells[sysNo + 1].Value);
                                    if (ReadEDCData[1] <= Add_Value[1])
                                    {
                                        _TransferData.ControlValue[sysNo] = modMain.PARA_P0[sysNo];
                                    }
                                    else
                                    {
                                        if (modMain.PARA_MODEn[sysNo, (int)(ReadEDCData[12] - 1)] == 0)
                                        {
                                            _TransferData.ControlValue[sysNo] = modMain.PARA_Pn[sysNo, (int)(ReadEDCData[12] - 1)];
                                        }
                                        else if (modMain.PARA_MODEn[sysNo, (int)(ReadEDCData[12] - 1)] == 1)
                                        {
                                            if (Convert.ToInt16(ReadEDCData[12]) - _TransferData.TEST_STEP[sysNo] == 1)
                                            {
                                                switch (modMain.PARA_CTRLn[sysNo, (int)(ReadEDCData[12] - 1)])
                                                {
                                                    case 0:
                                                        _TransferData.ControlValue[sysNo] = Convert.ToSingle(ReadEDCData[2]);
                                                        break;
                                                    case 1:
                                                        _TransferData.ControlValue[sysNo] = Convert.ToSingle(ReadEDCData[3]);
                                                        break;
                                                    case 2:
                                                        _TransferData.ControlValue[sysNo] = Convert.ToSingle(ReadEDCData[4]);
                                                        break;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            _TransferData.ControlValue[sysNo] = modMain.PARA_OFFn[sysNo, (int)(ReadEDCData[12] - 1)];
                                        }
                                    }
                                    //_TransferData.unbalancedness[sysNo] = Convert.ToSingle(ReadEDCData[i]);
                                    if ((ReadEDCData[5] + ReadEDCData[6]) != 0 && (modMain.TestType[sysNo] == 1 || modMain.TestType[sysNo] == 2) &&
                                            (ReadEDCData[1] > Add_Value[1]))
                                    {
                                        _TransferData.Unbalancedness[sysNo] = (float)Math.Abs(Math.Abs(ReadEDCData[5] - ReadEDCData[6]) * 100 / (ReadEDCData[5] + ReadEDCData[6]));
                                    }
                                    else
                                    {
                                        _TransferData.Unbalancedness[sysNo] = 0;
                                    }
                                    //_TransferData.Temperaturecontrol[sysNo] = Convert.ToSingle(ReadEDCData[i]);
                                    _TransferData.TemperatureControl[sysNo] = modMain.CREEP_TEMP[sysNo, 0];
                                    //_TransferData.TemperatureGradient[sysNo] = Convert.ToSingle(ReadEDCData[i]);
                                    float MAWEN = 0;
                                    float MIWEN = 0;
                                    MIWEN = (float)ReadEDCData[7];
                                    MAWEN = (float)ReadEDCData[7];
                                    if (ReadEDCData[8] <= MIWEN)
                                    {
                                        MIWEN = (float)ReadEDCData[8];
                                    }
                                    if (ReadEDCData[9] <= MIWEN)
                                    {
                                        MIWEN = (float)ReadEDCData[9];
                                    }
                                    if (ReadEDCData[8] >= MAWEN)
                                    {
                                        MAWEN = (float)ReadEDCData[8];
                                    }
                                    if (ReadEDCData[9] >= MAWEN)
                                    {
                                        MAWEN = (float)ReadEDCData[9];
                                    }
                                    _TransferData.TemperatureGradient[sysNo] = Math.Abs(MAWEN - MIWEN);

                                    //save file and send data
                                    strLog = modMain.LineNumber[sysNo].ToString() + "," +
                                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "," +
                                        strLog +
                                        modMain.strFree.ToString() + "," +
                                        modMain.strFree.ToString() + "," +
                                        modMain.strFree.ToString() + "," +
                                        modMain.strFree.ToString() + "," +
                                        (sysNo + 1).ToString() + "," +
                                        _TransferData.ControlValue[sysNo].ToString() + "," +
                                        _TransferData.Unbalancedness[sysNo].ToString() + "," +
                                        _TransferData.TemperatureControl[sysNo].ToString() + "," +
                                        _TransferData.TemperatureGradient[sysNo].ToString() + "," +
                                        modMain.EDC_STATE[sysNo].ToString() + "," +
                                        modMain.CMD_BIT[sysNo].ToString() + "," +
                                        modMain.USED_BUFFER[sysNo].ToString() + "," +
                                        modMain.TEST_END[sysNo].ToString() + "," +
                                        modMain.strFree.ToString();

                                    float sngTime;//单位是秒                                 
                                    string strName;
                                    //strName = "D:\\" + "specimen" + "_" + (sysNo + 1).ToString() + "_" + (modMain.LineNumber[sysNo] / 1000).ToString()
                                    //+ "." + string.Format("{0:000}", sysNo + 1) + "D";//1000行一个文件

                                    strName = modMain.DataFile[sysNo];
                                    sngTime = modMain.UnitValue_Load_Value(modMain.PARA_CTRLn[sysNo, 0], modMain.PARA_MODEn[sysNo, 0],
                                                modMain.PARA_Pn[sysNo, 0], modMain.PARA_Pn_Unit[sysNo, 0], modMain.PARA_Vn[sysNo, 0], modMain.PARA_Vn_Unit[sysNo, 0]);
                                    if (modMain.SaveMode[sysNo] == 1 ||
                                        (modMain.TestType[sysNo] != 3 && modMain.PARA_MODEn[sysNo, 0] == 0 && ReadEDCData[1] > Add_Value[1] && ReadEDCData[1] < Add_Value[1] + 60 + sngTime))
                                    {
                                        writeFile(sysNo, strName, strLog, (int)modMain.FileType.DataFile);
                                        modMain.LineNumber[sysNo]++;
                                    }
                                    else if (modMain.SaveMode[sysNo] == 0 && modMain.SaveTimeStep[sysNo] >= 2)//5 step
                                    {
                                        if (ReadEDCData[1] <= Add_Value[1])
                                        {
                                            if ((ReadEDCData[0] - modMain.TimePass[sysNo]) >=
                                                            modMain.Unit_TimeInv(float.Parse(modMain.SaveTimeInv[sysNo, 0]), modMain.SaveTimeInvUnit[sysNo, 0]))
                                            {
                                                writeFile(sysNo, strName, strLog, (int)modMain.FileType.DataFile);
                                                modMain.LineNumber[sysNo]++;
                                                modMain.TimePass[sysNo] = ReadEDCData[0];
                                            }
                                        }
                                        else
                                        {
                                            for (int intStep = 1; intStep < modMain.SaveTimeStep[sysNo]; intStep++)
                                            {
                                                if (ReadEDCData[1] >= modMain.Unit_TimeInv(float.Parse(modMain.SaveTimeFrom[sysNo, intStep]), modMain.SaveTimeFromToUnit[sysNo, intStep]) &&
                                                    ReadEDCData[1] <= modMain.Unit_TimeInv(float.Parse(modMain.SaveTimeTo[sysNo, intStep]), modMain.SaveTimeFromToUnit[sysNo, intStep]))
                                                {
                                                    if ((ReadEDCData[0] - modMain.TimePass[sysNo]) >=
                                                            modMain.Unit_TimeInv(float.Parse(modMain.SaveTimeInv[sysNo, intStep]), modMain.SaveTimeInvUnit[sysNo, intStep]))
                                                    {
                                                        writeFile(sysNo, strName, strLog, (int)modMain.FileType.DataFile);
                                                        modMain.LineNumber[sysNo]++;
                                                        modMain.TimePass[sysNo] = ReadEDCData[0];
                                                    }
                                                }
                                            }
                                            //  >SaveTimeTo
                                            if (ReadEDCData[1] > modMain.Unit_TimeInv(float.Parse(modMain.SaveTimeTo[sysNo, modMain.SaveTimeStep[sysNo] - 1]),
                                                                                        modMain.SaveTimeFromToUnit[sysNo, modMain.SaveTimeStep[sysNo] - 1]))
                                            {
                                                if ((ReadEDCData[0] - modMain.TimePass[sysNo]) >=
                                                        modMain.Unit_TimeInv(float.Parse(modMain.SaveTimeInv[sysNo, modMain.SaveTimeStep[sysNo] - 1]),
                                                                                        modMain.SaveTimeInvUnit[sysNo, modMain.SaveTimeStep[sysNo] - 1]))
                                                {
                                                    writeFile(sysNo, strName, strLog, (int)modMain.FileType.DataFile);
                                                    modMain.LineNumber[sysNo]++;
                                                    modMain.TimePass[sysNo] = ReadEDCData[0];
                                                }
                                            }
                                        }
                                    }
                                    else if (modMain.SaveMode[sysNo] == 2)//2=每隔设定的波形存储一个完整波形
                                    {

                                    }
                                    else if (modMain.SaveMode[sysNo] == 3)//3=每隔设定的循环存储一个完整循环
                                    {

                                    }
                                    //'数据文件顺序位置，共32个数据，用逗号隔开。  LineNumber行号 + DateTime当前日期时间 + _TransferData的30个变量                                 
                                    _TransferData.TOTAL_TIME[sysNo] = Convert.ToDouble(ReadEDCData[0]);
                                    _TransferData.TEST_TIME[sysNo] = Convert.ToSingle(ReadEDCData[1]);
                                    _TransferData.CHANNEL_S[sysNo] = Convert.ToSingle(ReadEDCData[2]);
                                    _TransferData.CHANNEL_F[sysNo] = Convert.ToSingle(ReadEDCData[3]);
                                    _TransferData.CHANNEL_E[sysNo] = Convert.ToSingle(ReadEDCData[4]);
                                    _TransferData.CHANNEL_4[sysNo] = Convert.ToSingle(ReadEDCData[5]);
                                    _TransferData.CHANNEL_5[sysNo] = Convert.ToSingle(ReadEDCData[6]);
                                    _TransferData.CHANNEL_7[sysNo] = Convert.ToSingle(ReadEDCData[7]);
                                    _TransferData.CHANNEL_8[sysNo] = Convert.ToSingle(ReadEDCData[8]);
                                    _TransferData.CHANNEL_9[sysNo] = Convert.ToSingle(ReadEDCData[9]);
                                    _TransferData.CYCLE_COUNT[sysNo] = Convert.ToInt64(ReadEDCData[10]);
                                    _TransferData.LOOP_COUNT[sysNo] = Convert.ToInt64(ReadEDCData[11]);
                                    _TransferData.TEST_STEP[sysNo] = Convert.ToInt16(ReadEDCData[12]);
                                    _TransferData.Free_13[sysNo] = Convert.ToSingle(ReadEDCData[13]);
                                    _TransferData.Free_14[sysNo] = Convert.ToSingle(ReadEDCData[14]);
                                    _TransferData.Free_15[sysNo] = Convert.ToSingle(ReadEDCData[15]);
                                    _TransferData.Free_16[sysNo] = Convert.ToSingle(ReadEDCData[15]);
                                    _TransferData.Free_17[sysNo] = Convert.ToSingle(ReadEDCData[15]);
                                    _TransferData.Free_18[sysNo] = Convert.ToSingle(ReadEDCData[15]);
                                    _TransferData.Free_19[sysNo] = Convert.ToSingle(ReadEDCData[15]);
                                    _TransferData.FuncID[sysNo] = Convert.ToInt16(sysNo + 1);
                                    //_TransferData.ControlValue[sysNo] = Convert.ToInt16(dataGridView1.Rows[3].Cells[sysNo + 1].Value);
                                    //_TransferData.Unbalancedness[sysNo] = Convert.ToSingle(ReadEDCData[i]);
                                    //_TransferData.TemperatureControl[sysNo] = Convert.ToSingle(ReadEDCData[i]);
                                    //_TransferData.TemperatureGradient[sysNo] = Convert.ToSingle(ReadEDCData[i]);
                                    _TransferData.EDC_STATE[sysNo] = Convert.ToInt16(modMain.EDC_STATE[sysNo]);
                                    _TransferData.CMD_BIT[sysNo] = Convert.ToInt16(modMain.CMD_BIT[sysNo]);
                                    _TransferData.USED_BUFFER[sysNo] = Convert.ToSingle(modMain.USED_BUFFER[sysNo]);
                                    _TransferData.TEST_END[sysNo] = Convert.ToInt16(modMain.TEST_END[sysNo]);
                                    _TransferData.Free_29[sysNo] = Convert.ToSingle(ReadEDCData[15]);
                                    //show data
                                    dataGridView1.Rows[0].Cells[sysNo + 1].Value = _TransferData.FuncID[sysNo];
                                    switch (modMain.TestType[sysNo])
                                    {
                                        case 0:
                                            dataGridView1.Rows[1].Cells[sysNo + 1].Value = "持久试验";
                                            break;
                                        case 1:
                                            dataGridView1.Rows[1].Cells[sysNo + 1].Value = "蠕变试验";
                                            break;
                                        case 2:
                                            dataGridView1.Rows[1].Cells[sysNo + 1].Value = "松弛试验";
                                            break;
                                        case 3:
                                            dataGridView1.Rows[1].Cells[sysNo + 1].Value = "慢速拉伸";
                                            break;
                                    }
                                    dataGridView1.Rows[2].Cells[sysNo + 1].Value = _TransferData.EDC_STATE[sysNo];
                                    dataGridView1.Rows[3].Cells[sysNo + 1].Value = _TransferData.ControlValue[sysNo];
                                    dataGridView1.Rows[4].Cells[sysNo + 1].Value = _TransferData.CHANNEL_F[sysNo];
                                    dataGridView1.Rows[5].Cells[sysNo + 1].Value = _TransferData.CHANNEL_S[sysNo];
                                    dataGridView1.Rows[6].Cells[sysNo + 1].Value = _TransferData.CHANNEL_4[sysNo];
                                    dataGridView1.Rows[7].Cells[sysNo + 1].Value = _TransferData.CHANNEL_5[sysNo];
                                    dataGridView1.Rows[8].Cells[sysNo + 1].Value = _TransferData.CHANNEL_E[sysNo];
                                    dataGridView1.Rows[9].Cells[sysNo + 1].Value = _TransferData.Unbalancedness[sysNo];
                                    if (modMain.CREEP_TEMP[sysNo, 0] < -900)
                                    {
                                        dataGridView1.Rows[10].Cells[sysNo + 1].Value = "常温";
                                    }
                                    else
                                    {
                                        dataGridView1.Rows[10].Cells[sysNo + 1].Value = _TransferData.TemperatureControl[sysNo];
                                    }
                                    dataGridView1.Rows[11].Cells[sysNo + 1].Value = _TransferData.CHANNEL_7[sysNo];
                                    dataGridView1.Rows[12].Cells[sysNo + 1].Value = _TransferData.CHANNEL_8[sysNo];
                                    dataGridView1.Rows[13].Cells[sysNo + 1].Value = _TransferData.CHANNEL_9[sysNo];
                                    dataGridView1.Rows[14].Cells[sysNo + 1].Value = _TransferData.TemperatureGradient[sysNo];
                                    dataGridView1.Rows[15].Cells[sysNo + 1].Value = _TransferData.TOTAL_TIME[sysNo];
                                    dataGridView1.Rows[16].Cells[sysNo + 1].Value = _TransferData.CYCLE_COUNT[sysNo];
                                    dataGridView1.Rows[17].Cells[sysNo + 1].Value = _TransferData.LOOP_COUNT[sysNo];

                                    Display(buf);
                                    break;
                                case DoSA.DoSA_EXT_CMD_IDX.EXT_CMD_IDX_TEST_END:
                                    //if Sys1RunTime(SysIndex) >= PARA_CREEP_TEST_TIME(SysIndex) - 0.02 Then
                                    //    Sys1Error(SysIndex) = "O"
                                    //    WriteRec(SysIndex) = True
                                    //    filenum = FreeFile()
                                    //    Open ResFile1(SysIndex) For Append As #filenum
                                    //    Write #filenum, Date$, Time$, "试验时间到，试验结束"
                                    //    Close #filenum
                                    //    WriteRec(SysIndex) = False
                                    //    ChaoXian = "试验时间到，试验结束" & vbLf
                                    //Else
                                    //    Sys1Error(SysIndex) = "S"
                                    //    WriteRec(SysIndex) = True
                                    //    filenum = FreeFile()
                                    //    Open ResFile1(SysIndex) For Append As #filenum
                                    //    Write #filenum, Date$, Time$, "触发检测条件，试验结束"
                                    //    Close #filenum
                                    //    WriteRec(SysIndex) = False
                                    //    ChaoXian = "触发检测条件，试验结束" & vbLf
                                    //End if                                    
                                    modMain.TEST_END[sysNo] = 1;
                                    _TransferData.FuncID[sysNo] = Convert.ToInt16(sysNo + 1);
                                    _TransferData.TEST_END[sysNo] = Convert.ToInt16(modMain.TEST_END[sysNo]);
                                    Display("试验时间到，试验结束: " + Text);

                                    writeFile(sysNo, modMain.DataFile[sysNo], buf, (int)modMain.FileType.StopFile);
                                    buf = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "," + "EXT_CMD_IDX_TEST_END停止试验" + "," + Text;
                                    writeFile(sysNo, modMain.RecoFile[sysNo], buf, (int)modMain.FileType.RecoFile);

                                    strEDC = modMain.strEDCfile + DateTime.Now.ToString("yyyy_MM_dd") + "." + "EDC";
                                    buf = "FuncID=" + string.Format("{0:000}", sysNo + 1) + "," + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "," + Text;
                                    writeFile(sysNo, strEDC, buf, (int)modMain.FileType.EDCiFile);

                                    modMain.KeepTest[sysNo] = 0;
                                    writeFile(0, modMain.strKeepfile, "", (int)modMain.FileType.KeepFile);
                                    break;
                                case DoSA.DoSA_EXT_CMD_IDX.EXT_CMD_IDX_TEST_STOP:
                                    //Write #filenum, Date$, Time$, "试验由EDC异常停止"
                                    modMain.TEST_END[sysNo] = 2;
                                    _TransferData.FuncID[sysNo] = Convert.ToInt16(sysNo + 1);
                                    _TransferData.TEST_END[sysNo] = Convert.ToInt16(modMain.TEST_END[sysNo]);
                                    Display("试验由EDC异常停止: " + Text);

                                    writeFile(sysNo, modMain.DataFile[sysNo], buf, (int)modMain.FileType.StopFile);
                                    buf = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "," + "EXT_CMD_IDX_TEST_STOP停止试验" + "," + Text;
                                    writeFile(sysNo, modMain.RecoFile[sysNo], buf, (int)modMain.FileType.RecoFile);

                                    strEDC = modMain.strEDCfile + DateTime.Now.ToString("yyyy_MM_dd") + "." + "EDC";
                                    buf = "FuncID=" + string.Format("{0:000}", sysNo + 1) + "," + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "," + Text;
                                    writeFile(sysNo, strEDC, buf, (int)modMain.FileType.EDCiFile);

                                    modMain.KeepTest[sysNo] = 0;
                                    writeFile(0, modMain.strKeepfile, "", (int)modMain.FileType.KeepFile);
                                    break;
                                case DoSA.DoSA_EXT_CMD_IDX.EXT_CMD_IDX_SERIES_END:
                                    //Write #filenum, Date$, Time$, "试验由EDC异常停止"
                                    modMain.TEST_END[sysNo] = 2;
                                    _TransferData.FuncID[sysNo] = Convert.ToInt16(sysNo + 1);
                                    _TransferData.TEST_END[sysNo] = Convert.ToInt16(modMain.TEST_END[sysNo]);
                                    Display("试验由EDC异常停止: " + Text);

                                    writeFile(sysNo, modMain.DataFile[sysNo], buf, (int)modMain.FileType.StopFile);
                                    buf = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "," + "EXT_CMD_IDX_SERIES_END停止试验" + "," + Text;
                                    writeFile(sysNo, modMain.RecoFile[sysNo], buf, (int)modMain.FileType.RecoFile);

                                    strEDC = modMain.strEDCfile + DateTime.Now.ToString("yyyy_MM_dd") + "." + "EDC";
                                    buf = "FuncID=" + string.Format("{0:000}", sysNo + 1) + "," + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "," + Text;
                                    writeFile(sysNo, strEDC, buf, (int)modMain.FileType.EDCiFile);

                                    modMain.KeepTest[sysNo] = 0;
                                    writeFile(0, modMain.strKeepfile, "", (int)modMain.FileType.KeepFile);
                                    break;
                                // __ToDo__ process all other messages...
                                default:
                                    Display("EDC->PC: " + Text);

                                    strEDC = modMain.strEDCfile + DateTime.Now.ToString("yyyy_MM_dd") + "." + "EDC";
                                    buf = "FuncID=" + string.Format("{0:000}", sysNo + 1) + "," + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "," + Text;
                                    writeFile(sysNo, strEDC, buf, (int)modMain.FileType.EDCiFile);
                                    break;
                            }
                            break;

                        // __ToDo__ process all other messages...
                        default:
                            Display("EDC->PC: " + Text);//text="3;19;Limit"  超过负荷上限值EXT_CMD_PARA_LIMIT_PP

                            strEDC = modMain.strEDCfile + DateTime.Now.ToString("yyyy_MM_dd") + "." + "EDC";
                            buf = "FuncID=" + string.Format("{0:000}", sysNo + 1) + "," + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "," + Text;
                            writeFile(sysNo, strEDC, buf, (int)modMain.FileType.EDCiFile);
                            break;
                    }
                }
            }
            return Error;
        }

        private void writeFile(int sysNo, string strFileName, string strMessages, int FileType)//sysNo从0开始
        {
            string strLog = "";
            strLog = strLog +
                            modMain.LineNumber[sysNo].ToString() + "," +
                            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "," +
                            _TransferData.TOTAL_TIME[sysNo].ToString() + "," +
                            _TransferData.TEST_TIME[sysNo].ToString() + "," +
                            _TransferData.CHANNEL_S[sysNo].ToString() + "," +
                            _TransferData.CHANNEL_F[sysNo].ToString() + "," +
                            _TransferData.CHANNEL_E[sysNo].ToString() + "," +
                            _TransferData.CHANNEL_4[sysNo].ToString() + "," +
                            _TransferData.CHANNEL_5[sysNo].ToString() + "," +
                            _TransferData.CHANNEL_7[sysNo].ToString() + "," +
                            _TransferData.CHANNEL_8[sysNo].ToString() + "," +
                            _TransferData.CHANNEL_9[sysNo].ToString() + "," +
                            _TransferData.CYCLE_COUNT[sysNo].ToString() + "," +
                            _TransferData.LOOP_COUNT[sysNo].ToString() + "," +
                            _TransferData.TEST_STEP[sysNo].ToString() + "," +
                            _TransferData.Free_13[sysNo].ToString() + "," +
                            _TransferData.Free_14[sysNo].ToString() + "," +
                            _TransferData.Free_15[sysNo].ToString() + "," +
                            _TransferData.Free_16[sysNo].ToString() + "," +
                            _TransferData.Free_17[sysNo].ToString() + "," +
                            _TransferData.Free_18[sysNo].ToString() + "," +
                            _TransferData.Free_19[sysNo].ToString() + "," +
                            _TransferData.FuncID[sysNo].ToString() + "," +
                            _TransferData.ControlValue[sysNo].ToString() + "," +
                            _TransferData.Unbalancedness[sysNo].ToString() + "," +
                            _TransferData.TemperatureControl[sysNo].ToString() + "," +
                            _TransferData.TemperatureGradient[sysNo].ToString() + "," +
                            _TransferData.EDC_STATE[sysNo].ToString() + "," +
                            _TransferData.CMD_BIT[sysNo].ToString() + "," +
                            _TransferData.USED_BUFFER[sysNo].ToString() + "," +
                            _TransferData.TEST_END[sysNo].ToString() + "," +
                            _TransferData.Free_29[sysNo].ToString();
            switch (FileType)
            {
                case (int)modMain.FileType.DataFile:
                case (int)modMain.FileType.RecoFile:
                    using (StreamWriter wf = File.AppendText(strFileName))
                    {
                        wf.WriteLine(strMessages);
                    }
                    if (FileType == (int)modMain.FileType.DataFile)
                    {
                        goto case (int)modMain.FileType.TempFile;
                    }
                    break;
                case (int)modMain.FileType.StopFile://DataFile  end test
                    using (StreamWriter wf = File.AppendText(strFileName))
                    {
                        wf.WriteLine(strLog);
                    }
                    break;
                case (int)modMain.FileType.EDCiFile:
                    if (Directory.Exists(modMain.strEDCfile) == false)//如果不存在就创建文件夹
                    {
                        Directory.CreateDirectory(modMain.strEDCfile);
                    }
                    using (StreamWriter wf = File.AppendText(strFileName))
                    {
                        wf.WriteLine(strMessages);
                        wf.WriteLine(strLog);
                    }
                    break;
                case (int)modMain.FileType.KeepFile://每行sysno，keeptest，paraName
                    strLog = "";
                    for (int i = 0; i < GlobeVal.myconfigfile.machinecount; i++)
                    {
                        if (i < GlobeVal.myconfigfile.machinecount - 1)
                        {
                            strLog = strLog + (i + 1).ToString() + "," + modMain.KeepTest[i].ToString() + "," + modMain.ParaFile[i] + Environment.NewLine;
                        }
                        else
                        {
                            strLog = strLog + (i + 1).ToString() + "," + modMain.KeepTest[i].ToString() + "," + modMain.ParaFile[i];
                        }
                    }
                    using (StreamWriter wf = File.CreateText(modMain.strKeepfile))
                    {
                        wf.WriteLine(strLog);
                    }
                    break;
                case (int)modMain.FileType.TempFile://2行每行32个，第一行是temp，第2行是实时数据。
                    string strLogTemp = "";
                    strLogTemp = strLogTemp + modMain.strFree + "," + modMain.strFree + "," + modMain.strFree + "," + modMain.strFree + "," + modMain.strFree + ","
                                            + modMain.strFree + "," + modMain.strFree + "," + modMain.strFree + "," + modMain.strFree + "," + modMain.strFree + ","
                                            + modMain.strFree + "," + modMain.strFree + "," + modMain.strFree + "," + modMain.strFree + "," + modMain.strFree + ","
                                            + modMain.strFree + "," + modMain.strFree + "," + modMain.strFree + "," + modMain.strFree + "," + modMain.strFree + ","
                                            + modMain.strFree + "," + modMain.strFree + "," + modMain.strFree + "," + modMain.strFree + "," + modMain.strFree + ","
                                            + modMain.strFree + "," + modMain.strFree + "," + modMain.strFree + "," + modMain.strFree + "," + modMain.strFree + ","
                                            + modMain.strFree + "," + modMain.strFree + Environment.NewLine;
                    strLogTemp = strLogTemp + strLog;
                    using (StreamWriter wf = File.CreateText(modMain.TempFile[sysNo]))
                    {
                        wf.WriteLine(strLogTemp);
                    }
                    break;
            }//switch
        }

        private string GetName(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";

            if (str.Length > 0)
            {
                if (str.LastIndexOf("/") > 0)
                {
                    str = str.Substring(str.LastIndexOf("/") + 1);
                }
                else if (str.LastIndexOf("\\") > 0)
                {
                    str = str.Substring(str.LastIndexOf("\\") + 1);
                }
            }
            return str;
        }

        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            /*
            一个函数搞定，都不用去考虑递归（以前居然不知道），太强大了。
            string[] files = System.IO.Directory.GetFiles(_dir, "*.*", System.IO.SearchOption.AllDirectories);
            System.IO.SearchOption.AllDirectories表示搜索本文件夹和所有子目录，很碉堡吧。
            "*.*"也可以是"*"，都一样。
            通配符，支持 *，?
            string[] files = System.IO.Directory.GetDirectories(_dir, "*川*", System.IO.SearchOption.AllDirectories);
            结果files包含文件夹和文件。
            */
            string dir = @"D:\CCSS\试验数据\";
            string[] files = System.IO.Directory.GetFiles(dir, "*.??C", System.IO.SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                if (GetName(files[i]).Contains("700") == true)//文件名过滤      CreationTime返回文件的创建时间
                {
                    FileInfo fi = new FileInfo(files[i]);
                    if (DateTime.Compare(Convert.ToDateTime(fi.LastWriteTime), Convert.ToDateTime("2018-11-19 00:00")) > 0 &&
                        DateTime.Compare(Convert.ToDateTime(fi.LastWriteTime), Convert.ToDateTime("2018-11-20 00:00")) < 0)//文件修改日期  
                    {
                        this.listBox1.Items.Add(files[i]);
                        this.listBox1.Items.Add(fi.LastWriteTime);
                    }
                }

            }


            //最小二乘法线性拟合方程
            //double[] x = new double[] { 10000, 4000, 2000, 1000, 500, 200 };
            //double[] y = new double[] { 100, 200, 300, 400, 500, 600 };
            //string strResult = "";
            //// double[,] xArray;
            //double[] ratio;
            //double[] yy = new double[y.Length];
            //Console.WriteLine("一次拟合：");
            //ratio = FittingFunct.Linear(y, x);
            //foreach (double num in ratio)
            //{
            //    //Console.WriteLine(num);
            //    strResult += num.ToString() + "     ";
            //}
            //for (int i = 0; i < x.Length; i++)
            //{
            //    yy[i] = ratio[0] + ratio[1] * x[i];
            //}
            ////Console.WriteLine("R²=: " + FittingFunct.Pearson(y, yy) + "\r\n");
            //strResult += FittingFunct.Pearson(y, yy).ToString() + "     ";
            //Display(strResult);

            //string strName = @"D:\CCSS\试验数据\data1.01D";
            //modMain.TempFile[1] = @"D:\CCSS\试验数据\data1.01F";
            //modMain.LineNumber[1] = 123;
            //writeFile(1, strName, "1,2,3,4,5", (int)modMain.FileType.DataFile);
            //modMain.LineNumber[1] = 0;
            //openTempFile(1, modMain.TempFile[1]);

            //string strName = @"D:\CCSS\试验数据\ZS187088R南方科技大学\2018年11月26日 160656\700.01D";
            //openDataFile(1, strName);

            //ReadTestFileAndContinue();
            //string buf="";
            //for (int j = 0; j < 10; j++)
            //{
            //    if (j < 9)
            //    {
            //        buf = buf + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "," + j.ToString() + Environment.NewLine;
            //    }
            //    else
            //    {
            //        buf = buf + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "," + j.ToString() ;
            //    }

            //}
            //writeFile(1, modMain.strKeepfile, buf, (int)modMain.FileType.KeepFile);

            //int ii = 0;
            //string strName = @"D:\CCSS\试验数据\data1.01Data";
            //using (StreamWriter wf = File.CreateText(@strName))
            //{
            //    for (int j = 0; j < 10; j++)
            //    {
            //        ii++;
            //        wf.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "," + ii.ToString());
            //    }
            //}

            //GlobeVal.mysys = new ClassSys();
            //if (File.Exists(System.Windows.Forms.Application.StartupPath + "\\AppleLabJ" + "\\sys\\setup.ini") == true)
            //{
            //    GlobeVal.mysys = GlobeVal.mysys.DeSerializeNow(System.Windows.Forms.Application.StartupPath + "\\AppleLabJ" + "\\sys\\setup.ini");
            //}


            //int xx = 3;
            //switch (xx)
            //{
            //    case 0:  case 1:
            //        xx=1;
            //        break;
            //    case 2: case 3:                
            //        xx = 20;
            //        break;
            //}

            //string fileDir = DateTime.Now.ToString("yyyy_MM_dd");
            //fileDir = fileDir + "D";
            //if (Directory.Exists(@"D:\EDC") == false)//如果不存在就创建file文件夹
            //{
            //    Directory.CreateDirectory(@"D:\EDC");
            //}

            //// 获取启动了应用程序的可执行文件的路径。
            //string fileDir = System.Windows.Forms.Application.StartupPath;
            //this.toolStripStatusLabel3.Text = fileDir;

            //获取当前运行程序的目录
            //string fileDir = Environment.CurrentDirectory;
            //fileDir = fileDir + "D";
            //fileDir = fileDir + "E";
            //this.toolStripStatusLabel3.Text = fileDir;

            //string strName = @"D:\CCSS\试验数据\data1.01C";          
            //if (File.Exists(strName))
            //{
            //    File.Move(strName, strName + "_" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss"));
            //}

            //string ss= "001_Creep_Para";
            //string a=ss.Substring(0,ss.Length - 4) + "Data";

            //int ii = 0;
            //string strName = @"D:\CCSS\试验数据\data1.01C";
            //using (StreamWriter wf = File.AppendText(@strName))
            //{                
            //    for (int j = 0; j < 10; j++)
            //    {
            //        ii++;
            //        wf.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")+"," + ii.ToString());
            //    }
            //}
            //string[] lines = File.ReadAllLines(@strName);
            //int row = lines.GetLength(0); //行数
            //string[] cols = lines[0].Split(',');
            //int col = cols.GetLength(0);  //每行数据的个数
            //string[,] p1 = new string[row, col]; //数组
            //for (int i = 0; i < row; i++)  //读入数据并赋予数组
            //{
            //    string[] data = lines[i].Split(',');
            //    for (int j = 0; j < col; j++)
            //    {
            //        p1[i, j] = (data[j]);
            //    }
            //}
        }

        ///----------------------------------------------------------------------
        /// 读取试验TempFile文件
        ///----------------------------------------------------------------------
        private void openTempFile(int sysNo, string strFileName)//sysNo从0开始
        {
            //读取文件           
            try
            {
                if (File.Exists(strFileName))
                {
                    string[] lines = File.ReadAllLines(strFileName);
                    int row = lines.GetLength(0); //行数
                    string[] cols = lines[0].Split(',');
                    int col = cols.GetLength(0);  //每行数据的个数  
                    string[,] arrParas = new string[row, col]; //数组
                    for (int i = 0; i < row; i++)  //读入数据并赋予数组
                    {
                        string[] data = lines[i].Split(',');
                        for (int j = 0; j < col; j++)
                        {
                            arrParas[i, j] = (data[j]);
                        }
                    }
                    //赋值
                    //modMain.LineNumber[1] = int.Parse(arrParas[1, 0]);
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);               
            }
        }

        ///----------------------------------------------------------------------
        /// 读取试验数据文件
        ///----------------------------------------------------------------------
        private void openDataFile(int sysNo, string strFileName)//sysNo从0开始,readtemp--LineNumber
        {
            string currentLine;
            //string[,] strData = new string[7457568,18] ;//只取行，在数据处理中在分解行。
            //ArrayList listData = new ArrayList();
            int idx = 0;
            DateTime startTime = DateTime.Now;
            if (File.Exists(strFileName))
            {
                using (StreamReader rf = File.OpenText(strFileName))
                {
                    //if ((++idx % 10000) == 0)                   
                    while ((currentLine = rf.ReadLine()) != null)
                    {
                        Application.DoEvents();
                        idx++;
                        //listData.Add(currentLine);
                        //string[] data = currentLine.Split(',');
                        //strData[idx - 1, 0] = data[0];
                        //strData[idx - 1, 1] = data[1];
                        //strData[idx - 1, 2] = data[2];
                        //strData[idx - 1, 3] = data[3];
                        //strData[idx - 1, 4] = data[0];
                        //strData[idx - 1, 5] = data[0];
                        //strData[idx - 1, 6] = data[0];
                        //strData[idx - 1, 7] = data[0];
                        //strData[idx - 1, 8] = data[0];
                        //strData[idx - 1, 9] = data[0];
                        //strData[idx - 1, 10] = data[0];
                        //strData[idx - 1, 11] = data[0];
                        //strData[idx - 1, 12] = data[0];
                        //strData[idx - 1, 13] = data[0];
                        //strData[idx - 1, 14] = data[0];
                        //strData[idx - 1, 15] = data[0];
                        //strData[idx - 1, 16] = data[0];
                        //strData[idx - 1, 17] = data[0];
                        //for (int i = 0; i < 18; i++)//不能循环，否则内存溢出
                        //{
                        //strData[idx - 1,0] = data[0];
                        //strData[idx - 1, 1] = data[1];
                        //strData[idx - 1, 2] = data[2];
                        //strData[idx - 1, 3] = data[3];
                        //strData[idx - 1, 4] = data[4];
                        //strData[idx - 1, 5] = data[5];
                        //strData[idx - 1, 6] = data[6];
                        //strData[idx - 1, 7] = data[7];
                        //strData[idx - 1, 8] = data[8];
                        //strData[idx - 1, 9] = data[9];
                        //strData[idx - 1, 10] = data[10];
                        //strData[idx - 1, 11] = data[11];
                        //strData[idx - 1, 12] = data[12];
                        //strData[idx - 1, 13] = data[13];
                        //strData[idx - 1, 14] = data[14];
                        //strData[idx - 1, 15] = data[15];
                        //strData[idx - 1, 16] = data[16];
                        //strData[idx - 1, 17] = data[17];
                        //}
                    }
                    //this.toolStripStatusLabel3.Text = idx.ToString() + "+" + (DateTime.Now - startTime).ToString() + "+" + strData[idx - 1,0];
                }
            }
        }//openDataFile

        ///----------------------------------------------------------------------
        /// 读取前台软件的设备配置文件，初始化变量
        ///----------------------------------------------------------------------
        private void ReadConfigFileAndInit()
        {
            //读取文件
            bool blnFileExist = false;//参数文件是否存在         
                                      //string strFileName = _pipeServer._TransferCmd.strPara_FileName;           
                                      //try
                                      //{
                                      //    if (File.Exists(strFileName))
                                      //    {
                                      //        string[] lines = File.ReadAllLines(strFileName);
                                      //        int row = lines.GetLength(0); //行数
                                      //        string[] cols = lines[0].Split(',');
                                      //        int col = cols.GetLength(0);  //每行数据的个数  
                                      //        string[,] arrParas = new string[row, col]; //数组
                                      //        for (int i = 0; i < row; i++)  //读入数据并赋予数组
                                      //        {
                                      //            string[] data = lines[i].Split(',');
                                      //            for (int j = 0; j < col; j++)
                                      //            {
                                      //                arrParas[i, j] = (data[j]);
                                      //            }
                                      //        }                    
                                      //        intArrNo = 0;

            //        blnFileExist = true;
            //    }
            //    else
            //    {
            //        //MessageBox.Show("文件不存在");
            //        blnFileExist = false;
            //    }
            //}
            //catch (Exception ex)
            //{
            //    //MessageBox.Show(ex.Message);

            //}

            //GlobeVal.mysys = new ClassSys();
            //if (File.Exists(System.Windows.Forms.Application.StartupPath + "\\AppleLabJ" + "\\sys\\setup.ini") == true)
            //{
            //    GlobeVal.mysys = GlobeVal.mysys.DeSerializeNow(System.Windows.Forms.Application.StartupPath + "\\AppleLabJ" + "\\sys\\setup.ini");
            //    blnFileExist = true;
            //}
            //else
            //{
            //    blnFileExist = false;
            //}
            blnFileExist = true;
            if (blnFileExist)//init
            {
                GlobeVal.myconfigfile.machinecount = 2;
                modMain.initValue(GlobeVal.myconfigfile.machinecount);
                Demo.Init();
                Demo.readdemo(Application.StartupPath + @"\demo\计算演示1.txt");
                Demo.makesin();
                _TransferData.init();
                if (GlobeVal.myconfigfile.mode == (int)modMain.CtlerType.EDC220 || GlobeVal.myconfigfile.mode == (int)modMain.CtlerType.EDCi15)
                {
                    if (tmrEDC.Enabled == false)
                    {
                        ConnectToEdcAll();
                        tmrEDC.Enabled = true;
                    }
                }

                dataGridView1.Columns.Clear();
                DataGridViewColumn b = new DataGridViewColumn();
                b.Name = "项目";
                b.HeaderText = "项目";
                DataGridViewCell dgvcell = new DataGridViewTextBoxCell();
                b.CellTemplate = dgvcell;
                b.Frozen = true;
                dataGridView1.Columns.Add(b);
                for (int i = 0; i < GlobeVal.myconfigfile.machinecount; i++)
                {
                    b = new DataGridViewColumn();
                    b.HeaderText = "主机" + (i + 1).ToString().Trim();
                    dgvcell = new DataGridViewTextBoxCell();
                    b.CellTemplate = dgvcell;
                    dataGridView1.Columns.Add(b);
                }

                string[] sname = new string[20];
                sname[0] = "编号";
                sname[1] = "类型";
                sname[2] = "状态";
                sname[3] = "控制值";
                sname[4] = "负荷[N]";
                sname[5] = "位移[mm]";
                sname[6] = "变形A[mm]";
                sname[7] = "变形B[mm]";
                sname[8] = "平均变形[mm]";
                sname[9] = "不平衡度[%]";
                sname[10] = "控温[℃]";
                sname[11] = "上段温度";
                sname[12] = "中段温度";
                sname[13] = "下段温度";
                sname[14] = "温度梯度";
                sname[15] = "试验时间[s]";
                sname[16] = "波形个数";
                sname[17] = "循环次数";
                dataGridView1.Rows.Clear();               
                for (int i = 0; i <= 17; i++)
                {
                    string[] mt = new string[GlobeVal.myconfigfile.machinecount + 1];
                    mt[0] = sname[i];
                    mt[1] = (i + 1).ToString();
                    mt[2] = (i + 3).ToString();
                    if ((i == 4) || (i == 5) || (i == 6) || (i == 7) || (i == 8))
                    {
                        mt[1] = "0";
                        mt[2] = "0";
                    }
                    dataGridView1.Rows.Add(mt);
                }
                if (timer1.Enabled == false)
                {
                    timer1.Enabled = true;
                }
            }
        }//ReadConfigFileAndInit

        ///----------------------------------------------------------------------
        /// 读取正在试验的记录文件，继续试验     1台设备一行，每行sysno，keeptest，paraName
        ///----------------------------------------------------------------------
        private void ReadTestFileAndContinue()
        {
            //读取文件
            bool blnFileExist = false;//参数文件是否存在         
            string strFileName = modMain.strKeepfile;
            try
            {
                if (File.Exists(strFileName))
                {
                    string[] lines = File.ReadAllLines(strFileName);
                    int row = lines.GetLength(0); //行数
                    string[] cols = lines[0].Split(',');
                    int col = cols.GetLength(0);  //每行数据的个数  
                    string[,] arrParas = new string[row, col]; //数组
                    for (int i = 0; i < row; i++)  //读入数据并赋予数组
                    {
                        string[] data = lines[i].Split(',');
                        for (int j = 0; j < col; j++)
                        {
                            arrParas[i, j] = (data[j]);
                        }
                    }
                    if (GlobeVal.myconfigfile.machinecount == row)
                    {
                        blnFileExist = true;
                        for (int i = 0; i < GlobeVal.myconfigfile.machinecount; i++)
                        {
                            if (int.Parse(arrParas[i, 1]) > 0)
                            {
                                ReadParaAndWriteEDC(i + 1, arrParas[i, 2], 0);
                                modMain.blnStartTest[i] = true;
                            }
                            else
                            {
                                modMain.blnStartTest[i] = false;
                            }
                        }
                    }
                    else
                    {
                        blnFileExist = false;
                    }
                }
                else
                {
                    //MessageBox.Show("文件不存在");
                    blnFileExist = false;
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
                blnFileExist = false;
            }
        }//ReadTestFileAndContinue    

        //new procedure
    }
}
