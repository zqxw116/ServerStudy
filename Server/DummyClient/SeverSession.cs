using ServerCore;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DummyClient
{

    // 패킷 header
    public abstract class Packet
    {
        public ushort size; // 2byte
        public ushort packetId; // 2byte

        public abstract ArraySegment<byte> Write();
        public abstract void Read(ArraySegment<byte> _s);
    }

    class PlayerInfoReq : Packet 
    {
        public long playerId; // 8byte

        public PlayerInfoReq()
        {
            this.packetId = (ushort)PacktID.PlayerInfoReq;
        }

        public override void Read(ArraySegment<byte> _s)
        {
            // 지금까지 몇 byte를 넣었는지
            ushort count = 0;

            //ushort szie = BitConverter.ToUInt16(_s.Array, _s.Offset);
            count += 2;
            
            // 여기까지 들어온거는 내 자신이다 라는 뜻. 그래서 id 추츨은 굳이 필요 없다
            ushort id = BitConverter.ToUInt16(_s.Array, _s.Offset + count); // size 더해줌
            count += 2;

            // span은 범위를 지정해서 찝어주는 형식
            // 범위를 찝어주면서 확실히 몇 byte인지 지정한거라
            // 범위를 초과해 버리면 exception이 발생. 그것을 캐치해서 사용하면 됨.
            this.playerId = BitConverter.ToInt64(new ReadOnlySpan<byte>(_s.Array, _s.Offset + count, _s.Count - count));
            count += 8;
        }

        // 우리가 컨트롤하기 때문에 아무 문제 없다.
        public override ArraySegment<byte> Write()
        { 
            // 할당 받은 buffer 열어서 원하는 사이즈를 확보하고
            ArraySegment<byte> s = SendBufferHelper.Open(4096); //openSegment
            bool success = true;
            // 지금까지 몇 byte를 넣었는지
            ushort count = 0; // ushort로 해야 size가 맞다

            // [] [] [] [] [] [] [] []  <- 2byte를 넣어주면 그 다음으로 buffer가 줄어야한다.

            count += 2; // size(ushort = 2byte) 만큼
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + count, s.Count - count), this.packetId);
            count += 2;  // packetId(ushort = 2btye) 만큼 
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + count, s.Count - count), this.playerId);
            count += 8; // playerId(ulong = 8byte) 만큼

            // 반드시 size는 마지막에 시도해야한다. 왜냐면 byte를 다 더해준 것을 마지막에 알기 때문이다
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset, s.Count), count);

            if (success == false) 
                return null;

            // buffer 닫는다.
            return SendBufferHelper.Close(count);

        }
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

            PlayerInfoReq packet = new PlayerInfoReq() { playerId = 1001 };
            //보낸다
            //for (int i = 0; i < 5; i++)
            {
                // 나중에 패킷을 주고 받을 때 packet class 만들고  write해서
                // serialize 해줘서 byte 배열로 만들어 주면 된다.
                ArraySegment<byte> s = packet.Write();

                if (s != null)
                    Send(s);
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
