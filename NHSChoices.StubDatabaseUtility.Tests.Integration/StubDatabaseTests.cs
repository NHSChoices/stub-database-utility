namespace NHSChoices.StubDatabaseUtility.Tests.Integration
{
  using System.Configuration;
  using System.Data.SqlClient;
  using Microsoft.SqlServer.Management.Common;
  using Microsoft.SqlServer.Management.Smo;
  using NUnit.Framework;

  [TestFixture]
  public class StubDatabaseTests
  {
    //Not great tests - just sanity ones to make sure that nothing blows up

    [Test]
    public void DatabaseCopy()
    {
      //Arrange

      var sourceServerConnection = GetServerConnection("Source");
      var destinationServerConnection = GetServerConnection("Destination");

      //Act
      StubDatabaseUtility.CopyDatabase(sourceServerConnection, destinationServerConnection);

      //Assert
      Assert.That(new Server(destinationServerConnection).Databases[destinationServerConnection.DatabaseName], Is.Not.Null);
    }

    [Test]
    public void DatabaseTableClear()
    {
      //Arrange
      var serverConnection = GetServerConnection("Destination");

      //Act
      StubDatabaseUtility.ClearTables(serverConnection);

      //Assert
      Assert.That(new Server(serverConnection).Databases[serverConnection.DatabaseName].Tables[0].RowCount, Is.EqualTo(0));

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


     
  }
}