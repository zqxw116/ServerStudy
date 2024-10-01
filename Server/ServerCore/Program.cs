namespace ServerCore
{
    class program
    {
        // 상호배제
        // monitor
        static object _lock = new object();     // 기본적인 Lock
        static SpinLock _spinLock = new SpinLock(); // SpinLock

        // RWLock ReaderWriteLock
        static ReaderWriterLockSlim _lock3 = new ReaderWriterLockSlim();

        class Reward
        {

        }

        // 99.999999%
        static Reward GetRewardById(int id)
        {
            // 마치 LOCK이 없는것처럼 동시다발적으로 막 들어올 수 있다.
            _lock3.EnterReadLock();

            _lock3.ExitReadLock();

            return null;
        }

        // 0.000001% 1주일에 한번 호출되는 이벤트라면?
        static void AddReward(Reward reward)
        {
            _lock3.EnterWriteLock();

            _lock3.ExitWriteLock();

            lock (_lock)
            {

            }
        }


        static void Main(string[] args)
        {
            // 1번 직접 구현
            lock (_lock)
            {

            }

            bool lockTaken = false;

            // 2번 spinlock 사용
            try
            {
                _spinLock.Enter(ref lockTaken);
            }
            finally 
            { 
                if (lockTaken)
                {
                    _spinLock.Exit();
                }
            }
        }
    }
}
