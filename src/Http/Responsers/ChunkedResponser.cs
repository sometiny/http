using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IocpSharp.Http.Streams;

namespace IocpSharp.Http.Responsers
{
    /// <summary>
    /// 实现发送Chunked类型数据的应答器
    /// </summary>
    public class ChunkedResponser : HttpResponser
    {
        //回车换行
        private static byte[] _crlf = new byte[] { 13, 10 };

        //结束包内容
        private static byte[] _endingChunk = Encoding.ASCII.GetBytes("0\r\n\r\n");

        public ChunkedResponser() : this(200) { }

        public ChunkedResponser(int statusCode) : base(statusCode)
        {
            //移除长度标头,确保Chunked传输
            Response.RemoveHeader("Content-Length");

            //设置传输方式为Chunked
            this["Transfer-Encoding"] = "Chunked";
        }
        /// <summary>
        /// 对数据封包后，向基础流写入Chunk包
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        public override void Write(Stream stream, byte[] buffer, int offset, int size)
        {
            ///组装包头，包含长度数据
            ///数据长度的16进制表示形式+\r\n
            ///举例：
            ///如果数据长度是10，那么包头是：a\r\n
            ///如果数据长度是20，那么包头是：14\r\n
            ///如果数据长度是256，那么包头是：100\r\n
            string length = size.ToString("x") + "\r\n";
            byte[] lengthBuffer = Encoding.ASCII.GetBytes(length);

            ///注意调用的是父类Write
            ///写入长度数据
            base.Write(stream, lengthBuffer, 0, lengthBuffer.Length);

            ///写入包数据
            base.Write(stream, buffer, offset, size);

            ///写入包尾，固定的回车换行
            base.Write(stream, _crlf, 0, 2);
        }

        /// <summary>
        /// 向基础流写入结束包，即长度为0的Chunk包，编码后的值是0\r\n\r\n
        /// 这里直接写固定值，省去封包
        /// 必须向客户端发送结束包，不然客户端会一直等待数据。
        /// </summary>
        /// <param name="stream">基础流</param>
        public override void End(Stream stream)
        {
            base.Write(stream, _endingChunk, 0, 5);
        }
    }
}
