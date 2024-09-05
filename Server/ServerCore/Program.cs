namespace ServerCore
{
    internal class Program
    {
        volatile static bool _stop = false;
        static void ThreadMain()
        {
            Console.WriteLine("쓰레드 시작");
            if (_stop == false)
            {
                while (true)
                {
                    // 누군가가 stop 신호를 주기를 기다린다.
                }
            }
            while (_stop)
            {
                // 누군가가 stop 신호를 주기를 기다린다.
            }
            Console.WriteLine("쓰레드 종료");
        }

        static void Main(string[] args)
        {
            Task t = new Task(ThreadMain);
            t.Start();

            Thread.Sleep(1000); // 밀리세컨으로 1초동안 대기

            _stop = true;       // while 종료

            Console.WriteLine("Stop 호출");
            Console.WriteLine("종료 대기중");
            t.Wait(); // 쓰레드의 Join과 같음. 끝날때 까지 기다린다

            Console.WriteLine("Stop 성공");
        }
    }
}
