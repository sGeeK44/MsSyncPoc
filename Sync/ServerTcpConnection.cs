using System;
using System.Net;
using System.Net.Sockets;

namespace Sync
{
    public class ServerTcpConnection : TcpConnection
    {
        private Socket _listner;
        private readonly int _port;
        public ServerTcpConnection(int port)
        {
            _port = port;
        }

        public override void Start()
        {
            var ipEnd = new IPEndPoint(IPAddress.Any, _port);
            _listner = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            _listner.Bind(ipEnd);
            _listner.Listen(10);
            Console.WriteLine("Waiting client...");
            Sock = _listner.Accept();
            Console.WriteLine(string.Format("Client:{0} connected!", Sock.RemoteEndPoint));
        }

        public override void Stop()
        {
            _listner.Close();
            base.Stop();
        }
    }
}