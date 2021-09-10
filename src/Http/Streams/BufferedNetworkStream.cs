using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using IocpSharp.Http.Utils;

namespace IocpSharp.Http.Streams
{
    //继承NetworkStream
    public class BufferedNetworkStream : NetworkStream
    {
        /// <summary>
        /// 获取基础Socket
        /// </summary>
        public Socket BaseSocket
        {
            get
            {
                return Socket;
            }
        }
        /// <summary>
        /// 实现NetworkStream的两个构造方法
        /// </summary>
        /// <param name="baseSocket">基础Socket</param>
        public BufferedNetworkStream(Socket baseSocket) : base(baseSocket) { }

        /// <summary>
        /// 实现NetworkStream的两个构造方法
        /// </summary>
        /// <param name="baseSocket">基础Socket</param>
        /// <param name="ownSocket">是否拥有Socket，为true的话，在Stream关闭的同时，关闭Socket</param>
        public BufferedNetworkStream(Socket baseSocket, bool ownSocket) : base(baseSocket, ownSocket) { }

        /// <summary>
        /// 定义变量，标识当前流是否在Buffered模式下运行
        /// 默认为true
        /// 我们可以适时关闭buffered模式
        /// 例如在两个流拷贝数据的时候，都是大的数据块，没必要再去缓冲
        /// </summary>
        private bool _buffered = true;
        public bool Buffered
        {
            get => _buffered;
            set => _buffered = value;
        }

        /// <summary>
        /// 定义缓冲区，程序会尽可能读满缓冲区
        /// 可以根据不同的应用，合理设置这个值
        /// </summary>
        private byte[] _buffer = new byte[32768];

        /// <summary>
        /// 缓冲区中数据的读取索引
        /// </summary>
        private int _offset = 0;

        /// <summary>
        /// 缓冲区中的可用数据长度，缓冲区没有数据时，尝试读数据到缓冲区
        /// </summary>
        private int _length = 0;

        /// <summary>
        /// 重写ReadByte，直接从缓冲区拿数据
        /// 这里可以不重写这个方法，但是防止base.ReadByte频繁创建1字节的缓冲区，我们也把这个方法重写
        /// </summary>
        /// <returns></returns>
        public override int ReadByte()
        {
            if (_length > 0)
            {
                _length--;
                return _buffer[_offset++];
            }
            return base.ReadByte();
        }

        /// <summary>
        /// 重写Read方法，用户从缓冲区中读数据
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int size)
        {
            //缓冲区没有数据，从基础流中读取数据到缓冲区。
            if (_length == 0 && _buffered)
            {
                //索引恢复到起始位置
                _offset = 0;
                _length = base.Read(_buffer, 0, _buffer.Length);

                //没有从基础流读到数据，直接返回，代表流已经读完了所有数据。
                if (_length == 0) return 0;
            }
            //能进入这个分支，说明_buffered为false，并且缓冲区内没有数据了，直接从基础流读数据
            //否则会一直从缓冲区内读数据，直到清空缓冲区
            if (_length == 0)
            {
                return base.Read(buffer, offset, size);
            }
            return CopyFromBuffer(buffer, offset, size);
        }

        /// <summary>
        /// 重写BeginRead方法，用户从缓冲区中读数据
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
        {
            //代码逻辑同Read方法一样
            if (_length == 0 && _buffered)
            {
                _offset = 0;
                //使用我们自己实现的IAsyncResult类
                BufferedAsyncReadResult asyncResult = new BufferedAsyncReadResult(callback, state, buffer, offset, size);

                //从基础流中读取数据，并且在回调里面处理数据的拷贝和处理
                base.BeginRead(_buffer, _offset, _buffer.Length, AfterRead, asyncResult);
                return asyncResult;

            }

            //缓冲区内有数据，直接拷贝给下游应用，同样使用我们自己实现的IAsyncResult类
            if (_length > 0)
            {
                int rec = CopyFromBuffer(buffer, offset, size);
                BufferedAsyncReadResult asyncResult = new BufferedAsyncReadResult(callback, state, buffer, offset, size);
                asyncResult.BytesTransfered = rec;
                asyncResult.CallUserCallback();
                return asyncResult;
            }
            
            //没开启缓冲，直接调用基类方法
            return base.BeginRead(buffer, offset, size, callback, state);
        }

        /// <summary>
        /// 从基础流读数据到缓冲区的回调。
        /// </summary>
        /// <param name="asyncResult"></param>
        private void AfterRead(IAsyncResult asyncResult)
        {
            //AsyncState属性值是我们自己实现的IAsyncResult类
            BufferedAsyncReadResult asyncReadResult = asyncResult.AsyncState as BufferedAsyncReadResult;
            try
            {
                int rec = base.EndRead(asyncResult);
                if (rec == 0)
                {
                    //没读到数据，直接返回
                    asyncReadResult.BytesTransfered = 0;
                    asyncReadResult.CallUserCallback();
                    return;
                }
                //拷贝数据给下游应用
                _length += rec;
                asyncReadResult.BytesTransfered = CopyFromBuffer(asyncReadResult.Buffer, asyncReadResult.Offset, asyncReadResult.Count);
                asyncReadResult.CallUserCallback();
            }
            catch (Exception e)
            {
                //设置异步调用异常
                asyncReadResult.SetFailed(e);
            }
        }

        /// <summary>
        /// 重写EndRead方法
        /// </summary>
        /// <param name="asyncResult"></param>
        /// <returns></returns>
        public override int EndRead(IAsyncResult asyncResult)
        {
            //如果asyncResult是我们自己实现的IAsyncResult类
            if (asyncResult is BufferedAsyncReadResult asyncReadResult)
            {
                //有异常，抛出异常给下游应用
                if (asyncReadResult.Exception != null)
                    throw asyncReadResult.Exception;

                //返回实际传输的数据大小。
                return asyncReadResult.BytesTransfered;
            }
            return base.EndRead(asyncResult);
        }

        private int CopyFromBuffer(byte[] buffer, int offset, int size)
        {
            //如果要求读的数据超过了缓冲区内数据的大小，则只返回缓冲区内的可用数据
            if (size > _length) size = _length;

            //从缓冲区拷贝数据到应用
            Array.Copy(_buffer, _offset, buffer, offset, size);

            //数据拷贝完成，移动偏移，修改缓冲区的可能数据长度
            _offset += size;
            _length -= size;

            return size;
        }

        /// <summary>
        /// 重写Dispose，释放下缓冲区
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            _buffer = null;
            base.Dispose(disposing);
        }

    }
}
