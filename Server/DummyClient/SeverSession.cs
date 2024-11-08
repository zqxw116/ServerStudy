using ServerCore;
using System;
using System.Collections.Specialized;
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
        public string name; // 몇 byte인지 모른다.

        
        public struct SkillInfo
        {
            public int id;
            public short level;
            public float duration;

            //              전체 byte 배열, 실시간 몇 번째 count 작업중인지
            public bool Write(Span<byte> _s, ref ushort _count)
            {
                bool success = true;
                success &= BitConverter.TryWriteBytes(_s.Slice(_count, _s.Length - _count), id);
                _count += sizeof(int);
                success &= BitConverter.TryWriteBytes(_s.Slice(_count, _s.Length - _count), level);
                _count += sizeof(ushort);
                success &= BitConverter.TryWriteBytes(_s.Slice(_count, _s.Length - _count), duration);
                _count += sizeof(float);
                return success;
            }

            public void Read(ReadOnlySpan<byte> _s, ref ushort _count)
            {
                // span은 범위를 지정해서 찝어주는 형식
                // 범위를 찝어주면서 확실히 몇 byte인지 지정한거라
                // 범위를 초과해 버리면 exception이 발생. 그것을 캐치해서 사용하면 됨.
                id = BitConverter.ToInt32(_s.Slice(_count, _s.Length - _count));
                _count += sizeof(int);
                level = BitConverter.ToInt16(_s.Slice(_count, _s.Length - _count));
                _count += sizeof(ushort);
                duration = BitConverter.ToSingle(_s.Slice(_count, _s.Length - _count)); // ToSingle = float 전용
                _count += sizeof(float);
            }
        }
        public List<SkillInfo> listSkils = new List<SkillInfo>();

        public PlayerInfoReq()
        {
            this.packetId = (ushort)PacktID.PlayerInfoReq;
        }

        public override void Read(ArraySegment<byte> _segment)
        {
            // 지금까지 몇 byte를 넣었는지
            ushort count = 0;
            ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(_segment.Array, _segment.Offset, _segment.Count);

            count += sizeof(ushort);            
            count += sizeof(ushort);

            // span은 범위를 지정해서 찝어주는 형식
            // 범위를 찝어주면서 확실히 몇 byte인지 지정한거라
            // 범위를 초과해 버리면 exception이 발생. 그것을 캐치해서 사용하면 됨.
            this.playerId = BitConverter.ToInt64(s.Slice(count, s.Length - count));
            count += sizeof(long);

            // string에 넣어야 되는 부분.
            ushort nameLen = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
            count += sizeof(ushort);

            // 누군가가 패킷을 조작할 수 있기 때문에 만약의 사테를 두고 작업해야함
            this.name = Encoding.Unicode.GetString(s.Slice(count, nameLen));
            count += nameLen;


            // skill slit
            listSkils.Clear(); // 안전하게 이전꺼 clear
            ushort skillLen = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
            count += sizeof(ushort);

            for (int i = 0; i < skillLen; i++)
            {
                SkillInfo skill = new SkillInfo();
                skill.Read(s, ref count);
                listSkils.Add(skill);
            }
        }


        // 우리가 컨트롤하기 때문에 아무 문제 없다.
        public override ArraySegment<byte> Write()
        { 
            // 할당 받은 buffer 열어서 원하는 사이즈를 확보하고
            ArraySegment<byte> segement = SendBufferHelper.Open(4096); //openSegment
            bool success = true;
            // 지금까지 몇 byte를 넣었는지
            ushort count = 0; // ushort로 해야 size가 맞다


            Span<byte> s = new Span<byte>(segement.Array, segement.Offset, segement.Count);


            // [] [] [] [] [] [] [] []  <- 2byte를 넣어주면 그 다음으로 buffer가 줄어야한다.

            // slice는 span 자체가 변하는게 아니라 결과값이 span의 byte로 뽑혀오기 때문.
            // span 에서 slice로 바꾼 이유는 span이라는 타입으로 반환하기 때문에 그것을 사용. 코드가 깔끔
            count += sizeof(ushort); // size(ushort = 2byte) 만큼
            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.packetId);
            count += sizeof(ushort);  // packetId(ushort = 2btye) 만큼 
            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.playerId);
            count += sizeof(long); // playerId(ulong = 8byte) 만큼


            //// string에 넣어야 되는 부분.
            //// string = UTF-16이 C# 기본이기 때문에 굳이 UTF-16으로 변환 안해도 됨
            //// string length[2] <- byte []배열
            //ushort nameLen = (ushort)Encoding.Unicode.GetByteCount(this.name);
            //success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count),nameLen);
            //count += sizeof(ushort);
            //Encoding.Unicode.GetBytes(this.name);
            //                    // 여기서 시작해서           // 이쪽으로 복사할건데, 크기는 nameLen이다/
            //Array.Copy(Encoding.Unicode.GetBytes(this.name), 0, segement.Array, count, nameLen);
            //count += nameLen;

            // 위의 주석과 같은 내용이지만 더 효율적이게 만들어진 코드.
            // name 건네주고 0부터 시작해서, name을 전체 복사할건데, segement 배열에다가 segement.Offset + count 만큼 + 추가 ushort만큼 공간을 마련해야함(name Length).
            ushort nameLen = (ushort)Encoding.Unicode.GetBytes(this.name, 0, this.name.Length, segement.Array, segement.Offset + count + sizeof(ushort));
            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), nameLen);
            count += sizeof(ushort);
            count += nameLen;

            // skill list
            success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)listSkils.Count); // count는 4byte니까 2byte로 변경
            count += sizeof(ushort); // (ushort)listSkils.Count // count는 4byte니까 2byte로 변경

            foreach (var skill in listSkils)
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

            PlayerInfoReq packet = new PlayerInfoReq() { playerId = 1001, name = "ABCD" };
            packet.listSkils.Add(new PlayerInfoReq.SkillInfo() { id = 101, level =1, duration = 3f});
            packet.listSkils.Add(new PlayerInfoReq.SkillInfo() { id = 102, level =2, duration = 4f});
            packet.listSkils.Add(new PlayerInfoReq.SkillInfo() { id = 103, level =3, duration = 5f});
            packet.listSkils.Add(new PlayerInfoReq.SkillInfo() { id = 104, level =4, duration = 6f});

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
