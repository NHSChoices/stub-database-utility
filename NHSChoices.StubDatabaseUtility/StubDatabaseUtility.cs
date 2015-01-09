namespace NHSChoices.StubDatabaseUtility
{
  using System;
  using System.IO;
  using System.Text.RegularExpressions;
  using Microsoft.SqlServer.Management.Common;
  using Microsoft.SqlServer.Management.Smo;

  public class StubDatabaseUtility
  {
    /// <summary>
    /// This method will create a copy of the DB referenced by sourceServerConnection in the DB referenced by destinationServerConnection
    /// The copy does not contain any data
    /// </summary>
    public static void CopyDatabaseWithoutData(ServerConnection sourceServerConnection, ServerConnection destinationServerConnection)
    {
      var script = CreateDatabaseSchemaScript(sourceServerConnection);

      var correctedScript = CorrectInvalidWithStatements(script);

      BuildDbCopy(destinationServerConnection.ServerInstance, destinationServerConnection.DatabaseName, correctedScript);
    }

    /// <summary>
    /// This method clears out all the data from a database. It should be run before each test using the 
    /// test databases.
    /// </summary>
    public static void ClearDatabaseTables(ServerConnection serverConnection)
    {
      var srv = new Server(serverConnection);

      var db = srv.Databases[serverConnection.DatabaseName];

      foreach (Table table in db.Tables)
      {
        table.TruncateData();
      }
    }

    private static string CorrectInvalidWithStatements(string script)
    {
      return Regex.Replace(script, "with (.+) as", ";$&", RegexOptions.IgnoreCase);
    }

    private static string CreateDatabaseSchemaScript(ServerConnection sourceServerConnection)
    {
      var sourceServer = new Server(sourceServerConnection);
      var sourceDb = sourceServer.Databases[sourceServerConnection.DatabaseName];

      var tempFileName = Path.GetTempFileName();

      var xfr = new Transfer(sourceDb)
      {
        CopyData = false,
        PreserveLogins = true,
        CopyAllLogins = false,
        CopyAllTables = true,
        CopyAllUsers = false,
        Options = {WithDependencies = true, FileName = tempFileName,},
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
