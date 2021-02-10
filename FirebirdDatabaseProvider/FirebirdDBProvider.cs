using System.Data;
using FirebirdSql.Data.FirebirdClient;

namespace FirebirdDatabaseProvider
{
    public class FirebirdDBProvider
    {
        private readonly string _dbConnectionString;
        private FbConnection _connection;

        public bool ConnectionIsOpened
        {
            get => _connection.State == ConnectionState.Open;
        }

        public FbConnection Connection
        {
            get => _connection;
        }

        public FirebirdDBProvider(string connectionString)
        {
            _dbConnectionString = connectionString;
        }

        public void OpenConnection()
        {
            _connection = new FbConnection(_dbConnectionString);
            _connection.Open();
        }

        public void CloseConnection()
        {
            if (_connection?.State != ConnectionState.Closed)
                _connection?.Close();
            _connection.Dispose();
        }
    }
}
