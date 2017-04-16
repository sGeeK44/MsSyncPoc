using System;
using System.Diagnostics;
using Microsoft.Synchronization;
using Microsoft.Synchronization.Data;
using Microsoft.Synchronization.Data.SqlServerCe;

namespace Sync
{
    public class TcpPeerSyncOrchestrator
    {
        private readonly int _port;
        private readonly SqlCeSyncProvider _provider;
        private TcpConnection _connection;
        private SyncSessionContext _clientSession;
        private SyncSessionContext _serverSession;

        public TcpPeerSyncOrchestrator(string localDbPath, string scopeName, int port)
        {
            _port = port;

            // create a connection to the SyncCompactDB database
            _provider = Utility.CreateProvider(localDbPath, scopeName);

            // subscribe for errors that occur when applying changes to the client
            _provider.ApplyChangeFailed += ApplyChangeFailed;
        }

        public void Sync()
        {
            try
            {
                Action("Start client session", null, () =>
                {
                    _clientSession = new SyncSessionContext(_provider.IdFormats, new SyncCallbacks());
                    _provider.BeginSession(SyncProviderPosition.Local, _clientSession);
                });

                Action("Open client connection on " + _port, null, () =>
                {
                    _connection = new ClientTcpConnection("127.0.0.1", _port);
                });

                _connection.Start();
                Action("Start client sync", null, () => _connection.Send("Start"));
                SyncClient();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
            finally
            {
                _provider.EndSession(_clientSession);
                _connection.Stop();
            }
        }

        private void SyncClient()
        {
            Console.WriteLine("Receive batch size");
            var batchSize = (uint) _connection.Receive();
            Console.WriteLine("Batch size:{0}", batchSize);
            var knowledge = _connection.Receive() as SyncKnowledge;
            Console.WriteLine("knowledge:{0}", knowledge);
            object changedData;
            var changeBatch = _provider.GetChangeBatch(batchSize, knowledge, out changedData);

            Action("Send changed batch:" + changeBatch, null, () => _connection.Send(changeBatch));
            Action("Send changed data:" + changedData, null, () => _connection.Send(changedData));
        }

        public void StartListner()
        {
            try
            {
                Action("Open server connection on " + _port, null, () =>
                {
                    _connection = new ServerTcpConnection(_port);
                });
                _connection.Start();
                Action("Waiting for Sync", " => Sync started", () => _connection.Receive());

                Action("Start server session", null, () =>
                {
                    _provider.BeginSession(SyncProviderPosition.Remote, _serverSession);
                    _serverSession = new SyncSessionContext(_provider.IdFormats, new SyncCallbacks());
                });
                SyncServer();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                _provider.EndSession(_serverSession);
                _connection.Stop();
            }
        }

        private void SyncServer()
        {
            Action("Compute bach parameter", "", () =>
            {
                SyncKnowledge knowledge;
                uint batchSize;
                _provider.GetSyncBatchParameters(out batchSize, out knowledge);
                Console.WriteLine();
                Console.WriteLine(string.Format("BatchSize:{0}. Knowledge:{1}.", batchSize, knowledge));
                Action("Send bach size", null, () => _connection.Send(batchSize));
                Action("Send bach size", null, () => _connection.Send(knowledge));
            });


            ChangeBatch changeBatch = null;
            Action("Waiting change batch", null, () => changeBatch = _connection.Receive() as ChangeBatch);
            object changedData = null;
            Action("Waiting change batch", null, () => changedData = _connection.Receive());

            var syncStats = new SyncSessionStatistics();
            Action("Sync database", " => Ok : " + syncStats.ChangesApplied,
                () => _provider.ProcessChangeBatch(ConflictResolutionPolicy.DestinationWins, changeBatch, changedData,
                    new SyncCallbacks(), syncStats));
        }

        private void Action(string name, string resultOk, Action action)
        {
            try
            {
                Console.Write(name);
                action();
                Console.WriteLine(resultOk ?? " => Ok.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("=> Ko ({0}", ex.Message);
                throw;
            }
            
        }

        private void ApplyChangeFailed(object sender, DbApplyChangeFailedEventArgs e)
        {
            Console.WriteLine("Change failed:" + e.Error);
        }
    }
}
