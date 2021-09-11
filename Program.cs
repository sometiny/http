using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IocpSharp.Http
{
    class Program
    {
        static void Main(string[] args)
        {
            HttpServerBase server = new WebSocketMessager();
            try
            {
                server.Start("0.0.0.0", 4189);
                Console.WriteLine("服务器启动成功，监听地址：" + server.LocalEndPoint.ToString());
            }catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.ReadLine();
        }
    }
}
