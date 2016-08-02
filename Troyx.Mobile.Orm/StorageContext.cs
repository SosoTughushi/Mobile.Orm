using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using System.Data;

namespace Troyx.Mobile.Orm
{
    public abstract class StorageContext : IDisposable, IDbCommandProvider
    {
        private static bool _isBindingsDone = false;
        private static readonly object _locker_static = new object();

        public string ConnectionString { get; private set; }

        private DbConnection _connection;
        private DbTransaction _transaction;

        protected abstract void SetBindings();
        protected abstract DbConnection CreateConnection(string connectionString);

        public StorageContext(string connectionString)
        {

            lock (_locker_static)
            {
                if (!_isBindingsDone)
                {
                    SetBindings();
                    _isBindingsDone = true;
                }
            }

            ConnectionString = connectionString;

            _connection = CreateConnection(connectionString);
            _connection.Open();
        }

        public DbCommand CreateDbCommand()
        {
            if (_transaction == null)
            {
                _transaction = _connection.BeginTransaction();
            }

            DbCommand command = _connection.CreateCommand();
            command.Transaction = _transaction;
            return command;
        }

        public void SaveChanges()
        {
            if (_transaction != null)
            {
                _transaction.Commit();
                _transaction.Dispose();
                _transaction = null;
            }
        }

        public virtual void Dispose()
        {
            if (_transaction != null)
            {
                _transaction.Dispose();
                _transaction = null;
            }

            if (_connection != null)
            {
                _connection.Dispose();
                _connection = null;
            }
        }
    }
}
