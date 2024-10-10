using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServerCore
{
    class program
    {
        static Listener listener = new Listener();
        static void OnAcceptHandler(Socket _clientSocket)
        {
            try
            {
                Session session = new Session();
                session.Start(_clientSocket);

                byte[] sendBuff = Encoding.UTF8.GetBytes("Welcome to MMORPG Server !");
                session.Send(sendBuff);

                Thread.Sleep(1000);

                session.Disconnect();
                session.Disconnect();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }


        static void Main(string[] args)
        {
            // DNS (Domain Name System)
            string host = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddr = ipHost.AddressList[0]; //아이피가 여러개 있을수 있으며 배열로 ip를 반환함
            IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);

            listener.Init(endPoint, OnAcceptHandler);
            Console.WriteLine("Listening...");

            while (true)
            {
                ;

            }
        }
    }
}
