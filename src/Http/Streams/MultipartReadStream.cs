using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using IocpSharp.Http.Utils;


namespace IocpSharp.Http.Streams
{
    public class MultipartReadStream : Stream
    {
        private Stream _innerStream = null;
        private bool _leaveInnerStreamOpen = true;
        private byte[] _boundary = null;
        private int[] _bmBc = null;
        private int[] _bmGs = null;

        /// <summary>
        /// 实例化读取
        /// </summary>
        /// <param name="boundary">分隔符</param>
        /// <param name="stream">基础流</param>
        /// <param name="leaveInnerStreamOpen"></param>
        public MultipartReadStream(string boundary, Stream stream, bool leaveInnerStreamOpen)
        {
            _innerStream = stream;
            _leaveInnerStreamOpen = leaveInnerStreamOpen;
            _boundary = Encoding.ASCII.GetBytes("\r\n" + boundary);
            BoyerMoore.PrepareBoyerMoore(_boundary, out _bmBc, out _bmGs);
        }

        private byte[] _tempBlock = new byte[32768];

        private int _tempBlockLeft = 0;
        private int _tempBlockOffset = 0;
        private int _blockedOffset = 0;
        private int _blockedLeft = 0;
        private bool _blockHeadRead = false;
        private bool _blockEndingFound = false;
        private bool _readAfterEmpty = false;

        public bool BlockHeadRead { get => _blockHeadRead; internal set => _blockHeadRead = value; }
        public bool BlockEndingFound { get => _blockEndingFound; internal set => _blockEndingFound = value; }

        private int ReadFromTempBlock(byte[] buffer, int offset, int count)
        {
            if (count > _tempBlockLeft) count = _tempBlockLeft;
            Array.Copy(_tempBlock, _tempBlockOffset, buffer, offset, count);
            _tempBlockOffset += count;
            _tempBlockLeft -= count;
            return count;
        }
        private int ReadFromTempBlockWithOffset(byte[] buffer, int offset, int count)
        {
            if (count > _blockedLeft) count = _blockedLeft;
            Array.Copy(_tempBlock, _blockedOffset, buffer, offset, count);
            _blockedOffset += count;
            _blockedLeft -= count;
            return count;
        }
        private int ReadHeader(byte[] buffer, int offset, int count)
        {

            if (_tempBlockLeft > 0)
            {
                return ReadFromTempBlock(buffer, offset, count);
            }
            return _innerStream.Read(buffer, offset, count);
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
            if (!_blockHeadRead)
            {
                _readAfterEmpty = false;
                return ReadHeader(buffer, offset, count);
            }
            //临时块里面有数据的时候，优先从临时块里面读
            if (_blockedLeft > 0)
            {
                return ReadFromTempBlockWithOffset(buffer, offset, count);
            }
            if (_blockEndingFound) return 0;

            if (_readAfterEmpty || _tempBlockLeft == 0)
            {
                if (_tempBlockLeft > 0)
                {
                    for (int i = _tempBlockOffset; i < _tempBlockOffset + _tempBlockLeft; i++)
                    {
                        _tempBlock[i - _tempBlockOffset] = _tempBlock[i];
                    }
                }
                _tempBlockOffset = 0;

                int rec = _innerStream.Read(_tempBlock, _tempBlockLeft, _tempBlock.Length - _tempBlockLeft);
                _tempBlockLeft += rec;
                if (rec == 0) return 0;
            }

            int idxExtra = Search(_boundary, _tempBlock, _tempBlockOffset, _tempBlockOffset + _tempBlockLeft, out int nextPosition);
            if (idxExtra >= 0)
            {
                _blockedOffset = _tempBlockOffset;
                _blockedLeft = idxExtra - _tempBlockOffset;


                _tempBlockLeft = (_tempBlockOffset + _tempBlockLeft) - idxExtra - _boundary.Length;
                _tempBlockOffset = idxExtra + _boundary.Length;
                if (_tempBlockLeft == 0) _tempBlockOffset = 0;
                _blockEndingFound = true;
                return Read(buffer, offset, count);
            }

            if (nextPosition == -1)
            {
                _readAfterEmpty = true;
                return Read(buffer, offset, count);
            }

            _readAfterEmpty = nextPosition + _boundary.Length > _tempBlockOffset + _tempBlockLeft;
            _blockedOffset = _tempBlockOffset;
            _blockedLeft = nextPosition - _tempBlockOffset;

            _tempBlockLeft = (_tempBlockOffset + _tempBlockLeft) - nextPosition;
            _tempBlockOffset = nextPosition;
            if (_tempBlockLeft == 0) _tempBlockOffset = 0;
            return Read(buffer, offset, count);

        }


        /// <summary>
        /// BoyerMoore算法的实现，稍微修改，以适用本系统
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="source"></param>
        /// <param name="startIndex"></param>
        /// <param name="length"></param>
        /// <param name="nextPosition"></param>
        /// <returns></returns>
        private int Search(byte[] pattern, byte[] source, int startIndex, int length, out int nextPosition)
        {
            int i, j;
            int m = pattern.Length;
            nextPosition = -1;
            j = startIndex;
            while (j <= length - m)
            {
                for (i = m - 1; i >= 0 && pattern[i] == source[i + j]; i--) ;
                if (i < 0)
                {
                    return j;
                }
                else
                {
                    j += BoyerMoore.Max(_bmBc[source[i + j]] - m + 1 + i, _bmGs[i]);
                    nextPosition = j;
                }
            }
            return -1;
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
