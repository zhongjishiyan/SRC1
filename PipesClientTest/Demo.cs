using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace PipesClientTest
{
  public   class Demo
    {
      public   struct demodata
        {
            public float pos;
            public float load;
            public float  ext;
            public float cmd;
            public double time;
            public float count;
            
        }

        public static List<demodata> mdemodata;

        public static bool mdemo = false;

        public static int mdemoline = 0;

        public static double mdemotime = 0;

        public static   void readdemo(string fileName)
        {
            mdemodata = new List<demodata>();
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

            mdemo = true;

            mdemotime = System.Environment.TickCount / 1000.0;
        }
    }
}
