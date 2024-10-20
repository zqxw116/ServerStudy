using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServerCore
{
    public abstract class Session
    {
        Socket socket;
        int disconnected = 0;//커넥션 확인 하는 플레그


        object _lock = new object();
        Queue<byte[]> sendQueue = new Queue<byte[]>();
        List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>(); // 대기중인 목록
        SocketAsyncEventArgs sendArgs = new SocketAsyncEventArgs();
        SocketAsyncEventArgs recvArgs = new SocketAsyncEventArgs();

        // 클라이언트가 처음 접속했을 때
        public abstract void OnConnected(EndPoint _endPoint);
        public abstract void OnRecv(ArraySegment<byte> _buffer);
        public abstract void OnSend(int _numOfBytes);
        public abstract void OnDisConnected(EndPoint _endPoint);


        public void Start(Socket _clientSocket)
        {
            socket = _clientSocket;
            //데이터 수신 완료 후 실행할 메소드 설정.
            recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);
            recvArgs.SetBuffer(new byte[1024], 0, 1024);//버퍼 사이즈 설정.

            //데이터 송신 완료 후 실행할 메소드 설정.
            sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);

            RegisterRecv();
        }

        public void Send(byte[] _sendBuff)
        {
            /* SocketAsyncEventArgs 에서 실행되는 Thread와 Send를 호출하는 컨턴츠단 Thread가 동시에 같은 자원에 접근시
             * 문제가 발생하여 동기화 lock 적용
             * 데이터를 바로 전송할 수 있는 상태가 아닐경우 que에 넣고 넘김.
             */
            lock (_lock)
            {
                sendQueue.Enqueue(_sendBuff);
                // 대기중인게 1개도 없으면
                if (_pendingList.Count == 0)
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


            OnDisConnected(socket.RemoteEndPoint);
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }

        #region 네트워크 통신

        void RegisterSend()
        {
            //_sendArgs.BufferList : 데이터 전송을 한건씩 하는게 아닌 보낼 데이터를 묶어서 한번에 보냄.
            
            while (sendQueue.Count > 0)
            {
                byte[] buff = sendQueue.Dequeue();
                // ArraySegment = 어떤 배열의 일부 라는 구조체 // 배열, 시작 인덱스, 배열크기
                _pendingList.Add(new ArraySegment<byte>(buff, 0, buff.Length));

            }
            // 그저 BufferList 적용하는 방법은 =을 사용해서 넣어주는 것이다. Add는 안된다.
            sendArgs.BufferList = _pendingList;

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
                // BytesTransferred ='SocketAsyncEventArgs'가 읽어 들인 버퍼의 양
                if (_args.BytesTransferred > 0 && _args.SocketError == SocketError.Success)
                {
                    try
                    {
                        _pendingList.Clear();
                        sendArgs.BufferList = null;

                        OnSend(sendArgs.BytesTransferred);

                        // sendQueue를 확인해줘야 한다.
                        // 내 작업중 다른 사람이 추가했을 수 있기 때문이다
                        if (sendQueue.Count > 0)
                        {
                            RegisterSend();
                        }
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


        void RegisterRecv()
        {
            /* _socket.ReceiveAsync 데이터가 수신되면 false 반환. 없을 경우 true를 반환 하며 이후 데이터가 수신되면
             * SocketAsyncEventArgs에 의해 OnRecvCompleted() 실행.
            */
            bool pending = socket.ReceiveAsync(recvArgs);
            if (pending == false)
            {
                OnRecvCompleted(null, recvArgs);
            }
        }

        void OnRecvCompleted(object _sender, SocketAsyncEventArgs _args)
        {
            // 상대방이 연결을 끊거나 등 가끔 0이 올 수 있다.
            if (_args.BytesTransferred > 0 && _args.SocketError == SocketError.Success)
            {
                try
                {
                    OnRecv(new ArraySegment<byte>(_args.Buffer, _args.Offset, _args.BytesTransferred));                    
                    //데이터를 다시 수신할 수 있는 상태로 등록.

                    RegisterRecv();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"OnRecvCompleted Failed {e}");
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
