using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    // 재귀적 락을 허용할지 (Yes) WriteLock -> WritecLock OK, WriteLock -> ReadLock OK, ReadLock -> WriteLock NO
    // 스핀락 정책(5000번 -> Yield)
    class Lock
    {
        const int EMPTY_FLAG = 0x00000000;
        const int WRITE_MASK = 0x7FFF0000;
        const int READ_MASK = 0x0000FFFF;
        const int MAX_SPIN_COUNT = 5000;

        // [Unused(1) [WriteThreadId(15) [ReadCount(16)]]]
        int _flag = EMPTY_FLAG;
        int _writeCount = 0;

        public void WriteLock()
        {
            // 동일 쓰레드가 WriteLock을 이미 획득하고 있는지 확인.
            int lockThreadId = (_flag & WRITE_MASK) >> 16; // Write 비트를 16비트 오른쪽으로 이동 => ReadCount 비트
            if (Thread.CurrentThread.ManagedThreadId == lockThreadId)
            {
                _writeCount++;
                return;
            }

            // threadId를 16비트 왼쪽으로 이동해서 WriteThreadId의 위치로 변경하고, WRITE_MASK와 값이 같는지 & 연산자로 비교
            int desired = (Thread.CurrentThread.ManagedThreadId << 16) & WRITE_MASK;
            while (true)
            {
                for (int i = 0; i < MAX_SPIN_COUNT; i++)
                {
                    // 동시다발적으로 접근시, 2개가 존재하지 않게 해준다.
                    if (Interlocked.CompareExchange(ref _flag, desired, EMPTY_FLAG) == EMPTY_FLAG)
                    {
                        _writeCount = 1;
                        return;
                    }
                }

                Thread.Yield();
            }
        }

        public void WriteUnLock()
        {
            int lockCount = --_writeCount;
            if (lockCount == 0)
                Interlocked.Exchange(ref _flag, EMPTY_FLAG); // 초기상태로 변경
        }

        public void ReadLock()
        {
            // 동일 쓰레드가 WriteLock을 이미 획득하고 있는지 확인
            int lockThreadId = (_flag & WRITE_MASK) >> 16;
            if (Thread.CurrentThread.ManagedThreadId == lockThreadId)
            {
                Interlocked.Increment(ref _flag);
                return;
            }


            // 아무도 WriteThreadId 획득하지 않으면, ReadCount를 1 늘린다.
            while (true)
            {
                for(int i = 0; i < MAX_SPIN_COUNT; i++)
                {
                    // 첫번째 실패조건
                    // WriteLock을 사용중이면, WriteThreadId부분이 0이 아니게 되고,
                    // 내가 예상한 부분과 맞지 않게 될테니까 무조건 실패하게 된다
                    // 두번째 실패조건
                    // 동시다발적으로 접근할 때,내가 예상한 값의 1을 더했기에(expected + 1) 값이 달라서 실패하게 된다
                    int expected = (_flag & READ_MASK); // ReadCount의 부분만 가져온다
                                                        // 내가 예상한 부분은 Write의 값은 제외한 것.

                                                     // 원한 값은 1 더해줘라, 예상한 값
                    if (Interlocked.CompareExchange(ref _flag, expected + 1, expected) == expected)
                        return;
                    
                        
                }

                Thread.Yield();

            }
        }

        public void ReadUnLock()
        {
            Interlocked.Decrement(ref _flag);   // 그냥 1을 줄여주면 된다
        }
    }

}
