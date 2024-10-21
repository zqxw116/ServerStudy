using System;
using System.Collections.Generic;
using System.Text;

namespace ServerCore
{
    public class RecvBuffer
    {
        // ex) 10개로 [ ] [ ] [ ] [ ] [ ] [ ] [ ] [ ] [ ] [ ]
        ArraySegment<byte> buffer;

        // 마우스 커서 의 느낌
        int readPos;  // 마우스 커서 위치
        int writePos; // 커서에서 입력된 위치

        public RecvBuffer(int _bufferSize)
        {
            buffer = new ArraySegment<byte>(new byte[_bufferSize], 0, _bufferSize);
        }

        // 버퍼에 들어간, 처리되지 않은 데이터의 크기
        public int DataSize { get { return  writePos - readPos; } } // 쓰여진 위치와 읽는 부분의 위치를 뺀 값
        
        // 버퍼의 남은 공간
        public int FreeSize { get { return buffer.Count - writePos; } }// 총 크기 - 입력된 위치

        // 유효범위의 구획(Segment)
        // 어디부터 데이터를 읽으면 되는지 컨텐트에 전달
        public ArraySegment<byte> ReadSegment
        {
                                     // buffer.Array : 시작 위치, offset 시작위치, 데이터 크기
            get { return new ArraySegment<byte>(buffer.Array, buffer.Offset + readPos, DataSize); }
        }

        // 다음 recive 할 때 유효범위
        public ArraySegment<byte> WriteSegment
        {
                                        // buffer.Array : 시작 위치, offset 시작위치, 남은 데이터 크기
            get { return new ArraySegment<byte>(buffer.Array, buffer.Offset + writePos, FreeSize); }
        }

        /// <summary>
        /// 중간중간 앞부분 정리. r과 w를 처음으로 당겨준다.
        /// <summary>
        public void Clean()
        {
            int dataSize = DataSize;
            if (dataSize == 0)
            {
                // 남은 데이터가 없으면 복사하지 않고 커서 위치만 ㅣ셋
                readPos = writePos = 0;
            }
            else
            {
                // 남은 찌꺼기가 남아 있다면 시작위치로 복사
                   // source는 array에서 시작,  읽어야 될 부분, 처음으로 돌려 보내고, 목적지는 offset이고, dataSize만큼 복사 해서 시작위치로 돌려보낸다,
                Array.Copy(buffer.Array, buffer.Offset + readPos, buffer.Array, buffer.Offset, dataSize);
                readPos = 0;
                writePos = dataSize;
            }
        }

        /// <summary>
        /// 컨텐츠 코드에서 실제 데이터를 가공해서 처리할 때 성공적으로 처리되면 호출
        /// </summary>
        public bool OnRead(int _numOfByte)
        {
            // 처리한 크기가 데이터 크기보다 크면 에러다
            if (_numOfByte > DataSize)
                return false;
            readPos += _numOfByte;
            return true;
        }

        public bool OnWrite(int _numOfByte)
        {
            // 처리한 크기가 데이터 크기보다 크면 에러다
            if (_numOfByte > FreeSize)
                return false;
            writePos += _numOfByte;
            return true;

        }
    }
}
