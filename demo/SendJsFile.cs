using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using IocpSharp.Http.Responsers;
using IocpSharp.Http.Streams;
using System.IO.Compression;
using IocpSharp.Server;

namespace IocpSharp.Http
{
    public class SendJsFile : TcpIocpServer
    {
        protected override void NewClient(Socket client)
        {
            HttpStream stream = new HttpStream(new NetworkStream(client, true), false);
            //捕获一个HttpRequest
            HttpRequest request = stream.Capture<HttpRequest>();

            //从request拿到Url
            string url = request.Url;

            //url是以'/'开头的，这里简单处理下，合成新路径
            //实际应用中，为了安全我会对url进行一个判断，判断有没有危险字符，例如'..'，这两个点可能会导致全盘读取任意文件
            string vueAt = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "." + url));

            if (!File.Exists(vueAt))
            {
                //404这里直接关闭了基础流，所以既没有写Content-Length，也没有用Chunked传输
                //用完关闭，让客户端读取完所有数据即可。
                HttpResponser notFound = new HttpResponser(404);
                notFound.ContentType = "text/html; charset=utf-8";
                notFound.KeepAlive = false;
                notFound.Write(stream, $"文件'{vueAt}'未找到。");
                stream.Close();
                return;
            }

            //实例化ChunkedResponser类
            HttpResponser responser = new ChunkedResponser();

            //使用Content-Encoding标头，告诉客户端发送的是经过Gzip压缩的数据。 
            responser["Content-Encoding"] = "gzip";

            //发送的是JS文件，设置Content-Type
            responser.ContentType = "text/javascript";
            responser.KeepAlive = false;

            //使用ChunkedResponser打开一个写入流ChunkedWriteStream
            //GzipStream压缩后，直接写入ChunkedWriteStream
            //介意套娃的同学可以使用简单写法。
            using (Stream output = responser.OpenWrite(stream))
            {
                //实例化Gzip压缩流，压缩后写入output
                using (GZipStream input = new GZipStream(output, CompressionMode.Compress))
                {
                    //打开文件，读取数据
                    using (FileStream source = File.OpenRead(vueAt))
                    {
                        //使用原生方法，把文件数据拷贝到Gzip压缩流
                        source.CopyTo(input);
                    }
                }
            }

            //这一句不能忘记，写入结束包
            responser.End(stream);

            stream.Close();
        }
    }
}
