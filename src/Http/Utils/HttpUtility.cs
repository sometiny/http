using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.Reflection;

namespace IocpSharp.Http.Utils
{
    /// <summary>
    /// URL参数的解析相关辅助方法
    /// Encode和Decode部分参考内核的代码
    /// </summary>
    public class HttpUtility
    {
        private static char[] chars = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };
        internal static bool ValidateUrlEncodingParameters(byte[] bytes, int offset, int count)
        {
            if ((bytes == null) && (count == 0))
            {
                return false;
            }
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }
            if ((offset < 0) || (offset > bytes.Length))
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            if ((count < 0) || ((offset + count) > bytes.Length))
            {
                throw new ArgumentOutOfRangeException("count");
            }
            return true;
        }
        public static bool IsUrlSafeChar(char ch)
        {
            if ((((ch < 'a') || (ch > 'z')) && ((ch < 'A') || (ch > 'Z'))) && ((ch < '0') || (ch > '9')))
            {
                switch (ch)
                {
                    case '(':
                    case ')':
                    case '*':
                    case '-':
                    case '.':
                    case '!':
                        break;

                    case '+':
                    case ',':
                    case ' ':
                        goto Label_0051;

                    default:
                        if (ch != '_')
                        {
                            goto Label_0051;
                        }
                        break;
                }
            }
            return true;
            Label_0051:
            return false;
        }
        public static char IntToHex(int n)
        {
            if (n <= 9)
            {
                return (char)(n + 0x30);
            }
            return (char)((n - 10) + 0x61);
        }

        public static string UrlEncode(string src)
        {
            return UrlEncode(src, Encoding.UTF8);
        }
        public static string UrlEncode(string src, Encoding enc)
        {
            if (string.IsNullOrEmpty(src))
            {
                return "";
            }
            byte[] data = enc.GetBytes(src);
            return Encoding.ASCII.GetString(UrlEncode(data, 0, data.Length));
        }
        public static byte[] UrlEncode(byte[] bytes, int offset, int count)
        {
            if (!ValidateUrlEncodingParameters(bytes, offset, count))
            {
                return null;
            }
            int num = 0;
            int num2 = 0;
            for (int i = 0; i < count; i++)
            {
                char ch = (char)bytes[offset + i];
                if (!IsUrlSafeChar(ch))
                {
                    num2++;
                }
            }
            if ((num == 0) && (num2 == 0))
            {
                if ((offset == 0) && (bytes.Length == count))
                {
                    return bytes;
                }
                byte[] dst = new byte[count];
                Buffer.BlockCopy(bytes, offset, dst, 0, count);
                return dst;
            }
            byte[] buffer = new byte[count + (num2 * 2)];
            int num3 = 0;
            for (int j = 0; j < count; j++)
            {
                byte num6 = bytes[offset + j];
                char ch2 = (char)num6;
                if (IsUrlSafeChar(ch2))
                {
                    buffer[num3++] = num6;
                }
                else
                {
                    buffer[num3++] = 0x25;
                    buffer[num3++] = (byte)chars[(num6 >> 4) & 15];
                    buffer[num3++] = (byte)chars[num6 & 15];
                }
            }
            return buffer;
        }
        public static string UrlDecode(string src)
        {
            return UrlDecode(src, Encoding.UTF8);
        }

        public static string UrlDecode(string src, Encoding enc)
        {
            byte[] data = Encoding.ASCII.GetBytes(src);
            return enc.GetString(UrlDecode(data, 0, data.Length));
        }
        public static byte[] UrlDecode(byte[] bytes, int offset, int count)
        {
            if (!ValidateUrlEncodingParameters(bytes, offset, count))
            {
                return null;
            }
            int length = 0;
            byte[] sourceArray = new byte[count];
            for (int i = 0; i < count; i++)
            {
                int index = offset + i;
                byte num4 = bytes[index];
                if (num4 == 0x2b)
                {
                    num4 = 0x20;
                }
                else if ((num4 == 0x25) && (i < (count - 2)))
                {
                    int num5 = HexToInt((char)bytes[index + 1]);
                    int num6 = HexToInt((char)bytes[index + 2]);
                    if ((num5 >= 0) && (num6 >= 0))
                    {
                        num4 = (byte)((num5 << 4) | num6);
                        i += 2;
                    }
                }
                sourceArray[length++] = num4;
            }
            if (length < sourceArray.Length)
            {
                byte[] destinationArray = new byte[length];
                Array.Copy(sourceArray, destinationArray, length);
                sourceArray = destinationArray;
            }
            return sourceArray;
        }
        public static int HexToInt(char h)
        {
            if ((h >= '0') && (h <= '9'))
            {
                return (h - '0');
            }
            if ((h >= 'a') && (h <= 'f'))
            {
                return ((h - 'a') + 10);
            }
            if ((h >= 'A') && (h <= 'F'))
            {
                return ((h - 'A') + 10);
            }
            return -1;
        }

        /// <summary>
        /// 将URL查询参数转换为NameValueCollection
        /// </summary>
        /// <param name="bodyOrQuery">查询参数或请求体</param>
        /// <returns></returns>
        public static NameValueCollection ParseUriComponents(string bodyOrQuery)
        {
            NameValueCollection _params = new NameValueCollection();

            if (string.IsNullOrEmpty(bodyOrQuery)) return _params;

            var argvs = bodyOrQuery.Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Split('=')).ToArray() ;

            foreach(string[] arg in argvs)
            {
                if (arg.Length != 2) continue;
                _params.Add(arg[0], UrlDecode(arg[1]));
            }
            return _params;
        }
    }
}