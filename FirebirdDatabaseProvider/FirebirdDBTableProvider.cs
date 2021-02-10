using FirebirdDatabaseProvider.Extensions;
using FirebirdDatabaseProvider.Attributes;
using FirebirdSql.Data.FirebirdClient;
using System;
using System.Text;
using System.Reflection;
using System.Collections.Generic;

namespace FirebirdDatabaseProvider
{
    public class FirebirdDBTableProvider<T> where T : new()
    {
        private const string COMMA_SPLITTER = ", ";
        private static Type _tableEntityType;

        /// <summary>
        /// Table name in database
        /// </summary>
        private static readonly string _tableName;

        /// <summary>
        /// Key - property name, value - FBTableNameAttribute.
        /// </summary>
        private static readonly Dictionary<string, FBTableFieldAttribute> _tableFieldInfoDict;

        /// <summary>
        /// Key - table field name, value - Autoincrement generators name.
        /// </summary>
        private static readonly Dictionary<string, string> _tableFieldAutoincrementInfoDict;

        //private readonly string _dbConnectionString;
        //private FbConnection _connection;
        private FirebirdDBProvider _dbProvider;
        private FbCommand _command;
        
        public string TableName { get => _tableName; }

        static FirebirdDBTableProvider()
        {
            _tableEntityType = typeof(T);

            var attrs = _tableEntityType.GetCustomAttributes(typeof(FBTableNameAttribute), false);
            if (attrs.Length == 0)
                throw new Exception("Class is not marked as a \"Table\" by TableNameAttribute!");

            var tableName = ((FBTableNameAttribute)attrs[0]).TableName;
            if (tableName.FBIdentifierLengthIsTooLong())
                throw new Exception("Too long table name (31 bytes max!)");

            _tableName = tableName;

            _tableFieldInfoDict = new Dictionary<string, FBTableFieldAttribute>();
            _tableFieldAutoincrementInfoDict = new Dictionary<string, string>();

            var baseType = _tableEntityType;
            var inheritTypesList = new List<Type>();
            do
            {
                inheritTypesList.Add(baseType);
                baseType = baseType.BaseType; //Получаем родительский класс
            }
            while (baseType.FullName != "System.Object");
            inheritTypesList.Reverse();

            PropertyInfo[] properties;
            FBTableFieldAttribute attr;
            string propertyName, generatorName, fieldName;
            object[] propAttrs;

            foreach (var type in inheritTypesList)
            {
                properties = type.GetProperties(BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.DeclaredOnly | BindingFlags.Instance);

                for (int i = 0; i < properties.Length; i++)
                {
                    propertyName = properties[i].Name;

                    propAttrs = properties[i].GetCustomAttributes(typeof(FBTableFieldAttribute), false);
                    if (propAttrs.Length > 0)
                    {
                        attr = propAttrs[0] as FBTableFieldAttribute;
                        _tableFieldInfoDict.Add(propertyName, attr);

                        fieldName = attr.TableFieldName;
                        if(fieldName.FBIdentifierLengthIsTooLong())
                            throw new Exception($"Too long field name associated with property \"{propertyName}\". (31 bytes max!)");

                        propAttrs = properties[i].GetCustomAttributes(typeof(FBFieldAutoincrementAttribute), false);
                        if (propAttrs.Length > 0)
                        {
                            generatorName = (propAttrs[0] as FBFieldAutoincrementAttribute).GeneratorName;
                            if (generatorName.FBIdentifierLengthIsTooLong())
                                throw new Exception($"Too long generator name associated with property \"{propertyName}\". (31 bytes max!)");
                            _tableFieldAutoincrementInfoDict.Add(fieldName, generatorName);
                        }
                    }
                }
            }
        }

        public FirebirdDBTableProvider(FirebirdDBProvider dbProvider)
        {
            //_dbConnectionString = connectionString;
            _dbProvider = dbProvider;
        }

        //public void OpenConnection()
        //{
        //    _connection = new FbConnection(_dbConnectionString);
        //    _connection.Open();
        //}

        //public void CloseConnection()
        //{
        //    if(_connection?.State != ConnectionState.Closed)
        //        _connection?.Close();
        //    _connection.Dispose();
        //}

        public void CreateTableIfNotExists()
        {
            if (!TableExists())
            {
                var createTableQueryBuilder = new StringBuilder($@"CREATE TABLE {_tableName} (");

                string fieldName, fieldDBType, notNull, primaryKey, splitter = string.Empty;
                foreach (var fieldAttr in _tableFieldInfoDict.Values)
                {
                    fieldName = fieldAttr.TableFieldName;
                    fieldDBType = fieldAttr.TypeName;
                    notNull = fieldAttr.IsNotNull ? " NOT NULL" : string.Empty;
                    primaryKey = fieldAttr.IsPrymaryKey ? " PRIMARY KEY" : string.Empty;
                    createTableQueryBuilder.Append($"{splitter}{fieldName} {fieldDBType}{notNull}{primaryKey}");
                    splitter = COMMA_SPLITTER;
                }

                createTableQueryBuilder.Append(");");
                ExecuteNonQuery(createTableQueryBuilder.ToString());

                foreach (var fieldGenNamesPair in _tableFieldAutoincrementInfoDict)
                    CreateFieldAutoincrement(fieldGenNamesPair.Key, fieldGenNamesPair.Value);
            }
        }

        public void AddItem(T item)
        {
            var sqlInsertRequestString = GetInsertRequestString(item);
            ExecuteNonQuery(sqlInsertRequestString);
        }

        private object GetSingleObjBySQL(string sqlRequest)
        {
            object result;
            using (var command = new FbCommand(sqlRequest, _dbProvider.Connection))
            {
                result = command.ExecuteScalar();
            }
            return result;
        }

        private void CreateBoolIntDBDomain(string domainName)
        {
            if (domainName.FBIdentifierLengthIsTooLong())
                throw new Exception($"Too long domain name! (31 bytes max!)");

            var createBoolDomainSql = $@"CREATE DOMAIN {domainName} AS INTEGER DEFAULT 0 NOT NULL CHECK (VALUE IN(0,1));";
            ExecuteNonQuery(createBoolDomainSql);
        }

        public bool TableExists() => DatabaseMemberExists("RDB$RELATIONS", "RDB$RELATION_NAME", _tableName);

        private bool GeneratorExists(string generatorName) => DatabaseMemberExists("RDB$GENERATORS", "RDB$GENERATOR_NAME", generatorName);

        private bool DatabaseMemberExists(string tableName, string tableMemberFieldName, string memberName)
        {
            var sqlCheckTableExistString = $@"SELECT 1 FROM {tableName} tn WHERE tn.{tableMemberFieldName} = '{memberName}'";
            var result = GetSingleObjBySQL(sqlCheckTableExistString);
            return result != null;
        }

        public ICollection<T> GetAllItemsFromTable()
        {
            var itemsCollection = new List<T>();
            var sqlSelectQueryString = $"SELECT * FROM {_tableName};";
            FbDataReader reader;
            using (_command = new FbCommand(sqlSelectQueryString, _dbProvider.Connection))
            {
                reader = _command.ExecuteReader();
                if (!reader.HasRows)
                    return null;

                T item;
                while (reader.Read())
                {
                    item = new T();
                    foreach (var pair in _tableFieldInfoDict)
                    {
                        var tableFieldValue = reader[pair.Value.TableFieldName];
                        _tableEntityType.GetProperty(pair.Key).SetValue(item, tableFieldValue);
                    }
                    itemsCollection.Add(item);
                }
            }

            return itemsCollection;
        }

        private void ExecuteNonQuery(string query)
        {
            using (_command = new FbCommand(query, _dbProvider.Connection))
            {
                _command.ExecuteNonQuery();
            }
        }


        private string GetInsertRequestString(T item)
        {
            string stringValue, genName, splitter = string.Empty;
            object propValue;
            var sqlRequestStringBuilder1 = new StringBuilder($@"INSERT INTO {_tableName} (");
            var sqlRequestStringBuilder2 = new StringBuilder(") VALUES (");

            foreach (var info in _tableFieldInfoDict)
            {
                propValue = _tableEntityType.GetProperty(info.Key).GetValue(item);
                if(_tableFieldAutoincrementInfoDict.ContainsKey(info.Value.TableFieldName))
                {
                    genName = _tableFieldAutoincrementInfoDict[info.Value.TableFieldName];
                    stringValue = GetGeneratorNextValue(genName).ToString();
                }
                else
                    stringValue = GetSqlTypeStringValue(propValue);
                
                sqlRequestStringBuilder1.Append(splitter + info.Value.TableFieldName);
                sqlRequestStringBuilder2.Append(splitter + stringValue);
                splitter = COMMA_SPLITTER;
            }
            sqlRequestStringBuilder2.Append(");");
            
            var sqlRequestString = sqlRequestStringBuilder1.ToString() + sqlRequestStringBuilder2.ToString();
            return sqlRequestString;
        }

        /// <summary>
        /// Set autoincrement to Table field
        /// </summary>
        /// <param name="tableFieldName">Table field name wich need an autoincrement</param>
        /// <returns>Database Generator name</returns>
        private void CreateFieldAutoincrement(string tableFieldName, string generatorName)
        {
            FbCommand command;
            if (!GeneratorExists(generatorName))
            {
                using (command = new FbCommand($@"CREATE GENERATOR {generatorName};", _dbProvider.Connection))
                    command.ExecuteNonQuery();
            }

            var triggerName = $"TRG_{Math.Abs(tableFieldName.GetHashCode())}";
            var builder = new StringBuilder($@"CREATE TRIGGER {triggerName} FOR {_tableName} ACTIVE BEFORE INSERT AS BEGIN ");
            builder.Append($@"IF (NEW.{tableFieldName} IS NULL) THEN NEW.{tableFieldName} = GEN_ID({generatorName}, 1); END;");

            using (command = new FbCommand(builder.ToString(), _dbProvider.Connection))
                command.ExecuteNonQuery();
        }

        private long GetGeneratorNextValue(string generatorName)
        {
            var sqlRequestString = $@"SELECT GEN_ID({generatorName}, 1) FROM RDB$DATABASE";
            using (var command = new FbCommand(sqlRequestString, _dbProvider.Connection))
            {
                var result = command.ExecuteScalar();
                if (result != null)
                    return (long)result;
                throw new Exception("Не удалось получить следующий номер ID!");
            }
        }

        private string GetSqlTypeStringValue(object value)
        {
            var propType = value.GetType();

            switch (propType.Name)
            {
                case "String": return $@"'{value}'";
                case "Boolean": return ((bool)value).ToFireBirdDBBoolInt().ToString();
                case "Double": return ((double)value).ToFBSqlString();
                case "Decimal": return ((decimal)value).ToFBSqlString();
                default: return value.ToString();
            }
        }
    }
}
