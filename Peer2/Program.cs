using System;
using System.Threading;
using Sync;

namespace Peer2
{
    class Program
    {
        //Return the server connection string. 
        public const string ConnStrDbClientSync = @"Data Source='SyncSampleClient.sdf'";
        public const string ScopeName = "ProductsScope";

        static void Main(string[] args)
        {
            Console.WriteLine("Peer2 start.");
            Utility.CreateDatabase(ConnStrDbClientSync);
            Utility.ProvisionDatabase(ConnStrDbClientSync, ScopeName);
            var orchestrator = new TcpPeerSyncOrchestrator(ConnStrDbClientSync, ScopeName, 1665);
            Console.WriteLine("Press to Sync.");
            Console.ReadKey();
            orchestrator.Sync();
            Console.WriteLine("Press to Listen.");
            Console.ReadKey();
            orchestrator.StartListner();
            Console.WriteLine(String.Empty);
            Console.WriteLine("End...");
            Console.ReadKey();
        }
    }
}
