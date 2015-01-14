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
      var destinationServer = new Server(destinationServerConnection);

      if (destinationServer.Databases[destinationServerConnection.DatabaseName] != null)
      {
        destinationServer.Databases[destinationServerConnection.DatabaseName].Drop();        
      }

      var stubDatabaseUtility = new StubDatabaseUtility(destinationServerConnection);

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
      var assembly = Assembly.GetExecutingAssembly();
      const string resourceName = "NHSChoices.StubDatabaseUtility.Tests.Integration.TestResources.TableNames.txt";

      string result;
      using (var reader = new StreamReader(assembly.GetManifestResourceStream(resourceName)))
      {
        result = reader.ReadToEnd();
      }
      var tableList = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

      var serverConnection = GetServerConnection("Target");
      var stubDatabaseUtility = new StubDatabaseUtility(serverConnection);

      //Act

      stubDatabaseUtility.ClearDatabaseTables(tableList);

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

    // ReSharper restore AssignNullToNotNullAttribute
     
  }
}