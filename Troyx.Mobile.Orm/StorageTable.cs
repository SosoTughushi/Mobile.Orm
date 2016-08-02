using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Data.Common;

namespace Troyx.Mobile.Orm
{
    public abstract class StorageTable<TEntity, TFieldKey>
        where TEntity : class, new()
    {
        protected static TableBindingInfo<TFieldKey> BindingInfo = new TableBindingInfo<TFieldKey>();
        protected IDbCommandProvider CommandProvider;

        protected StorageTable(IDbCommandProvider commandProvider)
        {
            CommandProvider = commandProvider;
        }

        public abstract ISqlStorageQuery<TEntity, TFieldKey> CreateQuery();

        public static void SetBindings(string tableName, IEnumerable<TableFieldDescriptor<TFieldKey>> bindings)
        {
            BindingInfo.TableName = tableName;

            BindingInfo.Type = typeof(TEntity);
            BindingInfo.Bindings = new Dictionary<TFieldKey, TableFieldDescriptor<TFieldKey>>();
            BindingInfo.Fields = new Dictionary<TFieldKey, PropertyInfo>();

            bool hasPrimaryKey = false;

            foreach (var fieldDescriptor in bindings)
            {
                var field = BindingInfo.Type.GetProperty(fieldDescriptor.ClassFieldName);

                BindingInfo.Fields.Add(fieldDescriptor.FieldKey, field);

                var primaryFieldAttribute = field.GetCustomAttributes(typeof(PrimaryKey), false).Select(o => o as PrimaryKey).SingleOrDefault();

                if (primaryFieldAttribute != null)
                {
                    BindingInfo.PrimaryFieldDbName = fieldDescriptor.TableFieldName;
                    BindingInfo.IsIdentity = primaryFieldAttribute.IsIdentity;
                    BindingInfo.PrimaryField = field;
                    BindingInfo.PrimaryFieldKey = fieldDescriptor.FieldKey;
                    hasPrimaryKey = true;
                }

                BindingInfo.Bindings.Add(fieldDescriptor.FieldKey, fieldDescriptor);
            }

            if (!hasPrimaryKey)
            {
                throw new Exception(string.Format("No Primary Key defined in '{0}'", BindingInfo.Type.FullName));
            }
        }
    }
}
