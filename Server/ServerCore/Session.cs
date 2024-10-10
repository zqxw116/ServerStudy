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

        public void Start(Socket _socket)
        {
            socket = _socket;
            SocketAsyncEventArgs recvArgs = new SocketAsyncEventArgs();
            recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);
            recvArgs.SetBuffer(new byte[1024], 0, 1024); // buffer 연결해서 데이터 받을 준비.

            RegisterRecv(recvArgs);
        }

        public void Send(byte[] _sendBuff)
        {
            socket.Send(_sendBuff);
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
