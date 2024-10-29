using System.Net.Sockets;
using System.Net;
using System.Text;
using ServerCore;

namespace DummyClient
{
    class Packet
    {
        public ushort size; // 2byte
        public ushort packetId; // 2byte
    }

    /// <summary>
    /// 컨텐츠 단
    /// </summary>
    class GameSession : Session
    {
        public override void OnConnected(EndPoint _endPoint)
        {
            Console.WriteLine($"[Client] OnConnected : {_endPoint}");
            
            Packet packet = new Packet() { size = 4, packetId = 7};
            //보낸다.(블로킹 함수)
            for (int i = 0; i < 5; i++)
            {

                // 할당 받은 buffer 열어서 쪼개서 사용하고
                ArraySegment<byte> openSegment = SendBufferHelper.Open(4096);
                // [ 100 ] [10 ]
                byte[] buffer = BitConverter.GetBytes(packet.size);
                byte[] buffer2 = BitConverter.GetBytes(packet.packetId);

                // 시작지점, 인덱스, 목적지, 시작인덱스, 버퍼의 길이
                Array.Copy(buffer, 0, openSegment.Array, openSegment.Offset, buffer.Length);
                Array.Copy(buffer2, 0, openSegment.Array, openSegment.Offset + buffer.Length, buffer2.Length);

                // buffer 닫는다.
                // 넉넉하게 4096으로 잡아뒀지만, 예로 실질적 사용은 4byte(100), 4byte(10) 총 8byte만 보낸다.
                ArraySegment<byte> sendBuff = SendBufferHelper.Close(packet.size);


                Send(sendBuff);
            }
        }

        public override void OnDisConnected(EndPoint _endPoint)
        {
            Console.WriteLine($"[Client] OnDisConnected : {_endPoint}");
        }

        public override int OnRecv(ArraySegment<byte> _buffer)
        {
            // 무엇을 할건지 넣어주는 것.
            string recvData = Encoding.UTF8.GetString(_buffer.Array, _buffer.Offset, _buffer.Count);
            Console.WriteLine($"[From Sever] {recvData}");
            return _buffer.Count;
        }

        public override void OnSend(int _numOfBytes)
        {
            Console.WriteLine($"[Client] Transferred bytes : {_numOfBytes}");
        }
    }



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
            Thread.Sleep(5000);

            Connector connector = new Connector();

            connector.Connect(endPoint, () => { return new GameSession(); });


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
