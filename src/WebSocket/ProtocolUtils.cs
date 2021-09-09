using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IocpSharp.WebSocket
{
    public class ProtocolUtils
    {
        
        private static byte[] salt_ = Encoding.ASCII.GetBytes("258EAFA5-E914-47DA-95CA-C5AB0DC85B11");

        /// <summary>
        /// 固定值：258EAFA5-E914-47DA-95CA-C5AB0DC85B11
        /// </summary>
        public static byte[] Salt => salt_;

        /// <summary>
        /// SHA1算法
        /// </summary>
        /// <param name="clearData">明文</param>
        /// <returns>HASH值，Base64编码</returns>
        public static string SHA1(byte[] clearData)
        {
            using (System.Security.Cryptography.SHA1 sha1 = System.Security.Cryptography.SHA1.Create())
            {
                return Convert.ToBase64String(sha1.ComputeHash(clearData));
            }
        }
    }
}
