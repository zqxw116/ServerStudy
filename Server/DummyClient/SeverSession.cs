using ServerCore;
using System.Net;
using System.Text;

namespace DummyClient
{

    // 패킷 header
    class Packet
    {
        public ushort size; // 2byte
        public ushort packetId; // 2byte
    }

    class PlayerInfoReq : Packet 
    {
        public long playerId; // 8byte
    }
    class PlayerInfoOk : Packet
    {
        public int hp;
        public int attack;
    }
    public enum PacktID
    {
        PlayerInfoReq = 1,
        PlayerInfoOk = 2,
    }


    // 서버의 대리자가 SeverSession
    class SeverSession : Session
    {
        public override void OnConnected(EndPoint _endPoint)
        {
            Console.WriteLine($"[Client] OnConnected : {_endPoint}");

            PlayerInfoReq packet = new PlayerInfoReq() {packetId = (ushort)PacktID.PlayerInfoReq, playerId = 1001 };
            //보낸다
            //for (int i = 0; i < 5; i++)
            {

                // 할당 받은 buffer 열어서 원하는 사이즈를 확보하고
                ArraySegment<byte> s = SendBufferHelper.Open(4096); //openSegment
                bool success = true;
                // 지금까지 몇 byte를 넣었는지
                ushort count = 0; // ushort로 해야 size가 맞다

                // [] [] [] [] [] [] [] []  <- 2byte를 넣어주면 그 다음으로 buffer가 줄어야한다.
                
                count += 2; // size(ushort = 2byte) 만큼
                success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + count, s.Count - count), packet.packetId);
                count += 2;  // packetId(ushort = 2btye) 만큼 
                success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + count, s.Count - count), packet.playerId);
                count += 8; // playerId(ulong = 8byte) 만큼

                // 반드시 size는 마지막에 시도해야한다. 왜냐면 byte를 다 더해준 것을 마지막에 알기 때문이다
                success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset, s.Count), packet.size);

                // buffer 닫는다.
                ArraySegment<byte> sendBuff = SendBufferHelper.Close(count);

                if (success)
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


}
