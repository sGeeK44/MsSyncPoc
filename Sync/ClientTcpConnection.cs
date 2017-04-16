using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

// ReSharper disable UseStringInterpolation
// ReSharper disable UseNullPropagation
// ReSharper disable ConvertPropertyToExpressionBody

namespace Sync
{
    public class ClientTcpConnection : TcpConnection
    {
        private readonly string _host;
        private readonly int _port;

        public ClientTcpConnection(string host, int port)
        {
            _host = host;
            _port = port;
        }

        public override void Start()
        {
            //IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
            //IPAddress ipAddress = ipHostInfo.AddressList[0];
            //IPEndPoint remoteEP = new IPEndPoint(ipAddress,11000);
            
            var ip = IPAddress.Parse(_host);
            var ipEnd = new IPEndPoint(ip, _port);
            Sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            Console.WriteLine(string.Format("Connecting on server at:{0}.", ip));
            Sock.Connect(ipEnd);
        }
    }
}