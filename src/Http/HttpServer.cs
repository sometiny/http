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
using IocpSharp.Http.Utils;

namespace IocpSharp.Http
{
    public class HttpServer : TcpIocpServer
    {
        public HttpServer() : base()
        {
        }

        protected override void NewClient(Socket client)
        {
            BufferedNetworkStream baseStream = new BufferedNetworkStream(client, true);
            HttpRequest request = HttpRequest.Capture(baseStream);
        }
    }
}
