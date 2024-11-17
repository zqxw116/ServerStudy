
class PlayerInfoReq 
{
    public long playerId;
	public string name;
	
	public struct Skill
	{
	    public int id;
		public short level;
		public float duration;
	
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
        ArraySegment<byte> segement = SendBufferHelper.Open(4096); //openSegment
        bool success = true;
        // 지금까지 몇 byte를 넣었는지
        ushort count = 0; // ushort로 해야 size가 맞다


        Span<byte> s = new Span<byte>(segement.Array, segement.Offset, segement.Count);


        // [] [] [] [] [] [] [] []  <- 2byte를 넣어주면 그 다음으로 buffer가 줄어야한다.

        // slice는 span 자체가 변하는게 아니라 결과값이 span의 byte로 뽑혀오기 때문.
        // span 에서 slice로 바꾼 이유는 span이라는 타입으로 반환하기 때문에 그것을 사용. 코드가 깔끔
        count += sizeof(ushort); // size(ushort = 2byte) 만큼
        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)PacktID.PlayerInfoReq);
        count += sizeof(ushort);  // packetId(ushort = 2btye) 만큼 
        
        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.playerId);
		count += sizeof(long); // playerId(byte) 만큼
		// name 건네주고 0부터 시작해서, name을 전체 복사할건데, segement 배열에다가 segement.Offset + count 만큼 + 추가 ushort만큼 공간을 마련해야함(name Length).
		ushort nameLen = (ushort)Encoding.Unicode.GetBytes(this.name, 0, this.name.Length, segement.Array, segement.Offset + count + sizeof(ushort));
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

