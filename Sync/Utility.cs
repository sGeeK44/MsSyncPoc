using System;
using System.Data.SqlServerCe;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.Synchronization.Data;
using Microsoft.Synchronization.Data.SqlServerCe;

namespace Sync
{
    public class Utility
    {
        //Make server changes that are synchronized on the second 
        //synchronization.
        public static void MakeDataChanges(string db)
        {
            int rowCount = 0;

            using (var serverConn = new SqlCeConnection(db))
            {
                serverConn.Open();
                var sqlCommand = serverConn.CreateCommand();
                sqlCommand.CommandText = "INSERT INTO Customer (CustomerName) VALUES ('Cycle Mart');";
                rowCount += sqlCommand.ExecuteNonQuery();
                sqlCommand.CommandText = "UPDATE Customer SET  SalesPerson = 'James Bailey' WHERE CustomerName = 'Tandem Bicycle Store'";
                rowCount += sqlCommand.ExecuteNonQuery();
                sqlCommand.CommandText = "DELETE FROM Customer WHERE CustomerName = 'Sharp Bikes'";
                rowCount += sqlCommand.ExecuteNonQuery();
                serverConn.Close();
            }

            Console.WriteLine("Rows inserted, updated, or deleted at the server: " + rowCount);
        }

        public static void CreateDatabase(string path)
        {
            using (var clientConn = new SqlCeConnection(path))
            {
                if (File.Exists(clientConn.Database))
                {
                    File.Delete(clientConn.Database);
                }
            }

            var sqlCeEngine = new SqlCeEngine(path);
            sqlCeEngine.CreateDatabase();
            CreateTable(path);
        }

        private static void CreateTable(string path)
        {
            using (var clientConn = new SqlCeConnection(path))
            {
                clientConn.Open();
                using (var command = clientConn.CreateCommand())
                {
                    command.CommandText = "CREATE TABLE [Customer] (Id integer PRIMARY KEY IDENTITY, CustomerName nvarchar (200), SalesPerson nvarchar (200)); ";
                    command.ExecuteNonQuery();
                    command.CommandText = "INSERT INTO Customer (CustomerName, SalesPerson) VALUES ('Bike Mart', 'FPE');";
                    command.ExecuteNonQuery();
                    command.CommandText = "INSERT INTO Customer (CustomerName, SalesPerson) VALUES ('Tandem Bicycle Store', 'FPE');";
                    command.ExecuteNonQuery();
                    command.CommandText = "INSERT INTO Customer (CustomerName, SalesPerson) VALUES ('Sharp Bikes', 'FPE');";
                    command.ExecuteNonQuery();
                }
            }
        }

        public static DbSyncScopeDescription ProvisionDatabase(string db, string scopeName)
        {
            var serverConn = new SqlCeConnection(db);

            // define a new scope named ProductsScope
            var scopeDesc = new DbSyncScopeDescription(scopeName);

            // get the description of the Products table from SyncDB dtabase
            var tableDesc = SqlCeSyncDescriptionBuilder.GetDescriptionForTable("Customer", serverConn);

            // add the table description to the sync scope definition
            scopeDesc.Tables.Add(tableDesc);

            // create a server scope provisioning object based on the ProductScope
            var serverProvision = new SqlCeSyncScopeProvisioning(serverConn, scopeDesc);

            // skipping the creation of table since table already exists on server
            serverProvision.SetCreateTableDefault(DbSyncCreationOption.Skip);

            // start the provisioning process
            serverProvision.Apply();

            // get the description of ProductsScope from the SyncDB server database
            return SqlCeSyncDescriptionBuilder.GetDescriptionForScope(scopeName, serverConn);
        }

        public static void ProvisionningDatabase(string db, DbSyncScopeDescription scopeDesc)
        {
            // create a connection to the SyncCompactDB database
            var clientConn = new SqlCeConnection(db);

            // create CE provisioning object based on the ProductsScope
            var clientProvision = new SqlCeSyncScopeProvisioning(clientConn, scopeDesc);

            // starts the provisioning process
            clientProvision.Apply();
        }

        public static SqlCeSyncProvider CreateProvider(string db, string scopeName)
        {
            var clientConn = new SqlCeConnection(db);
            var clientProvider = new SqlCeSyncProvider(scopeName, clientConn);
            return clientProvider;
        }

        public static MemoryStream SerializeToStream(object o)
        {
            var stream = new MemoryStream();
            IFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, o);
            return stream;
        }

        public static object DeserializeFromStream(MemoryStream stream)
        {
            IFormatter formatter = new BinaryFormatter();
            stream.Seek(0, SeekOrigin.Begin);
            var o = formatter.Deserialize(stream);
            return o;
        }
    }
}