using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Text.RegularExpressions;

namespace Similarity
{
    /// <summary>
    /// 语音匹配算法流程
    /// 1.去掉语音中的特殊字符
    /// 2.从语音参数中获取中文部分
    /// 3.中文部分做匹配获取最佳匹配的字段名
    /// 4.返回匹配到结果的那行数据的字段名，期望填充的类型，从原语音中分割出值
    ///     4.1类型是字符串，原语音去掉开始匹配到的中文作为值，
    ///     4.2类型是数字，原语音去掉开始匹配到的中文后过滤出数字作为字符串。
    /// 5.将字段名和值返回
    /// </summary>
    public class Similarity
    {
        //public static void Main()
        //{
        //    Regex reg = new Regex("[?。，？,!！、]*");
        //    string str = reg.Replace("F071药品陈列环境温度、湿度记录表", "");
        //    //Dictionary<String, string> res = new Dictionary<string, string>();
        //    double  res = CompareString(str, "CompareString");

        //    //Console.WriteLine(res["key"] + "\t" + res["value"]);
        //    Console.WriteLine(res);
        //    Console.Read();
        //}

        /*   public static Dictionary<String, string> test(string str, DataTable dt)
           {
               Dictionary<String, string> res = new Dictionary<string, string>();
               string key = "匹配失败";
               string value = "null";
               double threshold = 0;
               Regex reg = new Regex("[?。，？,!！、]*");
               str = reg.Replace(str, "");
               string ChineseChar = new Regex("[\u4e00-\u9fa5]+").Match(str).Value;
               //DataTable dt = FieldData.getTable();
               int index = UpdateSimilarity(ChineseChar, dt);
               if (double.Parse(dt.Rows[index]["similarity"].ToString()) > threshold)
               {
                   key = dt.Rows[index][0].ToString();
                   value = str.Substring(ChineseChar.Length);
                   if (int.Parse(dt.Rows[index]["valueType"].ToString()) == 0)
                   {
                       //字符串

                   }
                   else
                   {
                       //数字

                   }
               }
               res.Add("key", key);
               res.Add("value", value);
               return res;
           }*/

        /// <summary>
        /// 新的相似度算法比较
        /// </summary>
        /// <param name="str1"></param>
        /// <param name="str2"></param>
        /// <returns></returns>
        public static double CompareStringNew(string str1, string str2)
        {     

            string strA = str1;
            string strB = str2;
            if(str1.Length>str2.Length)
            {
                strA = str2;
                strB = str1;
            }
            int length1 = strA.Length;
            int length2 = strB.Length;
            double curSimi = 0;
            for(int i=length1;i<=length2;i++)
            {
                curSimi=Math.Max(CompareString(strA, strB.Substring(i - length1, length1)),curSimi);
            }
            return curSimi;
        }
        /// <summary>
        /// 比较两个字符串的相似度
        /// </summary>
        /// <param name="str1"></param>
        /// <param name="str2"></param>
        /// <returns>相似度</returns>
        public static double CompareString(string str1, string str2)
        {
            if(str1 == "" && str2 == "")
            {
                return 0;
            }
            double result = 0;
            result += Leven(str1, str2);
            result += Leven(Spell.MakeSpellCode(str1, SpellOptions.EnableUnicodeLetter), Spell.MakeSpellCode(str2, SpellOptions.EnableUnicodeLetter));
            return result/2;
        }
        private static string FormatCaption(string str)
        {
            Regex reg = new Regex("[\\。\\，\\/\\-\\、\\—\\s]*");
            str = reg.Replace(str, "");
            return str.Trim();
        }
        /// <summary>
        /// 获取匹配到的结果集
        /// </summary>
        /// <param name="dtSrc">待匹配的源结果集</param>
        /// <param name="numberOfRows">返回的行数</param>
        /// <param name="colName">结果集中匹配的字段</param>
        /// <param name="srcStr">待匹配的字符串</param>
        /// <param name="minThreshold">最小阈值</param>
        /// <returns>返回结果集</returns>
        public static DataTable GetResultList(DataTable dtSrc, string colName, string srcStr, int numberOfRows, double minThreshold,bool needSort = true)
        {
            DataTable result = new DataTable();
            if (dtSrc.Columns.Contains(colName))
            {

                DataTable dtCopy = dtSrc.Copy();
                dtCopy.Columns.Add("FSimilarity", typeof(double));
                result = dtCopy.Clone();
                for (int i = 0; i < dtCopy.Rows.Count; i++)
                {
                    dtCopy.Rows[i]["FSimilarity"] = 0;
                    double weight = double.Parse(dtCopy.Rows[i]["FSimilarity"].ToString());
                    weight = CompareString(FormatCaption(dtCopy.Rows[i][colName].ToString()), srcStr);
                    dtCopy.Rows[i]["FSimilarity"] = weight;
                    if (weight > minThreshold)
                    {
                        result.ImportRow(dtCopy.Rows[i]);
                    }
                }
                DataView dv = result.DefaultView;//获取表视图
                if (needSort)
                {

                    dv.Sort = "Fsimilarity DESC";//按照ID倒序排序
                }
                result = dv.ToTable();//转为表
                DataTable temp = result.Copy();
                result.Clear();
                for (int i = 0; i < numberOfRows; i++)
                {
                    if (i < temp.Rows.Count)
                        result.ImportRow(temp.Rows[i]);
                    else
                        break;
                }

            }
            return result;
        }
        private static int UpdateSimilarity(string desc, DataTable dt)
        {
            if (desc == "")
            {
                return -1;
            }
            var maxRowIndex = 0;
            string qp = Spell.MakeSpellCode(desc, SpellOptions.EnableUnicodeLetter);
            string sm = Spell.MakeSpellCode(desc, SpellOptions.FirstLetterOnly);
            double lastWeight = 0;
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                dt.Rows[i][3] = 0;
                double weight = int.Parse(dt.Rows[i][3].ToString());
                weight = CompareString(FormatCaption(dt.Rows[i][0].ToString()), desc);
                dt.Rows[i][3] = weight;
                if (i > 0)
                {
                    if (weight > lastWeight)
                    {
                        maxRowIndex = i;
                        lastWeight = weight;
                    }
                }
                else
                    lastWeight = weight;
            }
            return maxRowIndex;
        }
        /// <summary>
        /// Leven算法，最短编辑距离
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <returns></returns>
        private static double Leven(string value1, string value2)
        {
            int len1 = value1.Length;
            int len2 = value2.Length;
            int[,] dif = new int[len1 + 1, len2 + 1];
            for (int a = 0; a <= len1; a++)
            {
                dif[a, 0] = a;
            }
            for (int a = 0; a <= len2; a++)
            {
                dif[0, a] = a;
            }
            int temp = 0;
            for (int i = 1; i <= len1; i++)
            {
                for (int j = 1; j <= len2; j++)
                {
                    if (value1[i - 1] == value2[j - 1])
                    { temp = 0; }
                    else
                    {
                        temp = 1;
                    }
                    dif[i, j] = Min(dif[i - 1, j - 1] + temp, dif[i, j - 1] + 1,
                        dif[i - 1, j] + 1);
                }
            }

            double similarity = 1 - (double)dif[len1, len2] / Math.Max(len1, len2);
            return similarity;
        }

        private static int Min(int a, int b, int c)
        {
            int i = a < b ? a : b;
            return i = i < c ? i : c;
        }
    }
}
