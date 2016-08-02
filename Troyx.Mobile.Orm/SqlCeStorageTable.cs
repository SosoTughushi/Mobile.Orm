using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace Troyx.Mobile.Orm
{
    public class SqlCeStorageTable<TEntity, TFieldKey> : StorageTable<TEntity, TFieldKey>
        where TEntity : class, new()
    {
        public SqlCeStorageTable(IDbCommandProvider commandProvider)
            : base(commandProvider)
        {
        }

        public override ISqlStorageQuery<TEntity, TFieldKey> CreateQuery()
        {
            return new SqlStorageQuery<TEntity, TFieldKey>(CommandProvider, BindingInfo);
        }
    }
}
