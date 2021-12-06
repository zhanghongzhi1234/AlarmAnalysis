using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace AlarmAnalysisService
{
    public static class CommonFunction
    {
        /// <summary>
        /// 计算算数平均数:（x1+x2+...+xn）/n
        /// </summary>
        /// <param name="arr">数组</param>
        /// <returns>算术平均数</returns>
        public static double ArithmeticMean(double[] arr)
        {
            double result = 0;
            foreach (double num in arr)
            {
                result += num;
            }
            return result / arr.Length;
        }

        /// <summary>
        /// 几何平均数：(x1*x2*...*xn)^(1/n)
        /// </summary>
        /// <param name="arr">数组</param>
        /// <returns>几何平均数</returns>
        public static double GeometricMean(double[] arr)
        {
            double result = 1;
            foreach (double num in arr)
            {
                result *= Math.Pow(num, 1.0 / arr.Length);
            }
            return result;
        }

        /// <summary>
        /// 调和平均数：n/((1/x1)+(1/x2)+...+(1/xn))
        /// </summary>
        /// <param name="arr">数组</param>
        /// <returns>调和平均数</returns>
        public static double HarmonicMean(double[] arr)
        {
            double temp = 0;
            foreach (double num in arr)
            {
                temp += (1.0 / num);
            }
            return arr.Length / temp;
        }

        /// <summary>
        /// 平方平均数：((x1*x1+x2*x2+...+xn*xn)/n)^(1/2)
        /// </summary>
        /// <param name="arr">数组</param>
        /// <returns>平方平均数</returns>
        public static double RootMeanSquare(double[] arr)
        {
            double temp = 0;
            foreach (double num in arr)
            {
                temp += (num * num);
            }
            return Math.Sqrt(temp / arr.Length);
        }

        /// <summary>
        /// 计算中位数
        /// </summary>
        /// <param name="arr">数组</param>
        /// <returns></returns>
        public static double Median(double[] arr)
        {
            //为了不修改arr值，对数组的计算和修改在tempArr数组中进行
            double[] tempArr = new double[arr.Length];
            arr.CopyTo(tempArr, 0);

            //对数组进行排序
            double temp;
            for (int i = 0; i < tempArr.Length; i++)
            {
                for (int j = i; j < tempArr.Length; j++)
                {
                    if (tempArr[i] > tempArr[j])
                    {
                        temp = tempArr[i];
                        tempArr[i] = tempArr[j];
                        tempArr[j] = temp;
                    }
                }
            }

            //针对数组元素的奇偶分类讨论
            if (tempArr.Length % 2 != 0)
            {
                return tempArr[arr.Length / 2 + 1];
            }
            else
            {
                return (tempArr[tempArr.Length / 2] +
                    tempArr[tempArr.Length / 2 + 1]) / 2.0;
            }
        }

        //X16 + X12 + X5 + 1 余式表
        private static ushort[] CRCTable = new ushort[256]
        {
            0x0000,0x1021,0x2042,0x3063,0x4084,0x50a5,0x60c6,0x70e7,
            0x8108,0x9129,0xa14a,0xb16b,0xc18c,0xd1ad,0xe1ce,0xf1ef,
            0x1231,0x0210,0x3273,0x2252,0x52b5,0x4294,0x72f7,0x62d6,
            0x9339,0x8318,0xb37b,0xa35a,0xd3bd,0xc39c,0xf3ff,0xe3de,
            0x2462,0x3443,0x0420,0x1401,0x64e6,0x74c7,0x44a4,0x5485,
            0xa56a,0xb54b,0x8528,0x9509,0xe5ee,0xf5cf,0xc5ac,0xd58d,
            0x3653,0x2672,0x1611,0x0630,0x76d7,0x66f6,0x5695,0x46b4,
            0xb75b,0xa77a,0x9719,0x8738,0xf7df,0xe7fe,0xd79d,0xc7bc,
            0x48c4,0x58e5,0x6886,0x78a7,0x0840,0x1861,0x2802,0x3823,
            0xc9cc,0xd9ed,0xe98e,0xf9af,0x8948,0x9969,0xa90a,0xb92b,
            0x5af5,0x4ad4,0x7ab7,0x6a96,0x1a71,0x0a50,0x3a33,0x2a12,
            0xdbfd,0xcbdc,0xfbbf,0xeb9e,0x9b79,0x8b58,0xbb3b,0xab1a,
            0x6ca6,0x7c87,0x4ce4,0x5cc5,0x2c22,0x3c03,0x0c60,0x1c41,
            0xedae,0xfd8f,0xcdec,0xddcd,0xad2a,0xbd0b,0x8d68,0x9d49,
            0x7e97,0x6eb6,0x5ed5,0x4ef4,0x3e13,0x2e32,0x1e51,0x0e70,
            0xff9f,0xefbe,0xdfdd,0xcffc,0xbf1b,0xaf3a,0x9f59,0x8f78,
            0x9188,0x81a9,0xb1ca,0xa1eb,0xd10c,0xc12d,0xf14e,0xe16f,
            0x1080,0x00a1,0x30c2,0x20e3,0x5004,0x4025,0x7046,0x6067,
            0x83b9,0x9398,0xa3fb,0xb3da,0xc33d,0xd31c,0xe37f,0xf35e,
            0x02b1,0x1290,0x22f3,0x32d2,0x4235,0x5214,0x6277,0x7256,
            0xb5ea,0xa5cb,0x95a8,0x8589,0xf56e,0xe54f,0xd52c,0xc50d,
            0x34e2,0x24c3,0x14a0,0x0481,0x7466,0x6447,0x5424,0x4405,
            0xa7db,0xb7fa,0x8799,0x97b8,0xe75f,0xf77e,0xc71d,0xd73c,
            0x26d3,0x36f2,0x0691,0x16b0,0x6657,0x7676,0x4615,0x5634,
            0xd94c,0xc96d,0xf90e,0xe92f,0x99c8,0x89e9,0xb98a,0xa9ab,
            0x5844,0x4865,0x7806,0x6827,0x18c0,0x08e1,0x3882,0x28a3,
            0xcb7d,0xdb5c,0xeb3f,0xfb1e,0x8bf9,0x9bd8,0xabbb,0xbb9a,
            0x4a75,0x5a54,0x6a37,0x7a16,0x0af1,0x1ad0,0x2ab3,0x3a92,
            0xfd2e,0xed0f,0xdd6c,0xcd4d,0xbdaa,0xad8b,0x9de8,0x8dc9,
            0x7c26,0x6c07,0x5c64,0x4c45,0x3ca2,0x2c83,0x1ce0,0x0cc1,
            0xef1f,0xff3e,0xcf5d,0xdf7c,0xaf9b,0xbfba,0x8fd9,0x9ff8,
            0x6e17,0x7e36,0x4e55,0x5e74,0x2e93,0x3eb2,0x0ed1,0x1ef0
        };
        /*****************************************************************************
        ;** 函数名称: XmlGetCrc16
        ;** 功能描述: 计算CRC16
        ;** 参数: ptr_source: 数据
        ;** len: 长度
        ;** 返回值: CRC16 码
        ;**-------------------------------------------------------------------------
        ;****************************************************************************/
        public static ushort XmlGetCrc16(byte[] source)
        {
            ushort crc = 0, by;
            int i;
            int len = source.Count();
            for (i = 0; i < len; i++)
            {
                by = (ushort)((crc >> 8) & 0xff);
                crc = (ushort)((crc & 0xffff) << 8);
                crc = (ushort)((crc ^ CRCTable[(source[i] ^ by) & 0xff]) & 0xffff);
            }
            return crc;
        }

        public static byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            byte[] toEncryptArray = UTF8Encoding.UTF8.GetBytes(plainText);
            if (toEncryptArray.Count() % 16 != 0)
            {
                for (int i = 0; i < 16 - toEncryptArray.Count() % 16; i++)
                    plainText += '\0';
            }
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");
            byte[] encrypted;
            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.None;
                //aesAlg.KeySize = 16;
                // Create a decrytor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }

            // Return the encrypted bytes from the memory stream.
            return encrypted;
        }

        public static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");

            // Declare the string used to hold the decrypted text.
            string plaintext = null;

            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.None;
                //aesAlg.KeySize = 16;
                // Create a decrytor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {   // Read the decrypted bytes from the decrypting stream and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
            DebugUtil.Instance.LOG.Debug(plaintext);
            return plaintext;
        }

        public static string GetMd5Hash(MD5 md5Hash, string input)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        // Verify a hash against a string.
        public static bool VerifyMd5Hash(MD5 md5Hash, string input, string hash)
        {
            // Hash the input.
            string hashOfInput = GetMd5Hash(md5Hash, input);

            // Create a StringComparer an compare the hashes.
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;

            if (0 == comparer.Compare(hashOfInput, hash))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        public static string ByteArrayToString1(byte[] ba)
        {
            string hex = BitConverter.ToString(ba);
            return hex.Replace("-", " ");
        }

        public static byte[] ConvertIntToByte(object value, bool IsLittleEndian = true)
        {
            byte[] ret = null;
            Type t = value.GetType();

            if (value.GetType().Name == "Boolean")
            {
                bool temp = (bool)value;
                ret = BitConverter.GetBytes(temp);
            }
            else if (value.GetType().Name == "Char")
            {
                char temp = (char)value;
                ret = BitConverter.GetBytes(temp);
            }
            else if (value.GetType().Name == "Double")
            {
                double temp = (double)value;
                ret = BitConverter.GetBytes(temp);
            }
            else if (value.GetType().Name == "Single")
            {
                float temp = (float)value;
                ret = BitConverter.GetBytes(temp);
            }
            else if (value.GetType().Name == "UInt16")
            {
                ushort temp = (ushort)value;
                ret = BitConverter.GetBytes(temp);
            }
            else if (value.GetType().Name == "UInt32")
            {
                uint temp = (uint)value;
                ret = BitConverter.GetBytes(temp);
            }
            else if (value.GetType().Name == "UInt64")
            {
                ulong temp = (ulong)value;
                ret = BitConverter.GetBytes(temp);
            }
            else if (value.GetType().Name == "Int16")
            {
                short temp = (short)value;
                ret = BitConverter.GetBytes(temp);
            }
            else if (value.GetType().Name == "Int32")
            {
                int temp = (int)value;
                ret = BitConverter.GetBytes(temp);
            }
            else if (value.GetType().Name == "Int64")
            {
                long temp = (long)value;
                ret = BitConverter.GetBytes(temp);
            }
            //if (BitConverter.IsLittleEndian == true)
            if (IsLittleEndian == true)
                Array.Reverse(ret);
            return ret;
        }

        public static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
        //convert unix eopch time to DateTime(UTC)
        public static DateTime ConvertUnixTimeToDateTimeUTC(int unixTimeInSecond)
        {
            return epoch.AddSeconds(unixTimeInSecond);
        }

        //convert DateTime(UTC) to unix eopch time
        public static int ConvertDateTimeUTCToUnixTime(DateTime dtDateTime)
        {
            return Convert.ToInt32((dtDateTime - epoch).TotalSeconds);
        }

        //convert unix eopch time to DateTime(Local)
        public static DateTime ConvertUnixTimeToDateTimeLocal(int unixTimeInSecond)
        {
            return epoch.AddSeconds(unixTimeInSecond).ToLocalTime();
        }

        //convert DateTime(Local) to unix eopch time
        public static int ConvertDateTimeLocalToUnixTime(DateTime dtDateTime)
        {
            return Convert.ToInt32((dtDateTime.ToUniversalTime() - epoch).TotalSeconds);
        }

        /// <summary>
        /// Creates color with corrected brightness.
        /// </summary>
        /// <param name="color">Color to correct.</param>
        /// <param name="correctionFactor">The brightness correction factor. Must be between -1 and 1. 
        /// Negative values produce darker colors.</param>
        /// <returns>
        /// Corrected <see cref="Color"/> structure.
        /// </returns>
        public static Color ChangeColorBrightness(Color color, float correctionFactor)
        {
            float red = (float)color.R;
            float green = (float)color.G;
            float blue = (float)color.B;

            if (correctionFactor < 0)
            {
                correctionFactor = 1 + correctionFactor;
                red *= correctionFactor;
                green *= correctionFactor;
                blue *= correctionFactor;
            }
            else
            {
                red = (255 - red) * correctionFactor + red;
                green = (255 - green) * correctionFactor + green;
                blue = (255 - blue) * correctionFactor + blue;
            }

            Color ret = Color.FromArgb(color.A, (byte)red, (byte)green, (byte)blue);
            return ret;
        }

        public static Brush ChangeBrushBrightness(Brush brush, float correctionFactor)
        {
            Brush ret = null;
            if (brush is SolidColorBrush)
            {
                SolidColorBrush brush1 = brush as SolidColorBrush;
                Color color = ChangeColorBrightness(brush1.Color, correctionFactor);
                ret = new SolidColorBrush(color);
            }
            
            return ret;
        }

        /*public static void GetNextSimilarColor(Brush brush, float delta)
        {   //Init Chart3_1, Chart3_2, Chart3_3, Chart3_4
            Brush subBrush = CommonFunction.ChangeBrushBrightness(brush, 0.5f + delta * j);
        }*/

        public static Color GetInvertedColor(Color color)
        {
            Color color1 = new Color();
            color1.A = color.A;
            color1.R = Convert.ToByte(0xff - color.R);
            color1.G = Convert.ToByte(0xff - color.G);
            color1.B = Convert.ToByte(0xff - color.B);
            return color1;
        }

        public static SolidColorBrush GetInvertedBrush(SolidColorBrush brush)
        {
            SolidColorBrush ret = null;
            if (brush != null)
                ret = new SolidColorBrush(GetInvertedColor(brush.Color));

            return ret;
        }

        public static bool GetBoolValue(object o)
        {
            if (o == null)
                return false;
            string temp = o.ToString().ToLower();
            if (temp == "false" || temp == "0")
            {
                return false;
            }
            else
                return true;
        }
    }
}
