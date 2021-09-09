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
    /// 因为Frame的数据是固定长度的，所以我们继承ContentedWriteStream即可
    /// </summary>
    public class FrameWriteStream : ContentedWriteStream
    {
        private byte[] _maskKey = null;
        private int _maskKeyOffset = 0;

        /// <summary>
        /// 使用指定长度、基础流和模式创建实例
        /// </summary>
        /// <param name="payloadLength">数据长度</param>
        /// <param name="stream"></param>
        /// <param name="leaveInnerStreamOpen"></param>
        public FrameWriteStream(byte[] maskKey, long payloadLength, Stream stream, bool leaveInnerStreamOpen) : base(stream, payloadLength, leaveInnerStreamOpen)
        {
            _maskKey = maskKey;
        }

        /// <summary>
        /// 重写Write方法
        /// 注意：如果有maskKey的话，这里会改变原始字节数据。
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if(_maskKey == null)
            {
                base.Write(buffer, offset, count);
                return;
            }

            for (int i = offset; i < count + offset; i++)
            {
                //maskKey是循环使用的，所以对4取模
                buffer[i] ^= _maskKey[_maskKeyOffset++ % 4];
            }
            base.Write(buffer, offset, count);
        }
    }
}
