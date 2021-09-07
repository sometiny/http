using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IocpSharp.Http.Streams
{
    /// <summary>
    /// 实现一个对Chunked类型消息读取的流
    /// </summary>
    public class ChunkedReadStream : Stream
    {

        private Stream _innerStream = null;
        private bool _leaveInnerStreamOpen = true;
        private byte[] _lineBuffer = null;

        /// <summary>
        /// 使用基础流和模式创建实例
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="leaveInnerStreamOpen"></param>
        public ChunkedReadStream(Stream stream, bool leaveInnerStreamOpen)
        {
            _innerStream = stream;
            _leaveInnerStreamOpen = leaveInnerStreamOpen;
        }


        private int _chunkedLeft = 0;
        private bool _zeroChunkReceived = false;

        /// <summary>
        /// 重写Read方法
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_lineBuffer == null) _lineBuffer = new byte[8192];
            if (_chunkedLeft == 0)
            {
                if (_zeroChunkReceived) return 0;

                int chunkSize = ReadChunkSize();
                if (chunkSize == 0)
                {
                    ReadAndForward(true);
                    _zeroChunkReceived = true;
                    return 0;
                }
                _chunkedLeft = chunkSize;
            }
            if (count > _chunkedLeft) count = _chunkedLeft;
            count = _innerStream.Read(buffer, offset, count);
            _chunkedLeft -= count;
            if (_chunkedLeft == 0)
            {
                // 每次读完一个chunk后，把紧跟的CRLF也读出来扔掉
                ReadAndForward();
            }
            return count;
        }

        /// <summary>
        /// 每次读完一个chunk后，把紧跟的CRLF也读出来扔掉
        /// 如果是最后一个chunk，需要一直读读到空行，防止有的chunk带有tailer。
        /// </summary>
        /// <param name="isLastChunk"></param>
        private void ReadAndForward(bool isLastChunk = false)
        {
            if (isLastChunk)
            {
                while (!string.IsNullOrEmpty(ReadLine())) ;
                return;
            }
            string line = ReadLine();
            if (line == null) throw new Exception("连接关闭，chunk CRLF无法读取");
            if (line != "") throw new Exception("未发现chunk CRLF");
        }

        private int ReadChunkSize()
        {
            string line = ReadLine();
            if (line == null) throw new Exception("连接关闭，chunk-size无法读取");

            //如果带有chunk-extension，忽略extension，只取chunk-size
            int idx = line.IndexOf(' ');
            if (idx >= 0) line = line.Substring(0, idx);
            if (line == "") throw new Exception("数据不完整，chunk-size无法读取");

            if (!int.TryParse(line, System.Globalization.NumberStyles.HexNumber, null, out int chunkSize))
            {
                throw new Exception("长度数据错误，chunk-size无法读取");
            }
            return chunkSize;
        }


        private string ReadLine()
        {
            int offset = 0;
            int chr;
            //因为基础流是BufferedNetworkStream，我这里肆无忌惮的使用ReadByte^_^
            while ((chr = _innerStream.ReadByte()) > 0)
            {
                _lineBuffer[offset] = (byte)chr;
                if (chr == '\n')
                {
                    if (offset < 1 || _lineBuffer[offset - 1] != '\r')
                        throw new Exception("chunk-size read error, unformed");

                    if (offset == 1)
                        return "";

                    return Encoding.ASCII.GetString(_lineBuffer, 0, offset - 1);
                }
                offset++;
                if (offset >= _lineBuffer.Length)
                    throw new Exception("chunk-size read error, exceeds");
            }
            //连接丢失
            return null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_leaveInnerStreamOpen)
            {
                _innerStream?.Close();
            }
            _lineBuffer = null;
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
