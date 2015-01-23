namespace NHSChoices.StubDatabaseUtility
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Text.RegularExpressions;
  using Microsoft.SqlServer.Management.Common;
  using Microsoft.SqlServer.Management.Smo;

  public class StubDatabaseUtility
  {
    private readonly ServerConnection _destinationServerConnection;
    private string _correctedScript;
    private readonly List<Insertion> _insertionList = new List<Insertion>();

    public StubDatabaseUtility(ServerConnection destinationServerConnection)
    {
      _destinationServerConnection = destinationServerConnection;
    }

    /// <summary>
    /// This method will apply the schema of the DB referenced by sourceServerConnection
    /// If the  
    /// </summary>
    public void CopySchemaFromDatabase(ServerConnection sourceServerConnection)
    {
      var script = CreateDatabaseSchemaScript(sourceServerConnection);

      _correctedScript = CorrectInvalidWithStatements(script);

      BuildDbCopy(_destinationServerConnection.ServerInstance, _destinationServerConnection.DatabaseName, _correctedScript);
    }

    private static Database GetDatabase(ServerConnection serverConnection)
    {
      var srv = new Server(serverConnection);
      var db = srv.Databases[serverConnection.DatabaseName];
      return db;
    }

    private static string CorrectInvalidWithStatements(string script)
    {
      return Regex.Replace(script, "with (.+) as", ";$&", RegexOptions.IgnoreCase);
    }

    private static string CreateDatabaseSchemaScript(ServerConnection sourceServerConnection)
    {
      var db = GetDatabase(sourceServerConnection);

      var tempFileName = Path.GetTempFileName();

      var xfr = new Transfer(db)
      {
        CopyData = false,
        PreserveLogins = true,
        CopyAllLogins = false,
        CopyAllTables = true,
        CopyAllUsers = false,
        Options = {WithDependencies = true, FileName = tempFileName, DriAllConstraints = true, DriForeignKeys = true},
        //DestinationDatabase = arguments.DestinationDatabaseName,
        //DestinationServer = sourceServer.Name,
        //DestinationLoginSecure = true,
        CopySchema = true,
        
      };

      //Script the transfer. Alternatively perform immediate data transfer 
      // with TransferData method. 
      //xfr.TransferData();

      xfr.ScriptTransfer();

      var readAllText = File.ReadAllText(xfr.Options.FileName);

      return readAllText;
    }

    private static Database BuildDbCopy(string server, string database, string updateScript)
    {
      var db = CreateBlankDb(server, database);
      db.ExecuteNonQuery(updateScript, ExecutionTypes.ContinueOnError);
      return db;
    }

    private static Database CreateBlankDb(string server, string database)
    {
      var destinationServer = new Server(server);

      if (destinationServer.Databases[database] != null)
      {
        destinationServer.Databases[database].Drop();
      }

      var dbCopy = new Database(destinationServer, database);
      dbCopy.Create();

      return dbCopy;


    }

    public void InsertTableData(Insertion[] insertionList)
    {

      var db = GetDatabase(_destinationServerConnection);

      foreach (var insertion in insertionList)
      {
        try
        {
          db.ExecuteNonQuery(insertion.GetInsertionString());
          _insertionList.Add(insertion);
        }
        catch (Exception ex)
        {
          throw new Exception(string.Format("Insert into table '{0}' failed. See inner exception for details.", insertion.TableName), ex);
        }
      }

    }

    public void DeleteTableData()
    {
      var db = GetDatabase(_destinationServerConnection);

      foreach (var insertion in _insertionList.AsEnumerable().Reverse() )
      {
        try
        {
          db.ExecuteNonQuery(insertion.GetDeletionString());
        }
        catch (Exception ex)
        {
          throw new Exception(string.Format("Delete from table '{0}' failed. See inner exception for details.", insertion.TableName), ex);
        }
      }

    }
  }
}
