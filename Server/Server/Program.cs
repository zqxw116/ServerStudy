using System.Net;
using System.Net.Sockets;
using System.Text;
using ServerCore;

namespace Server
{
    class Packet
    {
        public ushort size; // 2byte
        public ushort packetId; // 2byte
    }

    class Program
    {
        /// <summary>
        /// 컨텐츠 단
        /// </summary>
        class GameSession : PacketSession
        {
            public override void OnConnected(EndPoint _endPoint)
            {
                Console.WriteLine($"OnConnected : {_endPoint}");

                //Packet packet = new Packet() { size = 100, packetId = 10 };


                //// 할당 받은 buffer 열어서 쪼개서 사용하고
                //ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);
                //// [ 100 ] [10 ]
                //byte[] buffer = BitConverter.GetBytes(packet.size);
                //byte[] buffer2 = BitConverter.GetBytes(packet.packetId);

                //// 시작지점, 인덱스, 목적지, 시작인덱스, 버퍼의 길이
                //Array.Copy(buffer, 0, openSegment.Array, openSegment.Offset, buffer.Length);
                //Array.Copy(buffer2, 0, openSegment.Array, openSegment.Offset + buffer.Length, buffer2.Length);
                
                //// buffer 닫는다.
                //// 넉넉하게 4096으로 잡아뒀지만, 예로 실질적 사용은 4byte(100), 4byte(10) 총 8byte만 보낸다.
                //ArraySegment<byte> sendBuff = SendBufferHelper.Close(buffer.Length + buffer2.Length);
                


                // 100명
                // 1명 이동 -> 이동패킷 100명
                // 100명 이동 ->  이동패킷 100 * 100 = 1만
                // 외부에서 한 번만 만들어두고 사용하는게 효율적.

                //Send(sendBuff);
                Thread.Sleep(5000);
                Disconnect();
            }

            // 첫 2byte는 사이즈, 다음 2byte는 패킷 id
            public override void OnRecvPacket(ArraySegment<byte> _buffer)
            {
                ushort szie = BitConverter.ToUInt16(_buffer.Array, _buffer.Offset);
                ushort id = BitConverter.ToUInt16(_buffer.Array, _buffer.Offset + 2); // size 더해줌
                Console.WriteLine($"OnRecvPacket szie : {szie},  id : {id}");
                
            }

            public override void OnDisConnected(EndPoint _endPoint)
            {
                Console.WriteLine($"OnDisConnected : {_endPoint}");
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
