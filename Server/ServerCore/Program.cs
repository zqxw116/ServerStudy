using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServerCore
{
    class program
    {
        
        static void Main(string[] args)
        {
            // DNS (Domain Name System)
            // 172.1.2.3 이렇게 직접 하드코딩 해도 되지만, 경우에 따라 IP가 변경될 수 있지만 수정할 수 없다,
            // 도메인을 하나 등록하고 www.rookiss.com -> 123.123.123.12 이렇게 IP를 연결하면 관리가 더 쉬워진다.
            string host = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddr = ipHost.AddressList[0]; // 구글같이 많은 트래픽이 있는 경우 IP가 여러개 일 수 있다.
                                                                
            IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777); // port는 숫자를 하나 지정하는 것 뿐
                                                                // (식당주소, 식당 문 번호)
            // 문지기
            Socket listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            // AddressFamily = ip 버전 무엇을 할건지? 우리는 이미 Dns를 통해서 ip 버전이 정해졌다.

            try
            {
                // 문지기 교육
                listenSocket.Bind(endPoint); // 포트번호 기입

                // 영업 시작
                // backlog : 최대 대기수
                listenSocket.Listen(10); // 최대 몇 명 들어올 수 있는지

                while (true)
                {
                    Console.WriteLine("Listening...");

                    // 손님을 입장시킨다
                    // 대리인의 소켓
                    Socket clientSocket = listenSocket.Accept();

                    // 받는다
                    byte[] recvBuff = new byte[1024]; // 대략적인 버퍼크기 적용
                    int recvBytes = clientSocket.Receive(recvBuff);

                    string recvData = Encoding.UTF8.GetString(recvBuff, 0, recvBytes);
                    Console.WriteLine($"[From Client] {recvData}");

                    // 보낸다
                    byte[] sendBuff = Encoding.UTF8.GetBytes("Welcome to MMORPG Server !");
                    clientSocket.Send(sendBuff);

                    // 쫒아낸다
                    clientSocket.Shutdown(SocketShutdown.Both); // 듣기도 싫고 말하기도 싫다?
                    clientSocket.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
