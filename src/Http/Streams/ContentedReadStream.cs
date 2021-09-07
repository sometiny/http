using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IocpSharp.Http.Streams
{
    /// <summary>
    /// 实现一个对固定数据长度消息读取的流
    /// </summary>
    public class ContentedReadStream : Stream
    {

        private Stream _innerStream = null;
        private bool _leaveInnerStreamOpen = true;
        private long _contentLength = 0;

        /// <summary>
        /// 使用指定长度、基础流和模式创建实例
        /// </summary>
        /// <param name="contentLength">数据长度</param>
        /// <param name="stream"></param>
        /// <param name="leaveInnerStreamOpen"></param>
        public ContentedReadStream(long contentLength, Stream stream, bool leaveInnerStreamOpen)
        {
            if(contentLength < 0)
            {
                throw new ArgumentOutOfRangeException("contentLength", "contentLength must >= 0");
            }

            _innerStream = stream;
            _leaveInnerStreamOpen = leaveInnerStreamOpen;
            _contentLength = contentLength;
        }

        /// <summary>
        /// 重写Read方法，主要目的是限制数据读取的长度
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            //读完数据，返回0
            if (_contentLength == 0) return 0;

            if(count > _contentLength)
            {
                count = (int)(_contentLength & 0xffffffff);
            }

            count = _innerStream.Read(buffer, offset, count);
            _contentLength -= count;
            return count;
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
        public override void Flush() => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long length) => throw new NotSupportedException();
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    }
}
