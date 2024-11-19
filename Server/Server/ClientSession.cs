using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace Server
{


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
                        Console.WriteLine($"[Sever] PlayerInfoReq : {p.playerId} {p.name}");

                        foreach (var skill in p.skills)
                        {
                            Console.WriteLine($"[Sever] Skill Info   id : {skill.id}, level : {skill.level}, duration : {skill.duration}");

                        }
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
