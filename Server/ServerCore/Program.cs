namespace ServerCore
{
    class program
    {
        static ThreadLocal<string> ThreadName = new ThreadLocal<string>(() => 
        { 
            return $"My Name is {Thread.CurrentThread.ManagedThreadId}"; 
        });
        // 특정 Thread에서 Thread name을 수정해도 다른 곳에는 영향을 주지 않는다.

        static void WhAmI()
        {
            bool repeat = ThreadName.IsValueCreated;
            if (repeat)
                Console.WriteLine(ThreadName.Value + "(repeat)");
            else
                Console.WriteLine(ThreadName.Value);



        }
        static void Main(string[] args)
        {
            ThreadPool.SetMinThreads(1, 1);
            ThreadPool.SetMaxThreads(3, 3);
            // Parallel 여기다 넣어주는 action만큼 task로 만들어서 실행해준다
            Parallel.Invoke(WhAmI, WhAmI, WhAmI, WhAmI, WhAmI, WhAmI, WhAmI, WhAmI, WhAmI);

            ThreadName.Dispose();
        }
    }
}
