namespace NHSChoices.StubDatabaseUtility
{
  using System;
  using System.IO;
  using System.Text.RegularExpressions;
  using Microsoft.SqlServer.Management.Common;
  using Microsoft.SqlServer.Management.Smo;

  public class StubDatabaseUtility
  {
    private readonly ServerConnection _destinationServerConnection;
    private string _correctedScript;

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

    /// <summary>
    /// This method clears out all the data from a database. It should be run before each test using the 
    /// test databases.
    /// </summary>
    /// <param name="tableList"></param>
    public void ClearDatabaseTables(string[] tableList)
    {

      var db = GetDatabase(_destinationServerConnection);

      foreach (var table in tableList)
      {
        //table.TruncateData(); //Can't truncate data when FK constraints exist
        db.ExecuteNonQuery(string.Format("delete from {0}", table));
      }
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
  }

}
