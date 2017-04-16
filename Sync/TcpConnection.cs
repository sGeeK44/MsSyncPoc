using System.Diagnostics;
using System.IO;
using System.Net.Sockets;

// ReSharper disable UseStringInterpolation
// ReSharper disable UseNullPropagation
// ReSharper disable ConvertPropertyToExpressionBody

namespace Sync
{
    public abstract class TcpConnection
    {
        private const int Ko32 = 1024 * 32;
        protected Socket Sock { get; set; }

        public abstract void Start();

        public virtual void Stop()
        {
            Sock.Shutdown(SocketShutdown.Both);
            Sock.Close();
        }

        public object Receive()
        {
            Debug.WriteLine(string.Format("Received object."));
            var buffer = new byte[Ko32];
            var receiveBytes = Sock.Receive(buffer);
            if (receiveBytes <= 0)
                return null;

            using (var content = new MemoryStream())
            {
                content.Write(buffer, 0, receiveBytes);
                while (Sock.Available > 0)
                {
                    receiveBytes = Sock.Receive(buffer);
                    content.Write(buffer, 0, receiveBytes);
                }
                return Utility.DeserializeFromStream(content);
            }
        }

        public void Send(object objToSend)
        {
            Debug.WriteLine("Sending object over tcp...");
            int sended = Sock.Send(Utility.SerializeToStream(objToSend).ToArray(), SocketFlags.None);
            Debug.WriteLine(string.Format("Send success for {0} bytes.", sended));
        }
    }
}