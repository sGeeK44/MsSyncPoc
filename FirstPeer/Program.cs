using System;
using System.Threading;
using Sync;

namespace FirstPeer
{
    class Program
    {
        //Return the server connection string. 
        public const string ConnStrDbServerSync = @"Data Source='SyncSampleServer.sdf'";
        public const string ScopeName = "ProductsScope";

        static void Main(string[] args)
        {
            Console.WriteLine("Peer1 start.");
            Utility.CreateDatabase(ConnStrDbServerSync);
            Utility.ProvisionDatabase(ConnStrDbServerSync, ScopeName);
            Utility.MakeDataChanges(ConnStrDbServerSync);
            var orchestrator = new TcpPeerSyncOrchestrator(ConnStrDbServerSync, ScopeName, 1665);
            orchestrator.StartListner();
            Console.WriteLine("Press to Sync.");
            Console.ReadKey();
            orchestrator.Sync();
            Console.WriteLine(String.Empty);
            Console.WriteLine("End...");
            Console.ReadKey();
        }
    }
}
