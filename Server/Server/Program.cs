using ServerCore;
using System.Net;

namespace Server
{
    class program
    {
        static Listener listener = new Listener();

        static void Main(string[] args)
        {
            // DNS (Domain Name System)
            string host = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddr = ipHost.AddressList[0]; //아이피가 여러개 있을수 있으며 배열로 ip를 반환함
            IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);
            //IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7777);

            listener.Init(endPoint, () => { return new ClientSession(); });
            Console.WriteLine("[Sever] Listening...");

            while (true)
            {
                ;

            }
        }
    }
}
