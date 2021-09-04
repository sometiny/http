using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using IocpSharp.Http.Responsers;
using System.IO.Compression;
using IocpSharp.Server;

namespace IocpSharp.Http
{
    public class Gzip : TcpIocpServer
    {
        protected override void NewClient(Socket client)
        {
            Stream stream = new NetworkStream(client, true);
            //捕获一个HttpRequest
            HttpRequest request = HttpRequest.Capture(stream);

            //准备发送到客户端的数据
            string responseText = $"<p>hello world!</p><pre>{request.GetAllRequestHeaders()}</pre>";

            byte[] responseBuffer = Encoding.ASCII.GetBytes(responseText);

            //压缩数据
            responseBuffer = Compress(responseBuffer);

            HttpResponser responser = new HttpResponser();

            //使用Content-Encoding标头，告诉客户端发送的是经过Gzip压缩的数据。 
            responser["Content-Encoding"] = "gzip";

            responser.ContentLength = responseBuffer.Length;

            responser.ContentType = "text/html";
            responser.KeepAlive = false;

            responser.Write(stream, responseBuffer);


            stream.Close();
        }

        /// <summary>
        /// Gzip压缩
        /// </summary>
        /// <param name="source">原内容</param>
        /// <returns>压缩后内容</returns>
        private byte[] Compress(byte[] source)
        {

            //使用内存流保存压缩后的数据
            using (MemoryStream output = new MemoryStream())
            {
                using (GZipStream input = new GZipStream(output, CompressionMode.Compress))
                {
                    input.Write(source, 0, source.Length);
                }
                return output.ToArray();
            }
        }
    }
}
