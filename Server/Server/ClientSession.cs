using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace Server
{

    public enum PacktID
    {
        PlayerInfoReq = 1,
        Test = 2,

    }


    class PlayerInfoReq
    {
        public byte testByte;
        public long playerId;
        public string name;

        public class Skill
        {
            public int id;
            public short level;
            public float duration;

            public class Attributes
            {
                public int att;

                public void Read(ReadOnlySpan<byte> s, ref ushort count)
                {
                    // span은 범위를 지정해서 찝어주는 형식
                    // 범위를 찝어주면서 확실히 몇 byte인지 지정한거라
                    // 범위를 초과해 버리면 exception이 발생. 그것을 캐치해서 사용하면 됨.
                    this.att = BitConverter.ToInt32(s.Slice(count, s.Length - count));
                    count += sizeof(int);
                }

                //              전체 byte 배열, 실시간 몇 번째 count 작업중인지
                public bool Write(Span<byte> s, ref ushort count)
                {
                    bool success = true;
                    success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.att);
                    count += sizeof(int); // playerId(byte) 만큼
                    return success;
                }

            }
            public List<Attributes> attributess = new List<Attributes>();


            public void Read(ReadOnlySpan<byte> s, ref ushort count)
            {
                // span은 범위를 지정해서 찝어주는 형식
                // 범위를 찝어주면서 확실히 몇 byte인지 지정한거라
                // 범위를 초과해 버리면 exception이 발생. 그것을 캐치해서 사용하면 됨.
                this.id = BitConverter.ToInt32(s.Slice(count, s.Length - count));
                count += sizeof(int);
                this.level = BitConverter.ToInt16(s.Slice(count, s.Length - count));
                count += sizeof(short);
                this.duration = BitConverter.ToSingle(s.Slice(count, s.Length - count));
                count += sizeof(float);
                // skill slit
                this.attributess.Clear(); // 안전하게 이전꺼 clear
                ushort attributesLen = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
                count += sizeof(ushort);

                for (int i = 0; i < attributesLen; i++)
                {
                    Attributes attributes = new Attributes();
                    attributes.Read(s, ref count);
                    attributess.Add(attributes);
                }
            }

            //              전체 byte 배열, 실시간 몇 번째 count 작업중인지
            public bool Write(Span<byte> s, ref ushort count)
            {
                bool success = true;
                success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.id);
                count += sizeof(int); // playerId(byte) 만큼
                success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.level);
                count += sizeof(short); // playerId(byte) 만큼
                success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.duration);
                count += sizeof(float); // playerId(byte) 만큼
                                        // skill list
                success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)this.attributess.Count); // count는 4byte니까 2byte로 변경
                count += sizeof(ushort); // (ushort)listSkils.Count // count는 4byte니까 2byte로 변경

                foreach (Attributes attributes in this.attributess)
                {
                    // span으로 찝어둔 영역에 하나씩 넣을 것이다.
                    // Write 안에서 count가 늘어난다
                    success &= attributes.Write(s, ref count);
                }

                return success;
            }

        }
        public List<Skill> skills = new List<Skill>();


        public void Read(ArraySegment<byte> segment)
        {
            // 지금까지 몇 byte를 넣었는지
            ushort count = 0;

            ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
            count += sizeof(ushort);
            count += sizeof(ushort);

            this.testByte = (byte)segment.Array[segment.Offset + count];
            count += sizeof(byte);
            this.playerId = BitConverter.ToInt64(s.Slice(count, s.Length - count));
            count += sizeof(long);
            ushort nameLen = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
            count += sizeof(ushort);

            // 누군가가 패킷을 조작할 수 있기 때문에 만약의 사테를 두고 작업해야함
            this.name = Encoding.Unicode.GetString(s.Slice(count, nameLen));
            count += nameLen;
            // skill slit
            this.skills.Clear(); // 안전하게 이전꺼 clear
            ushort skillLen = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
            count += sizeof(ushort);

            for (int i = 0; i < skillLen; i++)
            {
                Skill skill = new Skill();
                skill.Read(s, ref count);
                skills.Add(skill);
            }
        }


        // 우리가 컨트롤하기 때문에 아무 문제 없다.
        public ArraySegment<byte> Write()
        {
            // 할당 받은 buffer 열어서 원하는 사이즈를 확보하고
            ArraySegment<byte> segment = SendBufferHelper.Open(4096); //openSegment
            bool success = true;
            // 지금까지 몇 byte를 넣었는지
            ushort count = 0; // ushort로 해야 size가 맞다

            Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);

            // slice는 span 자체가 변하는게 아니라 결과값이 span의 byte로 뽑혀오기 때문.
            // span 에서 slice로 바꾼 이유는 span이라는 타입으로 반환하기 때문에 그것을 사용. 코드가 깔끔
            count += sizeof(ushort); // size(ushort = 2byte) 만큼
            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)PacktID.PlayerInfoReq);
            count += sizeof(ushort);  // packetId(ushort = 2btye) 만큼 

            segment.Array[segment.Offset + count] = (byte)this.testByte;
            count += sizeof(byte);
            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.playerId);
            count += sizeof(long); // playerId(byte) 만큼
                                   // name 건네주고 0부터 시작해서, name을 전체 복사할건데, segment 배열에다가 segment.Offset + count 만큼 + 추가 ushort만큼 공간을 마련해야함(name Length).
            ushort nameLen = (ushort)Encoding.Unicode.GetBytes(this.name, 0, this.name.Length, segment.Array, segment.Offset + count + sizeof(ushort));
            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), nameLen);
            count += sizeof(ushort);
            count += nameLen;
            // skill list
            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)this.skills.Count); // count는 4byte니까 2byte로 변경
            count += sizeof(ushort); // (ushort)listSkils.Count // count는 4byte니까 2byte로 변경

            foreach (Skill skill in this.skills)
            {
                // span으로 찝어둔 영역에 하나씩 넣을 것이다.
                // Write 안에서 count가 늘어난다
                success &= skill.Write(s, ref count);
            }


            // 반드시 size는 마지막에 시도해야한다. 왜냐면 byte를 다 더해준 것을 마지막에 알기 때문이다
            success &= BitConverter.TryWriteBytes(s, count);
            if (success == false)
                return null;

            // buffer 닫는다.
            return SendBufferHelper.Close(count);

        }
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
