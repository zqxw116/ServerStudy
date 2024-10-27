using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace ServerCore
{
    public class SendBufferHelper
    {
        // 쓰레드 끼리의 경합을 없애기 위해서 ThreadLocal사용
        public static ThreadLocal<SendBuffer> CurrentBuffer = new ThreadLocal<SendBuffer>(() => { return null; });
        
        // 쓰레드마다 chunk를 잘라서 사용한다
        public static int ChunkSize { get; set; } = 4096; // Chunk = 뭉태기 느낌. 엄청 큰 느낌

        public static ArraySegment<byte> Open(int _reserveSize)
        {
            if (CurrentBuffer.Value == null)
                CurrentBuffer.Value = new SendBuffer(ChunkSize);

            if (CurrentBuffer.Value.FreeSize <_reserveSize)
                CurrentBuffer.Value = new SendBuffer(ChunkSize);

            return CurrentBuffer.Value.Open(_reserveSize);
        }

        public static ArraySegment<byte> Close(int _usedSize)
        {
            return CurrentBuffer.Value.Close(_usedSize);
        }
    }

    // 일회용으로만 사용할 예정
    public class SendBuffer
    {
        // [(usedSize)] [] [] [] [] [] [] [] [] [] 10byte
        byte[] buffer;
        int usedSize = 0;

        public int FreeSize { get { return buffer.Length - usedSize; }} // 전체 사이즈 - 커서 위치
               
        public SendBuffer(int _chunkSize)
        {
            buffer = new byte[_chunkSize];
        }
        public ArraySegment<byte> Open(int _reserveSize) // _reserveSize : 예상한 사이즈크기
        {

            if (_reserveSize == FreeSize)
                return null;

            return new ArraySegment<byte>(buffer, usedSize, _reserveSize);
        }
        
        public ArraySegment<byte> Close(int _usedSize)
        {
                                                            // buffer에서 시작, usedSize에서 시작한 다음, 실제 사용되는 크기
            ArraySegment<byte> segment = new ArraySegment<byte>(buffer, usedSize, usedSize);
            usedSize += usedSize; // 커서위치만큼 이동

            return segment;
        }
    }
}
