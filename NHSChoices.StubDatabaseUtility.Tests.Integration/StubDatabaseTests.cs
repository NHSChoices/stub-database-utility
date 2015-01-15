namespace NHSChoices.StubDatabaseUtility.Tests.Integration
{
  using System;
  using System.Configuration;
  using System.Data.SqlClient;
  using System.IO;
  using System.Reflection;
  using Microsoft.SqlServer.Management.Common;
  using Microsoft.SqlServer.Management.Smo;
  using NUnit.Framework;

  [TestFixture]
  public class StubDatabaseTests
  {
    //Not great tests - just sanity ones to make sure that nothing blows up
    // ReSharper disable AssignNullToNotNullAttribute

    [SetUp]
    public void SetUp()
    {
    }

    [Test]
    public void DatabaseCopy()
    {
      //Arrange
      var sourceServerConnection = GetServerConnection("Source");
      var destinationServerConnection = GetServerConnection("Target");
      DropDatabase(destinationServerConnection);
      var stubDatabaseUtility = GetStubDatabaseUtility(destinationServerConnection);

      //Act
      stubDatabaseUtility.CopySchemaFromDatabase(sourceServerConnection);

      //Assert
      Assert.That(new Server(destinationServerConnection).Databases[destinationServerConnection.DatabaseName], Is.Not.Null);
      Assert.That(new Server(destinationServerConnection).Databases[destinationServerConnection.DatabaseName].Schemas["choices"], Is.Not.Null);
    }

    [Test]
    public void DatabaseTableClear()
    {
      //Arrange
      var tableList = GetTableList();
      var serverConnection = GetServerConnection("Target");
      var stubDatabaseUtility = GetStubDatabaseUtility(serverConnection);

      //Act
      stubDatabaseUtility.ClearDatabaseTables(tableList);

      //Assert
      Assert.That(new Server(serverConnection).Databases[serverConnection.DatabaseName].Tables[0].RowCount, Is.EqualTo(0));

    }

    [Test]
    public void DatabaseTableClearShouldThrowExceptionWhenTableUnknown()
    {
      //Arrange
      var serverConnection = GetServerConnection("Target");
      var stubDatabaseUtility = GetStubDatabaseUtility(serverConnection);

      //Act + Assert
      var exception = Assert.Throws<Exception>(() => stubDatabaseUtility.ClearDatabaseTables(new[] { "UnknownTable" }));
      Assert.That(exception.Message, Is.EqualTo("Delete from table 'UnknownTable' failed. See inner exception for details."));
    }

    private static void DropDatabase(ServerConnection destinationServerConnection)
    {
      var destinationServer = new Server(destinationServerConnection);

      if (destinationServer.Databases[destinationServerConnection.DatabaseName] != null)
      {
        destinationServer.Databases[destinationServerConnection.DatabaseName].Drop();
      }
    }

    private static StubDatabaseUtility GetStubDatabaseUtility(ServerConnection serverConnection)
    {
      return new StubDatabaseUtility(serverConnection);
    }

    private static string[] GetTableList()
    {
      var assembly = Assembly.GetExecutingAssembly();
      const string resourceName = "NHSChoices.StubDatabaseUtility.Tests.Integration.TestResources.TableNames.txt";

      string result;
      using (var reader = new StreamReader(assembly.GetManifestResourceStream(resourceName)))
      {
        result = reader.ReadToEnd();
      }
      var tableList = result.Split(new[] {Environment.NewLine}, StringSplitOptions.None);
      return tableList;
    }

    private static ServerConnection GetServerConnection(string connectionStringName)
    {
      var sourceServerConnection = new ServerConnection(
        new SqlConnection(
          ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString
          )
        );
      return sourceServerConnection;
    }

    // ReSharper restore AssignNullToNotNullAttribute
     
  }
}