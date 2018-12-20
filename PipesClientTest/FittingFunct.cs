using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace PipesClientTest
{
    class FittingFunct
    {
        #region 多项式拟合函数,输出系数是y=a0+a1*x+a2*x*x+.........，按a0,a1,a2输出
        static public double[] Polyfit(double[] y, double[] x, int order)
        {
            double[,] guass = Get_Array(y, x, order);
            double[] ratio = Cal_Guass(guass, order + 1);
            return ratio;
        }
        #endregion

        #region 一次拟合函数，y=a0+a1*x,输出次序是a0,a1
        static public double[] Linear(double[] y, double[] x)
        {
            double[] ratio = Polyfit(y, x, 1);
            return ratio;
        }
        #endregion

        #region 一次拟合函数，截距为0，y=a0x,输出次序是a0
        static public double[] LinearInterceptZero(double[] y, double[] x)
        {
            double divisor = 0; //除数
            double dividend = 0; //被除数
            for (int i = 0; i < x.Length; i++)
            {
                divisor += x[i] * x[i];
                dividend += x[i] * y[i];
            }
            if (divisor == 0)
            {
                throw (new Exception("除数不为0！"));
                return null;
            }
            return new double[] { dividend / divisor };

        }
        #endregion

        #region 二次拟合函数，y=a0+a1*x+a2x²,输出次序是a0,a1,a2
        static public double[] TowTimesCurve(double[] y, double[] x)
        {
            double[] ratio = Polyfit(y, x, 2);
            return ratio;
        }
        #endregion

        #region 对数拟合函数,.y= c*(ln x)+b,输出为b,c
        static public double[] LOGEST(double[] y, double[] x)
        {
            double[] lnX = new double[x.Length];
            for (int i = 0; i < x.Length; i++)
            {
                if (x[i] == 0 || x[i] < 0)
                {
                    throw (new Exception("正对非正数取对数！"));
                    return null;
                }
                lnX[i] = Math.Log(x[i]);
            }
            return Linear(y, lnX);
        }
        #endregion

        #region 幂函数拟合模型, y=c*x^b,输出为c,b
        static public double[] PowEST(double[] y, double[] x)
        {
            double[] lnX = new double[x.Length];
            double[] lnY = new double[y.Length];
            double[] dlinestRet;
            for (int i = 0; i < x.Length; i++)
            {
                lnX[i] = Math.Log(x[i]);
                lnY[i] = Math.Log(y[i]);
            }
            dlinestRet = Linear(lnY, lnX);
            dlinestRet[0] = Math.Exp(dlinestRet[0]);
            return dlinestRet;
        }
        #endregion

        #region 指数函数拟合函数模型，公式为 y=c*m^x;输出为 c,m
        static public double[] IndexEST(double[] y, double[] x)
        {
            double[] lnY = new double[y.Length];
            double[] ratio;
            for (int i = 0; i < y.Length; i++)
            {
                lnY[i] = Math.Log(y[i]);
            }
            ratio = Linear(lnY, x);
            for (int i = 0; i < ratio.Length; i++)
            {
                if (i == 0)
                {
                    ratio[i] = Math.Exp(ratio[i]);
                }
            }
            return ratio;
        }
        #endregion

        #region 相关系数R²部分
        public static double Pearson(IEnumerable<double> dataA, IEnumerable<double> dataB)
        {
            int n = 0;
            double r = 0.0;
            double meanA = 0;
            double meanB = 0;
            double varA = 0;
            double varB = 0;
            int ii = 0;
            using (IEnumerator<double> ieA = dataA.GetEnumerator())
            using (IEnumerator<double> ieB = dataB.GetEnumerator())
            {
                while (ieA.MoveNext())
                {
                    if (!ieB.MoveNext())
                    {
                        //throw new ArgumentOutOfRangeException("dataB", Resources.ArgumentArraysSameLength);
                    }
                    ii++;
                    //Console.WriteLine("FF00::  " + ii + " --  " + meanA + " -- " + meanB + " -- " + varA + "  ---  " + varB);
                    double currentA = ieA.Current;
                    double currentB = ieB.Current;
                    double deltaA = currentA - meanA;
                    double scaleDeltaA = deltaA / ++n;
                    double deltaB = currentB - meanB;
                    double scaleDeltaB = deltaB / n;
                    meanA += scaleDeltaA;
                    meanB += scaleDeltaB;
                    varA += scaleDeltaA * deltaA * (n - 1);
                    varB += scaleDeltaB * deltaB * (n - 1);
                    r += (deltaA * deltaB * (n - 1)) / n;
                    //Console.WriteLine("FF00::  " + ii + " --  " + meanA + " -- " + meanB + " -- " + varA + "  ---  " + varB);
                }
                if (ieB.MoveNext())
                {
                    //throw new ArgumentOutOfRangeException("dataA", Resources.ArgumentArraysSameLength);
                }
            }
            return (r / Math.Sqrt(varA * varB)) * (r / Math.Sqrt(varA * varB));
        }
        #endregion

        #region 最小二乘法部分

        #region 计算增广矩阵
        static private double[] Cal_Guass(double[,] guass, int count)
        {
            double temp;
            double[] x_value;
            for (int j = 0; j < count; j++)
            {
                int k = j;
                double min = guass[j, j];
                for (int i = j; i < count; i++)
                {
                    if (Math.Abs(guass[i, j]) < min)
                    {
                        min = guass[i, j];
                        k = i;
                    }
                }
                if (k != j)
                {
                    for (int x = j; x <= count; x++)
                    {
                        temp = guass[k, x];
                        guass[k, x] = guass[j, x];
                        guass[j, x] = temp;
                    }
                }
                for (int m = j + 1; m < count; m++)
                {
                    double div = guass[m, j] / guass[j, j];
                    for (int n = j; n <= count; n++)
                    {
                        guass[m, n] = guass[m, n] - guass[j, n] * div;
                    }
                }

                /* System.Console.WriteLine("初等行变换：");
                 for (int i = 0; i < count; i++)
                 {
                     for (int m = 0; m < count + 1; m++)
                     {
                         System.Console.Write("{0,10:F6}", guass[i, m]);
                     }
                     Console.WriteLine();
                 }*/
            }
            x_value = Get_Value(guass, count);
            return x_value;
            /*if (x_value == null)
                Console.WriteLine("方程组无解或多解！");
            else
            {
                foreach (double x in x_value)
                {
                    Console.WriteLine("{0:F6}", x);
                }
            }*/
        }

        #endregion

        #region 回带计算X值
        static private double[] Get_Value(double[,] guass, int count)
        {
            double[] x = new double[count];
            double[,] X_Array = new double[count, count];
            int rank = guass.Rank;//秩是从0开始的
            for (int i = 0; i < count; i++)
                for (int j = 0; j < count; j++)
                    X_Array[i, j] = guass[i, j];

            if (X_Array.Rank < guass.Rank)//表示无解
            {
                return null;
            }
            if (X_Array.Rank < count - 1)//表示有多解
            {
                return null;
            }
            //回带计算x值
            x[count - 1] = guass[count - 1, count] / guass[count - 1, count - 1];
            for (int i = count - 2; i >= 0; i--)
            {
                double temp = 0;
                for (int j = i; j < count; j++)
                {
                    temp += x[j] * guass[i, j];
                }
                x[i] = (guass[i, count] - temp) / guass[i, i];
            }
            return x;
        }
        #endregion

        #region  得到数据的法矩阵,输出为发矩阵的增广矩阵
        static private double[,] Get_Array(double[] y, double[] x, int n)
        {
            double[,] result = new double[n + 1, n + 2];
            if (y.Length != x.Length)
            {
                throw (new Exception("两个输入数组长度不一！"));
                //return null;
            }
            for (int i = 0; i <= n; i++)
            {
                for (int j = 0; j <= n; j++)
                {
                    result[i, j] = Cal_sum(x, i + j);
                }
                result[i, n + 1] = Cal_multi(y, x, i);
            }
            return result;
        }

        #endregion

        #region 累加的计算
        static private double Cal_sum(double[] input, int order)
        {
            double result = 0;
            int length = input.Length;
            for (int i = 0; i < length; i++)
            {
                result += Math.Pow(input[i], order);
            }
            return result;
        }
        #endregion

        #region 计算∑(x^j)*y
        static private double Cal_multi(double[] y, double[] x, int order)
        {
            double result = 0;
            int length = x.Length;
            for (int i = 0; i < length; i++)
            {
                result += Math.Pow(x[i], order) * y[i];
            }
            return result;
        }
        #endregion

        #endregion
    }
}
