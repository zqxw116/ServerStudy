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
                // 받는다.(블로킹 함수)
                byte[] recvBuff = new byte[1024]; // 대략적인 버퍼크기 적용
                int recvBytes = _clientSocket.Receive(recvBuff);
                string recvData = Encoding.UTF8.GetString(recvBuff, 0, recvBytes);
                Console.WriteLine($"[From Client] {recvData}");

                // 전송 한다.(블로킹 함수)
                byte[] sendBuff = Encoding.UTF8.GetBytes("Welcome to MMORPG Server !");
                _clientSocket.Send(sendBuff);

                // 예고
                _clientSocket.Shutdown(SocketShutdown.Both); // 듣기도 싫고 말하기도 싫다?
                // 연결끊기
                _clientSocket.Close();
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
