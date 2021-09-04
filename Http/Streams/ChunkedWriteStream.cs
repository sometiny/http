using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IocpSharp.Http.Streams
{
    /// <summary>
    /// 实现同步写的Stream，不支持读取操作
    /// </summary>
    public class ChunkedWriteStream : Stream
    {
        private static byte[] _crlf = new byte[] { 13, 10};
        private static byte[] _ending = Encoding.ASCII.GetBytes("0\r\n\r\n");

        private Stream _innerStream = null;
        private bool _leaveInnerStreamOpen = true;

        /// <summary>
        /// 使用指定基础流和模式创建实例
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="leaveInnerStreamOpen"></param>
        public ChunkedWriteStream(Stream stream, bool leaveInnerStreamOpen)
        {
            _innerStream = stream;
            _leaveInnerStreamOpen = leaveInnerStreamOpen;
        }

        /// <summary>
        /// 写入数据块到基础流
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            ///组装包头，包含长度数据
            ///数据长度的16进制表示形式+\r\n
            ///举例：
            ///如果数据长度是10，那么包头是：a\r\n
            ///如果数据长度是20，那么包头是：14\r\n
            ///如果数据长度是256，那么包头是：100\r\n
            string length = count.ToString("x") + "\r\n";
            byte[] lengthBuffer = Encoding.ASCII.GetBytes(length);

            ///写入长度数据
            _innerStream.Write(lengthBuffer, 0, lengthBuffer.Length);

            ///写入包数据
            _innerStream.Write(buffer, offset, count);

            ///写入包尾，固定的回车换行
            _innerStream.Write(_crlf, 0, 2);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_leaveInnerStreamOpen)
            {
                _innerStream?.Close();
            }
            _innerStream = null;
            base.Dispose(disposing);
        }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long length) => throw new NotSupportedException();
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    }
}
