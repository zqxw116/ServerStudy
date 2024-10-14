using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    class Session
    {
        Socket socket;
        int disconnected = 0;

        
        object _lock = new object();
        Queue<byte[]> sendQueue = new Queue<byte[]>();
        bool _pending = false;
        SocketAsyncEventArgs sendArgs = new SocketAsyncEventArgs();

        public void Start(Socket _socket)
        {
            socket = _socket;
            SocketAsyncEventArgs recvArgs = new SocketAsyncEventArgs();
            recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);
            recvArgs.SetBuffer(new byte[1024], 0, 1024); // buffer 연결해서 데이터 받을 준비.

            sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);

            RegisterRecv(recvArgs);
        }

        public void Send(byte[] _sendBuff)
        {
            lock (_lock)
            {
                sendQueue.Enqueue(_sendBuff);
                if (_pending == false)
                {
                    RegisterSend();
                }
            }
        }

        public void Disconnect()
        {
            // 동시다발적으로 호출되거나 2번 연속 호출되면 안된다
            // 멀티 Thread 코드
                                     // A를 B로 바꾸고 원래의 A를 반환
            if (Interlocked.Exchange(ref disconnected, 1) == 1)
                return;


            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }

        #region 네트워크 통신

        void RegisterSend()
        {
            // 상위에서 lock을 하고 있기 때문에 lock안해도 된다.
            _pending = true;
            byte[] buff = sendQueue.Dequeue();
            sendArgs.SetBuffer(buff, 0, buff.Length);

            bool pending = socket.SendAsync(sendArgs);
            if (pending == false) 
            {
                OnSendCompleted(null, sendArgs);
            }
        }

        void OnSendCompleted(object _sender, SocketAsyncEventArgs _args)
        {            
            // 상위에서 lock을 하고 있기 때문에 lock안해도 된다.
            // 하지만 콜백 방식으로 호출하기도 한다. 결국 = lock
            lock (_lock)
            {
                if (_args.BytesTransferred > 0 && _args.SocketError == SocketError.Success)
                {

                    try
                    {
                        // sendQueue를 확인해줘야 한다.
                        // 내 작업중 다른 사람이 추가했을 수 있기 때문이다
                        if (sendQueue.Count > 0)
                        {
                            RegisterSend();
                        }
                        else
                              _pending = false;

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"OnRecvCompleted Fao;ed {e}");
                    }
                }
                else
                {
                    Disconnect();
                }

            }
        }


        void RegisterRecv(SocketAsyncEventArgs _args)
        {
            // 비동기, 논블럭
            bool pending = socket.ReceiveAsync(_args);
            if (pending == false)
            {
                OnRecvCompleted(null, _args);
            }
        }

        void OnRecvCompleted(object _sender, SocketAsyncEventArgs _args)
        {
            // 상대방이 연결을 끊거나 등 가끔 0이 올 수 있다.
            if (_args.BytesTransferred > 0 && _args.SocketError == SocketError.Success)
            {
                try
                {
                    // TODO
                    string recvData = Encoding.UTF8.GetString(_args.Buffer, _args.Offset, _args.BytesTransferred);
                    Console.WriteLine($"[From Client] {recvData}");
                    RegisterRecv(_args);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"OnRecvCompleted Fao;ed {e}");
                }
            }
            else
            {
                // TODO
            }
        }
        #endregion
    }
}
