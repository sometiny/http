using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace IocpSharp.Http
{
    public static class StreamUtils
    {
        /// <summary>
        /// 清空流
        /// </summary>
        /// <param name="stream"></param>
        public static void Clear(Stream stream)
        {
            byte[] forwardBuffer = new byte[32768];
            while (stream.Read(forwardBuffer, 0, forwardBuffer.Length) > 0);
        }

        /// <summary>
        /// 从流中读取所有数据
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static byte[] ReadAllBytes(Stream stream)
        {
            using MemoryStream output = new MemoryStream();
            stream.CopyTo(output);
            return output.ToArray();
        }
    }
}
