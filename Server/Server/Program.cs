using System.Net;
using System.Net.Sockets;
using System.Text;
using ServerCore;

namespace Server
{
    class Program
    {
        /// <summary>
        /// 컨텐츠 단
        /// </summary>
        class GameSession : Session
        {
            public override void OnConnected(EndPoint _endPoint)
            {
                Console.WriteLine($"OnConnected : {_endPoint}");

                byte[] sendBuff = Encoding.UTF8.GetBytes("Welcome to MMORPG Server !");
                Send(sendBuff);
                Thread.Sleep(1000);
                Disconnect();
            }

            public override void OnDisConnected(EndPoint _endPoint)
            {
                Console.WriteLine($"OnDisConnected : {_endPoint}");
            }

            public override int OnRecv(ArraySegment<byte> _buffer)
            {
                // 무엇을 할건지 넣어주는 것.
                string recvData = Encoding.UTF8.GetString(_buffer.Array, _buffer.Offset, _buffer.Count);
                Console.WriteLine($"[From Client] {recvData}");
                return _buffer.Count;
            }

            public override void OnSend(int _numOfBytes)
            {
                Console.WriteLine($"Transferred bytes : {_numOfBytes}");
            }
        }
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

                listener.Init(endPoint, () => { return new GameSession(); });
                Console.WriteLine("Listening...");

                while (true)
                {
                    ;

                }
            }
        }
    }
}
