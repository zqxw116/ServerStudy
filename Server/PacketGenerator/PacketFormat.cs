using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PacketGenerator
{
    class PacketFormat
    {
        // {0} 패킷 이름
        // {1} 멤버 변수들
        // {2} 멤버 변수 Read
        // {3} 멤버 변수 Write
        public static string packetFormat =
@"
class {0} 
{{
    {1}

    public void Read(ArraySegment<byte> segment)
    {{
        // 지금까지 몇 byte를 넣었는지
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);            
        count += sizeof(ushort);

        {2}
    }}


    // 우리가 컨트롤하기 때문에 아무 문제 없다.
    public ArraySegment<byte> Write()
    {{
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
        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)PacktID.{0});
        count += sizeof(ushort);  // packetId(ushort = 2btye) 만큼 
        
        {3}
        
        // 반드시 size는 마지막에 시도해야한다. 왜냐면 byte를 다 더해준 것을 마지막에 알기 때문이다
        success &= BitConverter.TryWriteBytes(s, count);
        if (success == false) 
            return null;

        // buffer 닫는다.
        return SendBufferHelper.Close(count);

    }}
}}

";
        // {0} 변수 형식
        // {1} 변수 이름
        public static string memberFormat =
@"public {0} {1};";


        // {0} 리스트 이름 [대문자] struct의 이름
        // {1} 리스트 이름 [소문자]
        // {2} 멤버 변수들
        // {3} 멤버 변수 Read
        // {4} 멤버 변수 Write
        public static string memberListFormat =
@"
public struct {0}
{{
    {2}

    public void Read(ReadOnlySpan<byte> s, ref ushort count)
    {{
        // span은 범위를 지정해서 찝어주는 형식
        // 범위를 찝어주면서 확실히 몇 byte인지 지정한거라
        // 범위를 초과해 버리면 exception이 발생. 그것을 캐치해서 사용하면 됨.
        {3}
    }}

    //              전체 byte 배열, 실시간 몇 번째 count 작업중인지
    public bool Write(Span<byte> s, ref ushort count)
    {{
        bool success = true;
        {4}
        return success;
    }}

}}
public List<{0}> {1}s = new List<{0}>();
";


        // {0} 변수 이름
        // {1} To~변수 형식
        // {2} 변수 형식
        public static string readForamt =
@"this.{0} = BitConverter.{1}(s.Slice(count, s.Length - count));
count += sizeof({2});";

        // {0} 변수 이름
        public static string readStringFormat =
@"ushort {0}Len = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
count += sizeof(ushort);

// 누군가가 패킷을 조작할 수 있기 때문에 만약의 사테를 두고 작업해야함
this.{0} = Encoding.Unicode.GetString(s.Slice(count, nameLen));
count += {0}Len;";

        // {0} 리스트 이름 [대문자]
        // {1} 리스트 이름 [소문자]
        public static string readListFormat =
@"// skill slit
this.{1}s.Clear(); // 안전하게 이전꺼 clear
ushort {1}Len = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
count += sizeof(ushort);

for (int i = 0; i < {1}Len; i++)
{{
    {0} {1} = new {0}();
    {1}.Read(s, ref count);
    {1}s.Add({1});
}}";

        // {0} 변수 이름
        // {1} 변수 형식
        public static string writeFormat =
@"success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.{0});
count += sizeof({1}); // playerId(byte) 만큼";

        // {0} 변수 이름
        public static string writeStringFormat =
@"// name 건네주고 0부터 시작해서, name을 전체 복사할건데, segement 배열에다가 segement.Offset + count 만큼 + 추가 ushort만큼 공간을 마련해야함(name Length).
ushort {0}Len = (ushort)Encoding.Unicode.GetBytes(this.{0}, 0, this.{0}.Length, segement.Array, segement.Offset + count + sizeof(ushort));
success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), {0}Len);
count += sizeof(ushort);
count += {0}Len;";


        // {0} 리스트 이름 [대문자]
        // {1} 리스트 이름 [소문자]
        public static string writeListFormat =
@"// skill list
success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)this.{1}s.Count); // count는 4byte니까 2byte로 변경
count += sizeof(ushort); // (ushort)listSkils.Count // count는 4byte니까 2byte로 변경

foreach ({0} {1} in this.{1}s)
{{
    // span으로 찝어둔 영역에 하나씩 넣을 것이다.
    // Write 안에서 count가 늘어난다
    success &= {1}.Write(s, ref count); 
}}
";
    }
}
