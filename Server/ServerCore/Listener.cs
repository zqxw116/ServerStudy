using System.Net;
using System.Net.Sockets;

namespace ServerCore
{
    class Listener
    {
        Socket listenSocket;
        Action<Socket> onAcceptHandler;

        public void Init(IPEndPoint _endPoint, Action<Socket> _onacceptHandler)
        {
            // 문지기
            listenSocket = new Socket(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            onAcceptHandler += _onacceptHandler;

            // 문지기 교육
            listenSocket.Bind(_endPoint); // 포트번호 기입

            // 영업 시작
            // backlog : 최대 대기수
            listenSocket.Listen(10); // 최대 몇 명 들어올 수 있는지

            //클라 접속 되면 연결 해준 함수로 콜백 해줌(콜백 함수 등록)
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptComplated);

            //소켓 오픈
            RegisterAccept(args); 
        }

        void RegisterAccept(SocketAsyncEventArgs _args)
        {
            //Onacceptcompleted메소드에서 accept후 다시 들어오면 args에 기존 값이 남아있어 해당 부분을 초기화 시켜줌. 초기화 하지 않으면 에러 발생.
            _args.AcceptSocket = null;


            //AcceptAsync client가 접속 되면 false 반환, 접속되지 않으면 true 반환
            //(true 반환 후 client가 접속 되면 SocketAsyncEventArgs로 소캣을 콜백 해줌.
            bool pending = listenSocket.AcceptAsync(_args);
            if (!pending ) //  true면 아직 접속된 클라가 없다는 뜻.
                OnAcceptComplated(null, _args);

        }

        // 물고기가 잡혔으니까 낚시대를 끌어올렸다.
        void OnAcceptComplated(object _sender, SocketAsyncEventArgs _args)
        {        
            //소켓 에러유무 확인
            if (_args.SocketError == SocketError.Success)
            {            	
                //접속이 완료되어 호출자에게 접속 소켓 전달.
                onAcceptHandler.Invoke(_args.AcceptSocket); 
            }
            else
            {
                Console.WriteLine(_args.SocketError.ToString());
            }

            //다음 접속을 기다릴 수 있도록 AcceptAsync를 재 실행.
            RegisterAccept(_args); 
        }
    }
}
