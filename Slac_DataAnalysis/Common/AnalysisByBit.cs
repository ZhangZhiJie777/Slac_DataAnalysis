using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slac_DataAnalysis.Common
{
    public class AnalysisByBit
    {

        /// <summary>
        /// 按位解析数据
        /// </summary>
        /// <param name="bitLength">按多少位来解析数据</param>
        /// <param name="data">原始数据(string类型)</param>
        public static List<int> AnalysisByBitMethod(int bitLength, string data)
        {
            List<int> list = new List<int>(); // 存储解析后的数据
            try
            {
                if (int.TryParse(data, out int result))
                {
                    // 二进制字符串
                    string binaryStr = Convert.ToString(result, 2).PadLeft(32, '0');

                    // 按多少位来解析数据，就截取多少位数据（在后16位中，按位来解析）
                    for (int i = binaryStr.Length; i > 16; i -= bitLength)
                    {
                        string subStr = binaryStr.Substring(i - bitLength, bitLength); // 二进制字符串中，按位来截取长度，转换为十进制
                        list.Add(Convert.ToInt32(subStr, 2));                          // 返回结果为十进制数据
                    }
                }
                else
                {
                    LogConfig.Intence.WriteLog("ErrLog\\Device_State", $"Device_State", $"错误：不是有效的32位整数或超出范围！data：{data}");
                    
                }
            }
            catch (Exception ex)
            {
                LogConfig.Intence.WriteLog("ErrLog\\Device_State", $"Device_State", $"按位解释数据异常：{ex.ToString()}");
            }

            return list;
        }



    }
}
