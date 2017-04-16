using System;
using System.Data.SqlServerCe;
using Microsoft.Synchronization;
using Microsoft.Synchronization.Data;
using Microsoft.Synchronization.Data.SqlServerCe;
using Sync;

namespace CustomChangeTracking
{
    class Program
    {
        //Return the client connection string with the password.
        public const string ConnStrSqlCeClientSync = @"Data Source='SyncSampleClient.sdf'";

        //Return the server connection string. 
        public const string ConnStrDbServerSync = @"Data Source='SyncSampleServer.sdf'";
        public const string ScopeName = "ProductsScope";

        //Delete the client database.
        public static void RecreateCompactDatabase()
        {
            Utility.CreateDatabase(ConnStrSqlCeClientSync);
            Utility.CreateDatabase(ConnStrDbServerSync);
        }

        private static void Main()
        {
            LocalSyncUncouplingSerialize();
        }

        public static void LocalSyncUncouplingSerialize()
        {
            RecreateCompactDatabase();
            ProvisionDb();

            // create a connection to the SyncCompactDB database
            var clientProvider = CreateProvider(ConnStrSqlCeClientSync);

            // subscribe for errors that occur when applying changes to the client
            clientProvider.ApplyChangeFailed += Program_ApplyChangeFailed;

            // create a connection to the SyncDB server database
            var serverProvider = CreateProvider(ConnStrDbServerSync);
            
            Utility.MakeDataChanges(ConnStrSqlCeClientSync);
            
            // execute the synchronization process
            SyncLocalWithSerialize(clientProvider, serverProvider);
            Console.WriteLine(String.Empty);
            Console.WriteLine("End...");
            Console.ReadKey();
        }

        private static void SyncLocalWithSerialize(SqlCeSyncProvider clientProvider, SqlCeSyncProvider serverProvider)
        {
            SyncLocalWithSerializeOnWay(serverProvider, clientProvider);
            SyncLocalWithSerializeOnWay(clientProvider, serverProvider);
        }

        private static void SyncLocalWithSerializeOnWay(SqlCeSyncProvider clientProvider, SqlCeSyncProvider serverProvider)
        {
            uint batchSize;
            SyncKnowledge knowledge;
            object changedData;
            var clientSession = new SyncSessionContext(clientProvider.IdFormats, new SyncCallbacks());
            var serverSession = new SyncSessionContext(serverProvider.IdFormats, new SyncCallbacks());
            clientProvider.BeginSession(SyncProviderPosition.Local, clientSession);
            serverProvider.BeginSession(SyncProviderPosition.Remote, serverSession);

            serverProvider.GetSyncBatchParameters(out batchSize, out knowledge);
            var clientBatchSize = (uint)Transfert(batchSize);
            var clientknowledge = (SyncKnowledge)Transfert(knowledge);
            var changeBatch = clientProvider.GetChangeBatch(clientBatchSize, clientknowledge, out changedData);

            var serverchangeBatch = (ChangeBatch)Transfert(changeBatch);
            var serverChangedData = Transfert(changedData);
            var syncStats = new SyncSessionStatistics();
            serverProvider.ProcessChangeBatch(ConflictResolutionPolicy.DestinationWins, serverchangeBatch, serverChangedData, new SyncCallbacks(), syncStats);
            serverProvider.EndSession(serverSession);
            clientProvider.EndSession(clientSession);

            // print statistics
            Console.WriteLine("ChangesApplied: " + syncStats.ChangesApplied);
            Console.WriteLine("ChangesFailed: " + syncStats.ChangesFailed);
        }

        private static object Transfert(object o)
        {
            var mem = Utility.SerializeToStream(o);
            return Utility.DeserializeFromStream(mem);
        }

        public static void LocalSyncUncoupling()
        {
            RecreateCompactDatabase();
            ProvisionDb();

            // create a connection to the SyncCompactDB database
            var clientProvider = CreateProvider(ConnStrSqlCeClientSync);

            // subscribe for errors that occur when applying changes to the client
            clientProvider.ApplyChangeFailed += Program_ApplyChangeFailed;

            // create a connection to the SyncDB server database
            var serverProvider = CreateProvider(ConnStrDbServerSync);

            Utility.MakeDataChanges(ConnStrSqlCeClientSync);

            // execute the synchronization process
            SyncLocal(clientProvider, serverProvider);
            Console.WriteLine(String.Empty);
            Console.WriteLine("End...");
            Console.ReadKey();
        }

        private static SqlCeSyncProvider CreateProvider(string db)
        {
            return Utility.CreateProvider(db, ScopeName);
        }

        private static void SyncLocal(SqlCeSyncProvider clientProvider, SqlCeSyncProvider serverProvider)
        {
            SyncLocalOnWay(serverProvider, clientProvider);
            SyncLocalOnWay(clientProvider, serverProvider);
        }

        private static void SyncLocalOnWay(SqlCeSyncProvider clientProvider, SqlCeSyncProvider serverProvider)
        {
            uint batchSize;
            SyncKnowledge knowledge;
            object changedData;
            var clientSession = new SyncSessionContext(clientProvider.IdFormats, new SyncCallbacks());
            var serverSession = new SyncSessionContext(serverProvider.IdFormats, new SyncCallbacks());
            clientProvider.BeginSession(SyncProviderPosition.Local, clientSession);
            serverProvider.BeginSession(SyncProviderPosition.Remote, serverSession);
            serverProvider.GetSyncBatchParameters(out batchSize, out knowledge);
            var changeBatch = clientProvider.GetChangeBatch(batchSize, knowledge, out changedData);

            var syncStats = new SyncSessionStatistics();
            serverProvider.ProcessChangeBatch(ConflictResolutionPolicy.DestinationWins, changeBatch, changedData,
                new SyncCallbacks(), syncStats);
            serverProvider.EndSession(serverSession);
            clientProvider.EndSession(clientSession);

            // print statistics
            Console.WriteLine("ChangesApplied: " + syncStats.ChangesApplied);
            Console.WriteLine("ChangesFailed: " + syncStats.ChangesFailed);
        }

        public static void LocalSync()
        {
            RecreateCompactDatabase();
            ProvisionDb();

            // create a connection to the SyncCompactDB database
            var clientConn = new SqlCeConnection(ConnStrSqlCeClientSync);

            // create a connection to the SyncDB server database
            var serverConn = new SqlCeConnection(ConnStrDbServerSync);

            // create the sync orhcestrator
            var syncOrchestrator = new SyncOrchestrator();

            // set local provider of orchestrator to a CE sync provider associated with the 
            // ProductsScope in the SyncCompactDB compact client database
            var clientProvider = new SqlCeSyncProvider("ProductsScope", clientConn);
            // subscribe for errors that occur when applying changes to the client
            clientProvider.ApplyChangeFailed += Program_ApplyChangeFailed;
            syncOrchestrator.LocalProvider = clientProvider;

            // set the remote provider of orchestrator to a server sync provider associated with
            // the ProductsScope in the SyncDB server database
            var serverProvider = new SqlCeSyncProvider("ProductsScope", serverConn);
            syncOrchestrator.RemoteProvider = serverProvider;

            // set the direction of sync session to Upload and Download
            syncOrchestrator.Direction = SyncDirectionOrder.UploadAndDownload;


            Utility.MakeDataChanges(ConnStrSqlCeClientSync);

            // execute the synchronization process
            var syncStats = syncOrchestrator.Synchronize();

            // print statistics
            Console.WriteLine("Start Time: " + syncStats.SyncStartTime);
            Console.WriteLine("Total Changes Uploaded: " + syncStats.UploadChangesTotal);
            Console.WriteLine("Total Changes Downloaded: " + syncStats.DownloadChangesTotal);
            Console.WriteLine("Complete Time: " + syncStats.SyncEndTime);
            Console.WriteLine(String.Empty);
            Console.ReadKey();
        }

        private static void ProvisionDb()
        {
            var scopeDesc = ProvisionMainDb();
            ProvisionningClient(scopeDesc);
        }

        private static DbSyncScopeDescription ProvisionMainDb()
        {
            return Utility.ProvisionDatabase(ConnStrDbServerSync, ScopeName);
        }

        private static void ProvisionningClient(DbSyncScopeDescription scopeDesc)
        {
            Utility.ProvisionDatabase(ConnStrSqlCeClientSync, ScopeName);
            //Utility.ProvisionningClient(ConnStrSqlCeClientSync, ScopeName, scopeDesc);
        }

        static void Program_ApplyChangeFailed(object sender, DbApplyChangeFailedEventArgs e)
        {
            // display conflict type
            Console.WriteLine(e.Conflict.Type);

            // display error message 
            Console.WriteLine(e.Error);
        }
    }
}