using System;
namespace FirebirdDatabaseProvider.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class FBTableNameAttribute : Attribute
    {
        public string TableName { get; set; }

        public FBTableNameAttribute(string tableName)
        {
            TableName = tableName;
        }
    }
}
