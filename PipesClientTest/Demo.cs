using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace PipesClientTest
{
  public class Demo
    {
      public struct demodata
        {
            public float pos;
            public float load;
            public float ext;
            public float cmd;
            public double time;
            public float count;            
        }
        public static List<demodata> mdemodata;
        public static List<demodata> mdemosindata;
        public static List<demodata> mdemocreepdata;
        public static int[] mdemocount;
        public static bool[] mdemo;
        public static int[] mdemoline;
        public static double[] mdemotime;

        public static void Init()
        {
            mdemosindata = new List<demodata>();
            mdemocreepdata = new List<demodata>();
            mdemodata = new List<demodata>();
            mdemo = new bool[100];
            mdemoline = new int[100];
            mdemotime = new double[100];
            mdemocount = new int[100];
            for(int i=0;i<100;i++)
            {
                mdemocount[i] = 0;
            }
        }

        public static void makesin()
        {
            double x=0;
            float  y = 0;              
            mdemosindata.Clear();
            for (int i=0;i<100;i++)
            {                
                y = Convert.ToSingle( 30 * Math.Sin(2 * 3.1415926 * i / 100));
                demodata m = new demodata();
                m.load = y;
                m.pos = y / 10;
                m.ext = 0;
                m.time = 1/50;                
                mdemosindata.Add(m);
            }
        }

        public static   void readdemo(string fileName)
        {           
            int i = -1;
            int j = 0;
            char[] sp;
            char[] sp1;
            string[] ww;
            string line;
            sp = new char[2];
            sp1 = new char[2];
            mdemodata.Clear();
            using (StreamReader sr = new StreamReader(fileName, Encoding.Default))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    i = i + 1;
                    if (i == 0)
                    {
                        sp[0] = Convert.ToChar(" ");
                        ww = line.Split(sp);
                        for (j = 0; j < ww.Length; j++)
                        {

                        }
                    }
                    else if (i == 1)
                    {
                        sp[0] = Convert.ToChar(" ");
                        ww = line.Split(sp);
                        for (j = 0; j < ww.Length; j++)
                        {

                        }
                    }
                    else
                    {
                        sp[0] = Convert.ToChar(" ");
                        ww = line.Split(sp);
                        int L = ww.Length;
                        demodata m = new demodata();
                        m.time = Convert.ToDouble(ww[0]);
                        m.load = Convert.ToSingle(ww[1]);
                        m.pos = Convert.ToSingle(ww[2]);
                        m.ext = Convert.ToSingle(ww[3]);
                        mdemodata.Add(m);
                    }
                }
            }           
        }
    }
}
