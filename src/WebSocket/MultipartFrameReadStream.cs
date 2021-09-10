using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IocpSharp.Http.Streams;
using System.IO;

namespace IocpSharp.WebSocket
{
    /// <summary>
    /// 针对FIN标志为0的非结束帧，我们使用单独的流来处理
    /// </summary>
    public class MultipartFrameReadStream : Stream
    {
        private Stream _innerStream = null;
        private bool _leaveInnerStreamOpen = true;
        private Frame _frame = null;
        private Frame _firstFrame = null;
        private Stream _frameReadStream = null;

        /// <summary>
        /// 使用指定长度、基础流和模式创建实例
        /// </summary>
        /// <param name="payloadLength">数据长度</param>
        /// <param name="stream"></param>
        /// <param name="leaveInnerStreamOpen"></param>
        public MultipartFrameReadStream(Frame frame, Stream stream, bool leaveInnerStreamOpen)
        {
            _firstFrame = _frame = frame;
            _innerStream = stream;
            _leaveInnerStreamOpen = leaveInnerStreamOpen;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if(_frameReadStream == null)
            {
                _frameReadStream = _frame.OpenRead(_innerStream);
            }

            int rec = _frameReadStream.Read(buffer, offset, count);
            if(rec == 0)
            {
                if (_frame.Fin) return 0;
                _frame.Dispose();
                _frame = Frame.NextFrame(_innerStream);
                _frameReadStream = _frame.OpenRead(_innerStream);
                return Read(buffer, offset, count);
            }
            return rec;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!_leaveInnerStreamOpen) _innerStream?.Close();
                if (_firstFrame != _frame) _frame?.Dispose();
            }
            _innerStream = null;
            _frame = null;
            _firstFrame = null;
            _frameReadStream = null;
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
