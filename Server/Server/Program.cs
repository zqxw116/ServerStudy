using System.Net;
using System.Net.Sockets;
using System.Text;
using ServerCore;

namespace Server
{
    class Knight
    {
        public int hp;
        public int  attack;
        public string name;
        public List<int> skills = new List<int>();
    }


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

                Knight knight = new Knight() { hp = 100, attack = 10 };


                // 할당 받은 buffer 열어서 쪼개서 사용하고
                ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);
                // [ 100 ] [10 ]
                byte[] buffer = BitConverter.GetBytes(knight.hp);
                byte[] buffer2 = BitConverter.GetBytes(knight.attack);

                // 시작지점, 인덱스, 목적지, 시작인덱스, 버퍼의 길이
                Array.Copy(buffer, 0, openSegment.Array, openSegment.Offset, buffer.Length);
                Array.Copy(buffer2, 0, openSegment.Array, openSegment.Offset + buffer.Length, buffer2.Length);
                
                // buffer 닫는다.
                // 넉넉하게 4096으로 잡아뒀지만, 예로 실질적 사용은 4byte(100), 4byte(10) 총 8byte만 보낸다.
                ArraySegment<byte> sendBuff = SendBufferHelper.Close(buffer.Length + buffer2.Length);
                


                // 100명
                // 1명 이동 -> 이동패킷 100명
                // 100명 이동 ->  이동패킷 100 * 100 = 1만
                // 외부에서 한 번만 만들어두고 사용하는게 효율적.

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
