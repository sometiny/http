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
    /// 因为Frame的数据是固定长度的，所以我们继承ContentedReadStream即可
    /// </summary>
    public class FrameReadStream : ContentedReadStream
    {
        private byte[] _maskKey = null;
        private int _maskKeyOffset = 0;

        /// <summary>
        /// 使用指定长度、基础流和模式创建实例
        /// </summary>
        /// <param name="payloadLength">数据长度</param>
        /// <param name="stream"></param>
        /// <param name="leaveInnerStreamOpen"></param>
        public FrameReadStream(byte[] maskKey, long payloadLength, Stream stream, bool leaveInnerStreamOpen) : base(payloadLength, stream, leaveInnerStreamOpen)
        {
            _maskKey = maskKey;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int rec = base.Read(buffer, offset, count);

            if (_maskKey == null || rec == 0) return rec;

            for(int i = offset; i < rec + offset; i++)
            {
                //maskKey是循环使用的，所以对4取模
                buffer[i] ^= _maskKey[_maskKeyOffset++ % 4];
            }

            return rec;
        }
    }
}
