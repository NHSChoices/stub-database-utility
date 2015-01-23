namespace NHSChoices.StubDatabaseUtility
{
  using System.Linq;

  public class Insertion
  {
    public string TableName { get; private set; }
    public string ColumnList { get; private set; }
    public string[] InsertionStrings { get; private set; }

    public Insertion(string tableName, string columnList, string[] insertionStrings)
    {
      TableName = tableName;
      ColumnList = columnList;
      InsertionStrings = insertionStrings;
    }

    public string GetInsertionString()
    {
      var inserts = string.Join(",", InsertionStrings.Select(s => "(" + s + ")"));
      return string.Format("insert into {0} ({1}) values {2}", TableName, ColumnList, inserts);
    }

    public string GetDeletionString()
    {
      return string.Format("delete from {0}", TableName);
    }
  }
}