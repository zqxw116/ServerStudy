using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    public class Connector
    {
        Func<Session> sessionFactory;


        public void Connect(IPEndPoint _endPoint, Func<Session> _sessionFactory)
        {
            //휴대폰 설정(소켓)
            // 여러개를 받을 수 있으니까 따로 변수로 저장하지는 않는다.
            Socket socket = new Socket(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            sessionFactory = _sessionFactory;

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += OnConnectCompleted;
            args.RemoteEndPoint = _endPoint;
            args.UserToken = socket;
            RegisterConnect(args);
        }

        void RegisterConnect(SocketAsyncEventArgs _args)
        {
            Socket socket = _args.UserToken as Socket;
            if (socket == null) 
                return;

            bool pending = socket.ConnectAsync(_args);
            // ConnectAsync가 비동기적으로 처리되면, 완료된 후에만 OnConnectCompleted가 호출됩니다.

            // 바보.. 여태껏  pending == ture로 적용하고 있었어....
            if (!pending)
            {
                // 비동기 작업이 완료되지 않은 경우, 즉시 호출
                OnConnectCompleted(null, _args);

            }

        }

        void OnConnectCompleted(object _sender, SocketAsyncEventArgs _args)
        {
            if (_args.SocketError == SocketError.Success)
            {
                Session session = sessionFactory.Invoke();
                // 연결됐으면 start. 전달받은 연결된 소캣으로
                session.Start(_args.ConnectSocket);
                session.OnConnected(_args.RemoteEndPoint);
                Console.WriteLine($"[Connector] OnConnectCompleted Success : {_args.RemoteEndPoint}");
            }
            else
            {
                Console.WriteLine($"[Connector] OnConnectCompleted Fail : {_args.SocketError}");
            }

        }
    }
}
