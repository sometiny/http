using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IocpSharp.Http.Utils
{
    /// <summary>
    /// 基于我们自己封装的AsyncResult，再封装一个用于读写数据的AsyncReadWriteResult
    /// 在重写NetworkStream的BeginRead/BeginWrite方法时会用到
    /// </summary>
    public class AsyncReadWriteResult : AsyncResult<object>
    {
        /// <summary>
        /// 实际Read/Write传输的数据大小
        /// </summary>
        public int BytesTransfered { get; internal set; } = 0;

        /// <summary>
        /// 缓冲区，用于Read/Write
        /// </summary>
        public byte[] Buffer { get; internal set; } = null;

        /// <summary>
        /// 数据在缓冲区中的偏移
        /// </summary>
        public int Offset { get; internal set; } = 0;

        /// <summary>
        /// 准备传输的数据大小
        /// </summary>
        public int Count { get; internal set; } = 0;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="callback">回调函数</param>
        /// <param name="state">用户状态</param>
        public AsyncReadWriteResult(AsyncCallback callback, object state) : this(callback, state, null, 0, 0)
        {
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="callback">回调函数</param>
        /// <param name="state">用户状态</param>
        /// <param name="buffer">设置缓冲区</param>
        /// <param name="offset">数据在缓冲区中的偏移</param>
        /// <param name="count">准备传输的数据大小</param>
        public AsyncReadWriteResult(AsyncCallback callback, object state, byte[] buffer, int offset, int count)
            : base(callback, state)
        {
            Buffer = buffer;
            Offset = offset;
            Count = count;
        }
        protected override void Dispose(bool disposing)
        {
            Buffer = null;
            base.Dispose(disposing);
        }
    }
}
