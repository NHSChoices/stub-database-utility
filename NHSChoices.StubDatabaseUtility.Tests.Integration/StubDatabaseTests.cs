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

  public class StubDatabaseTestsBase
  {
    protected static void DropDatabase(ServerConnection destinationServerConnection)
    {
      var destinationServer = new Server(destinationServerConnection);

      if (destinationServer.Databases[destinationServerConnection.DatabaseName] != null)
      {
        destinationServer.Databases[destinationServerConnection.DatabaseName].Drop();
      }
    }

    protected static StubDatabaseUtility GetStubDatabaseUtility(ServerConnection serverConnection)
    {
      return new StubDatabaseUtility(serverConnection);
    }

    protected static ServerConnection GetServerConnection(string connectionStringName)
    {
      var sourceServerConnection = new ServerConnection(
        new SqlConnection(
          ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString
          )
        );
      return sourceServerConnection;
    }
  }

  [TestFixture]
  public class StubDatabaseTests : StubDatabaseTestsBase
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


    // ReSharper restore AssignNullToNotNullAttribute
     
  }

  [TestFixture]
  public class StubDatabaseCrudTests : StubDatabaseTestsBase
  {
    private ServerConnection _serverConnection;
    private StubDatabaseUtility _stubDatabaseUtility;
    //Not great tests - just sanity ones to make sure that nothing blows up
    // ReSharper disable AssignNullToNotNullAttribute

    [SetUp]
    public void SetUp()
    {
      _serverConnection = GetServerConnection("Target");
      _stubDatabaseUtility = GetStubDatabaseUtility(_serverConnection);
    }

    [TearDown]
    public void TearDown()
    {
      _stubDatabaseUtility.DeleteTableData();
    }

    [Test]
    public void InsertionToString()
    {
      //Arrange
      var insertion = new Insertion("[choices].[Condition]", "ConditionId, ConditionName, isDeleted", new[] { "1, 'Measles', 0", "2, 'Mumps', 0" });

      //Act
      //Assert
      Assert.That(insertion.GetInsertionString(), Is.EqualTo("insert into [choices].[Condition] (ConditionId, ConditionName, isDeleted) values (1, 'Measles', 0),(2, 'Mumps', 0)"));

    }

    [Test]
    public void DatabaseInsert()
    {
      //Arrange
      var insertionList = new[]
      {
        new Insertion(
          "[choices].[Condition]",
          "ConditionId, ConditionName, isDeleted",
          new []
          {
            "1, 'Measles', 0",
            "2, 'Mumps', 0"
          } ),
      };

      //Act
      _stubDatabaseUtility.InsertTableData(insertionList);

      //Assert
      Assert.That(new Server(_serverConnection).Databases[_serverConnection.DatabaseName].Tables["Condition", "choices"].RowCount, Is.EqualTo(2));

    }

    [Test]
    public void DatabaseInsertShouldThrowExceptionWhenTableUnknown()
    {
      //Arrange
      var insertionList = new[]
      {
        new Insertion(
          "[choices].[ZZZZZZZZ]",
          "",
          new [] { "" } ),
      };

      //Act + Assert
      var exception = Assert.Throws<Exception>(() => _stubDatabaseUtility.InsertTableData(insertionList));
      Assert.That(exception.Message, Is.EqualTo("Insert into table '[choices].[ZZZZZZZZ]' failed. See inner exception for details."));
    }

    [Test]
    public void DatabaseDeleteShouldHandleForeignKeyDependancies()
    {
      //Arrange
      var insertionList = new[]
      {
        new Insertion(
          "[choices].[Condition]",
          "ConditionId, ConditionName, isDeleted",
          new []
          {
            "1, 'Measles', 0",
            "2, 'Mumps', 0"
          } ),
        new Insertion(
          "[choices].[TreatmentType]",
          "TreatmentTypeID,TreatmentType,IsDeleted",
          new []
          {
          "1,'EDOS',0",
          } ),
        new Insertion(
          "[choices].[Treatment]",
          "[TreatmentID],[TreatmentName],[TreatmentTypeID],[isDeleted],[DisplayName]",
          new []
          {
            "1,'General Surgery - [General Surgery]',1,1,'General Surgery'"
          } )
      }; 
      _stubDatabaseUtility.InsertTableData(insertionList);

      //Act
      _stubDatabaseUtility.DeleteTableData();

      //Assert
      var database = new Server(_serverConnection).Databases[_serverConnection.DatabaseName];

      Assert.That(database.Tables["Condition", "choices"].RowCount, Is.EqualTo(0));
      Assert.That(database.Tables["TreatmentType", "choices"].RowCount, Is.EqualTo(0));
      Assert.That(database.Tables["Treatment", "choices"].RowCount, Is.EqualTo(0));

    }

    [Test, Ignore("Can't think of how to get delete to fail yet")]
    public void DeleteTableDataShouldThrowExceptionWhenTableUnknown()
    {
      //Arrange

      //Act + Assert
      var exception = Assert.Throws<Exception>(() => _stubDatabaseUtility.DeleteTableData());
      Assert.That(exception.Message, Is.EqualTo("Delete from table 'UnknownTable' failed. See inner exception for details."));
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

    // ReSharper restore AssignNullToNotNullAttribute
     
  }

}