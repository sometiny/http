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
            StartStaticServer(4189);

            Console.ReadLine();
        }
        private static void StartStaticServer(int port)
        {
            //静态文件服务器
            HttpServerBase server = new HttpServerUplaod();
            try
            {
                server.Start("0.0.0.0", port);
                Console.WriteLine("HTTP服务器启动成功，监听地址：" + server.LocalEndPoint.ToString());
                Console.WriteLine($"HTTP服务器根目录：{server.WebRoot}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
