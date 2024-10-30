using ServerCore;
using System.Net;

namespace DummyClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // DNS (Domain Name System)
            string host = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddr = ipHost.AddressList[0]; //아이피가 여러개 있을수 있으며 배열로 ip를 반환함
            IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);
            //IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7777);

            //try
            //{
            //    TcpListener tcpListener = new TcpListener(IPAddress.Any, 7777);
            //    tcpListener.Start();
            //    Console.WriteLine("[Client] TcpListener Start");
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine("[Client] Error starting server: " + ex.Message);
            //}
            
            Thread.Sleep(1000); // 서버보다 먼저 시작하면 안된다.

            Connector connector = new Connector();

            connector.Connect(endPoint, () => { return new SeverSession(); });


            while (true)
            {
                try
                {
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

                Thread.Sleep(2000);
            }
        }
    }
}
