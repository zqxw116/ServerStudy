using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServerCore
{
    public abstract class PacketSession : Session
    {
        public static readonly int HeaderSize = 2;
        // [size(2)][packetId(2)][....][size(2)][packetId(2)][....]
        public sealed override int OnRecv(ArraySegment<byte> _buffer)
        {
            int processLen = 0;
            
            // 패킷 처리할 수 있게 계속 체크
            while (true)
            {
                // [size(2)] 추출해서 맞는지 확인
                // 최소한 헤더는 파싱할 수 있는지 확인
                if (_buffer.Count < HeaderSize)
                    break;

                // 패킷이 완전체로 도착했는지 확인.(ushort만큼 찾아옴)
                ushort dataSize = BitConverter.ToUInt16(_buffer.Array, _buffer.Offset);
                if (_buffer.Count < dataSize)
                    break; // 패킷이 완전체가 아니라 부분적으로 옴



                // 여기까지 왔으면 패킷 조립 가능
                // 패킷이 해당하는 영역을 다시 집어서 넘겨줌. [size(2)][packetId(2)][....] 범위가 이러니까 알아서 잘 파싱해서 사용해라.
                OnRecvPacket(new ArraySegment<byte>(_buffer.Array, _buffer.Offset, dataSize));
                //ArraySegment는 struct라 힙 영역에 저장하는게 아니다.


                //[size(2)][packetId(2)][....]
                processLen += dataSize;
                // [....]이후 부분으로 옮겨줘야 한다. offset + dataSize.  처리할 부분은 전체크기 - 데이터 크기가 뒷부분 크기
                _buffer = new ArraySegment<byte>(_buffer.Array, _buffer.Offset + dataSize, _buffer.Count - dataSize);
            }
            return 0;
        }

        public abstract void OnRecvPacket(ArraySegment<byte> _buffer);

    }

    public abstract class Session
    {
        Socket socket;
        int disconnected = 0;//커넥션 확인 하는 플레그

        RecvBuffer recvBuffer = new RecvBuffer(1024);

        object _lock = new object();
        Queue<ArraySegment<byte>> sendQueue = new Queue<ArraySegment<byte>>();
        List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>(); // 대기중인 목록
        SocketAsyncEventArgs sendArgs = new SocketAsyncEventArgs();
        SocketAsyncEventArgs recvArgs = new SocketAsyncEventArgs();

        // 클라이언트가 처음 접속했을 때
        public abstract void OnConnected(EndPoint _endPoint);
        public abstract int OnRecv(ArraySegment<byte> _buffer); // 얼마만큼의 데이터를 처리했느냐를 반환.
        public abstract void OnSend(int _numOfBytes);
        public abstract void OnDisConnected(EndPoint _endPoint);


        public void Start(Socket _clientSocket)
        {
            socket = _clientSocket;
            //데이터 수신 완료 후 실행할 메소드 설정.
            recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);
            //데이터 송신 완료 후 실행할 메소드 설정.
            sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);

            RegisterRecv();


        }

        public void Send(ArraySegment<byte> _sendBuff)
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
                ArraySegment<byte> buff = sendQueue.Dequeue();
                // ArraySegment = 어떤 배열의 일부 라는 구조체 // 배열, 시작 인덱스, 배열크기
                _pendingList.Add(buff);

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
            recvBuffer.Clean();
            // 무조건 초기 설정한 버퍼로 하는게 아닌,
            // 현재 유효한 buffer인지 확인
            ArraySegment<byte> segment = recvBuffer.WriteSegment;
            recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count); // offset부터 count까지 빈 공간이라고 찝어둠


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
                    // 1. Wrtie 커서  먼저 이동
                    // BytesTransferred : 수신받은 SocketAsyncEventArgs의 byte
                    if (recvBuffer.OnWrite(_args.BytesTransferred) == false)
                    {
                        // 버그. 절대 일어날 일 없음.
                        Disconnect();
                        return;
                    }

                    // 2. 컨텐츠 쪽으로 데이터를 넘겨주고 얼마나 처리했는지 받는다
                    // 100byte를 받는다고 정해도 80byte가 오고 이후에 20 byte가 올 수 있다. 이것을 받았을 때 처리를 정해야 한다.
                    int processLen = OnRecv(recvBuffer.ReadSegment);     // 처리한 버퍼 길이   
                    if (processLen < 0 || recvBuffer.DataSize < processLen)
                    {
                        Disconnect();
                        return;
                    }
                    
                    // 3. Read 커서 이동
                    if (recvBuffer.OnRead(processLen) == false)
                    {
                        Disconnect();
                        return;
                    }


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
