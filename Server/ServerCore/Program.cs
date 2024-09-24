namespace ServerCore
{
    // 존버메타. 잠금이 풀릴 때 까지 뺑뺑이 도는 것.
    class SpinLock 
    {
        // 이게 true면 다른곳에서 사용중이다.
        volatile int _locked = 0; // false = 0

        public void Acquire()
        {
            while (true)
            {
                //// 원본을 반환
                //// Exchange는 _locked를 1로 변경한다(대입한다)
                ////_locked는 공유해서 사용되기 때문에 멋대로 읽어서 사용하면 안된다.
                //// 뱉어준 값을 사용할 수 있는 이유는 stack에서 사용되는 값이기 때문이다.
                //int origianl = Interlocked.Exchange(ref _locked, 1);

                //// 변경되기 전의 값이 1이면 다른곳에서 사용중.
                //// 현재 값이 1이고 이전 값이 0이면 변경이 없었다는 뜻.
                //if (origianl == 0)
                //    break;

                // 첫 인자랑 마지막 인자를 비교해서 같다고 한다면, 두번째 값을 반환한다
                // CAS Compare-And-Swap
                int expected = 0; // 예상한 값
                int desired = 1;  // 원하는 값

                if (Interlocked.CompareExchange(ref _locked, desired, expected) == expected)
                    break;
            }
        }

        public void Release()
        {
            _locked = 0;
        }

    }
    class program
    {
        static int _num = 0;
        static SpinLock _lock = new SpinLock();

        static void Thread_1()
        {
            for (int i = 0; i < 100000; i++)
            {
                _lock.Acquire();
                _num++;
                _lock.Release();
            }
        }

        static void Thread_2()
        {
            for (int i = 0; i < 100000; i++)
            {
                _lock.Acquire();
                _num--;
                _lock.Release();
            }

        }

        static void Main(string[] args)
        {
            Task t1 = new Task(Thread_1);
            Task t2 = new Task(Thread_2);

            t1.Start();
            t2.Start();

            Task.WaitAll(t1, t2);
            Console.WriteLine(_num);

        }
    }
}
