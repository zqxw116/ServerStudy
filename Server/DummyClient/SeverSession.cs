using ServerCore;
using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;

namespace DummyClient
{

    // 서버의 대리자가 SeverSession
    class SeverSession : Session
    {
        public override void OnConnected(EndPoint _endPoint)
        {
            Console.WriteLine($"[Client] OnConnected : {_endPoint}");

            PlayerInfoReq packet = new PlayerInfoReq() { playerId = 1001, name = "ABCD" };

            var skill = new PlayerInfoReq.Skill() { id = 101, level = 1, duration = 3f };
            skill.attributess.Add(new PlayerInfoReq.Skill.Attributes() { att = 77 });
            packet.skills.Add(skill);

            packet.skills.Add(new PlayerInfoReq.Skill() { id = 201, level =2, duration = 4f});
            packet.skills.Add(new PlayerInfoReq.Skill() { id = 301, level =3, duration = 5f});
            packet.skills.Add(new PlayerInfoReq.Skill() { id = 401, level =4, duration = 6f});

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
