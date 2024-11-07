using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace Server
{
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
            //ushort id = BitConverter.ToUInt16(_s.Array, _s.Offset + count); // size 더해줌
            count += 2;

            // span은 범위를 지정해서 찝어주는 형식
            // 범위를 찝어주면서 확실히 몇 byte인지 지정한거라
            // 범위를 초과해 버리면 exception이 발생. 그것을 캐치해서 사용하면 됨.
            this.playerId = BitConverter.ToInt64(new ReadOnlySpan<byte>(_s.Array, _s.Offset + count, _s.Count - count));
            count += 8;
        }

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
    /// <summary>
    /// 컨텐츠 단
    /// </summary>
    class ClientSession : PacketSession
    {
        public override void OnConnected(EndPoint _endPoint)
        {
            Console.WriteLine($"[Sever] OnConnected : {_endPoint}");

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
            Thread.Sleep(100);
            Disconnect();
        }

        // 첫 2byte는 사이즈, 다음 2byte는 패킷 id
        public override void OnRecvPacket(ArraySegment<byte> _buffer) // _buffer는 완성된 크기의 패킷
        {
            // 지금까지 몇 byte를 넣었는지
            ushort count = 0;
            ushort szie = BitConverter.ToUInt16(_buffer.Array, _buffer.Offset);
            count += 2;
            ushort id = BitConverter.ToUInt16(_buffer.Array, _buffer.Offset + count); // size 더해줌
            count += 2;

            switch ((PacktID)id)
            {
                case PacktID.PlayerInfoReq:
                    {
                        PlayerInfoReq p = new PlayerInfoReq();
                        p.Read(_buffer); // 역질려화 해서 buffer에 있는 값을 빼온다
                        Console.WriteLine($"PlayerInfoReq : {p.playerId}");

                    }
                    break;
            }

            Console.WriteLine($"OnRecvPacket szie : {szie},  id : {id}");
        }

        public override void OnDisConnected(EndPoint _endPoint)
        {
            Console.WriteLine($"[Sever] OnDisConnected : {_endPoint}");
        }


        public override void OnSend(int _numOfBytes)
        {
            Console.WriteLine($"[Sever] Transferred bytes : {_numOfBytes}");
        }
    }
}
