using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using System.Reflection;

namespace Troyx.Mobile.Orm
{
    public class SqlStorageQuery<TEntity, TFieldKey> : Troyx.Mobile.Orm.ISqlStorageQuery<TEntity,TFieldKey> where TEntity : class, new()
    {
        //////////////////// Private fields:

        private static Dictionary<QueryFilterOperator, string> _queryFilterOperatorSigns = new Dictionary<QueryFilterOperator, string>()
        {
            {QueryFilterOperator.Equals, "="},
            {QueryFilterOperator.GreaterThen, ">"},
            {QueryFilterOperator.GreaterThenOrEqualTo, ">="},
            {QueryFilterOperator.LessThen, "<"},
            {QueryFilterOperator.LessThenOrEqualTo, "<="},
        };

        private IDbCommandProvider _commandProvider;
        private TableBindingInfo<TFieldKey> _tableBindingInfo;

        private IEnumerable<QueryFilter<TFieldKey>> _queryFiltersAll;
        private IEnumerable<QueryFilter<TFieldKey>> _queryFiltersAny;
        private IDictionary<TFieldKey, bool> _orderingFields;
        private int _takeCount;
        private int _skipCount;

        //////////////////// Constructors:

        public SqlStorageQuery(IDbCommandProvider commandProvider, TableBindingInfo<TFieldKey> tableBindingInfo)
        {
            _commandProvider = commandProvider;
            _tableBindingInfo = tableBindingInfo;
            
        }

        //////////////////// Public members - filtering methods:

        public SqlStorageQuery<TEntity, TFieldKey> Where(params QueryFilter<TFieldKey>[] queryFilters)
        {
            _queryFiltersAll = queryFilters;
            return this;
        }

        public SqlStorageQuery<TEntity, TFieldKey> WhereAny(params QueryFilter<TFieldKey>[] queryFilters)
        {
            _queryFiltersAny = queryFilters;
            return this;
        }

        public SqlStorageQuery<TEntity, TFieldKey> Take(int count)
        {
            _takeCount = count;
            return this;
        }

        public SqlStorageQuery<TEntity, TFieldKey> Skip(int count)
        {
            _skipCount = count;
            throw new NotImplementedException("Skip functionality is not implemented in this version!");
            return this;
        }

        public SqlStorageQuery<TEntity, TFieldKey> OrderBy(TFieldKey field, bool ascending)
        {
            if (_orderingFields != null)
                throw new Exception("Chaining of OrderBy is not supported - use ThenBy!");

            _orderingFields = new Dictionary<TFieldKey, bool>();
            _orderingFields.Add(field, ascending);

            return this;
        }

        public SqlStorageQuery<TEntity, TFieldKey> ThenBy(TFieldKey field, bool ascending)
        {
            if (_orderingFields == null)
                throw new Exception("OrderBy should be used before ThenBy!");

            _orderingFields.Add(field, ascending);

            return this;
        }

        //////////////////// Public members - executor methods:

        public TEntity SelectSingle()
        {
            return _SelectSingleRecord(false);
        }

        public TEntity SelectSingleOrDefault()
        {
            return _SelectSingleRecord(true);
        }

        public IEnumerable<TEntity> SelectMany()
        {
            using (var command = _CreateCommand())
            {
                command.CommandText =
                    string.Format("SELECT {0}* FROM [{1}]\r\n{2}\r\n{3}",
                    _takeCount == 0 ? "" : string.Format("TOP({0}) ", _takeCount),
                    _tableBindingInfo.TableName,
                    _GenerateWHERE(command),
                    _GenerateORDERBY());

                using (var reader = command.ExecuteReader())
                {
                    List<TEntity> entities = new List<TEntity>();

                    while (reader.Read())
                        entities.Add(_ExtractEntityFromReader(reader));

                    return entities;
                }
            }
        }

        public int Delete()
        {
            using (var command = _CreateCommand())
            {
                command.CommandText = string.Format("DELETE FROM [{0}]\r\n{1}",
                    _tableBindingInfo.TableName,
                    _GenerateWHERE(command));

                return command.ExecuteNonQuery();
            }
        }

        public int Count()
        {
            using (var command = _CreateCommand())
            {
                command.CommandText = String.Format("SELECT COUNT(*) FROM [{0}]\r\n{1}",
                    _tableBindingInfo.TableName,
                    _GenerateWHERE(command));

                return (int)command.ExecuteScalar();
            }
        }

        public int UpdateSingle(TEntity entity, params TFieldKey[] keys)
        {
            using (var command = _CreateCommand())
            {
                Dictionary<TFieldKey, object> fieldNewValues = keys.ToDictionary(
                    k => k,
                    k => _tableBindingInfo.Fields[k].GetValue(entity,null));

                _AddParameterToCommand(command, "PK", _tableBindingInfo.PrimaryField.GetValue(entity,null));

                command.CommandText = string.Format("UPDATE [{0}]\r\n{1}\r\nWHERE {2} = @PK",
                    _tableBindingInfo.TableName,
                    _GenerateSET(fieldNewValues, command),
                    _tableBindingInfo.PrimaryFieldDbName);

                return command.ExecuteNonQuery();
            }
        }

        public int UpdateMany(IDictionary<TFieldKey, object> values)
        {
            using (var command = _CreateCommand())
            {
                command.CommandText = string.Format("UPDATE [{0}]\r\n{1}\r\n{2}",
                    _tableBindingInfo.TableName,
                    _GenerateSET(values, command),
                    _GenerateWHERE(command));

                return command.ExecuteNonQuery();
            }
        }

        public void Insert(TEntity entity)
        {
            using (var command = _CreateCommand())
            {
                // INSERT INTO table_name (column1,column2,column3,...)
                // VALUES (@VALcolumn1,@VALcolumn2,@VALcolumn3,...);

                List<string> columns = new List<string>();

                foreach (var binding in _tableBindingInfo.Bindings)
                {
                    object fieldValue = _tableBindingInfo.Fields[binding.Value.FieldKey].GetValue(entity,null);

                    string tableFieldName = binding.Value.TableFieldName;

                    if (fieldValue == null ||
                        (_tableBindingInfo.IsIdentity && binding.Key.Equals(_tableBindingInfo.PrimaryFieldKey)))
                    {
                        continue;
                    }

                    columns.Add(tableFieldName);

                    _AddParameterToCommand(command, tableFieldName, fieldValue);
                }

                command.CommandText = string.Format("INSERT INTO [{0}]\r\n({1})\r\nVALUES ({2})",
                    _tableBindingInfo.TableName,
                    string.Join(",", columns.ToArray()),
                    string.Join(",", columns.Select(col => "@" + col).ToArray()));

                    command.ExecuteNonQuery();

                if (_tableBindingInfo.IsIdentity)
                {
                    command.CommandText = string.Format("SELECT @@IDENTITY");
                    _tableBindingInfo.PrimaryField.SetValue(entity, Convert.ToInt32(command.ExecuteScalar()),null);
                }
            }
        }

        ////////////////// Private methods:

        private TEntity _SelectSingleRecord(bool defaultAllowed)
        {
            using (var command = _CreateCommand())
            {
                command.CommandText = string.Format("SELECT TOP(2) * FROM [{0}]\r\n{1}",
                    _tableBindingInfo.TableName,
                    _GenerateWHERE(command));

                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        if (defaultAllowed) return default(TEntity);

                        throw new Exception("Requested record was not found in the database.");
                    }

                    var entity = _ExtractEntityFromReader(reader);

                    if (reader.Read())
                        throw new Exception("More than one records were found, when the single unique record was requested.");

                    return entity;

                }
            }
        }

        private string _GenerateSET(IDictionary<TFieldKey, object> values, DbCommand command)
        {
            List<string> rowUpdateValues = new List<string>();
            foreach (var pair in values)
            {
                var currentBinding = _tableBindingInfo.Bindings[pair.Key];
                if (currentBinding.ClassFieldName == _tableBindingInfo.PrimaryField.Name)
                {
                    throw new Exception(string.Format("Updating Primary Key ({0}) is not allowed.", currentBinding.ClassFieldName));
                }

                var val = pair.Value;
                if (val.GetType().IsEnum)
                {
                    val = (int)val;
                }
                rowUpdateValues.Add(string.Format("{0} = @{0}", currentBinding.TableFieldName));
                _AddParameterToCommand(command, currentBinding.TableFieldName, val);
            }
            return string.Format("SET {0}", string.Join(",", rowUpdateValues.ToArray()));
        }

        private DbCommand _CreateCommand()
        {
            return _commandProvider.CreateDbCommand();
        }

        private string _GenerateORDERBY()
        {
            if (_orderingFields == null || _orderingFields.Count == 0)
                return "";

            List<string> orders = new List<string>();
            foreach (var orderingField in _orderingFields)
            {
                orders.Add(string.Format("{0} {1}", _tableBindingInfo.Bindings[orderingField.Key].TableFieldName, orderingField.Value ? "DESC" : "ASC"));
            }

            return string.Format("ORDER BY {0}", string.Join(", ", orders.ToArray()));
        }

        private string _GenerateWHERE(DbCommand command)
        {
            string alls = _GenerateConditions(command, true);
            string anys = _GenerateConditions(command, false);

            if (alls != null && anys != null)
                return string.Format("WHERE ({0}) AND ({1}) ", alls, anys);

            if (alls != null || anys != null)
                return string.Format("WHERE {0} ", alls ?? anys);

            return "";
        }

        private int whereConditionCount = 0;
        private string _GenerateConditions(DbCommand command, bool all)
        {
            var queryFilters = all ? _queryFiltersAll : _queryFiltersAny;
            if (queryFilters == null || queryFilters.Count() == 0)
            {
                return null;
            }

            List<string> conditions = new List<string>(queryFilters.Count());
            foreach (var queryFilter in queryFilters)
            {
                var tableFieldName = _tableBindingInfo.Bindings[queryFilter.Field].TableFieldName;
                if (queryFilter.Operator == QueryFilterOperator.Contains)
                {
                    _AddParameterToCommand(command, tableFieldName + whereConditionCount.ToString(), "%" + queryFilter.Value.ToString() + "%");
                    conditions.Add(string.Format("{0} LIKE @{0}{1}", tableFieldName, whereConditionCount));
                }
                else
                {
                    _AddParameterToCommand(command, tableFieldName + whereConditionCount.ToString(), queryFilter.Value);
                    conditions.Add(string.Format(" {0} {1} @{0}{2} ",
                        tableFieldName,
                        _queryFilterOperatorSigns[queryFilter.Operator], whereConditionCount));
                }

                whereConditionCount++;
            }
            return string.Join(all ? " AND " : " OR ", conditions.ToArray());
        }

        private void _AddParameterToCommand(DbCommand command, string parameterName, object value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = parameterName;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }

        private TEntity _ExtractEntityFromReader(DbDataReader reader)
        {
            var entity = new TEntity();
            foreach (var binding in _tableBindingInfo.Bindings)
            {
                var value = reader[binding.Value.TableFieldName];
                if (value is DBNull)
                    continue;
                var field = _tableBindingInfo.Fields[binding.Value.FieldKey];
                field.SetValue(entity, GetValueByFieldInfo(field, value),null);
            }
            return entity;
        }

        /// <summary>
        /// Solves the problem of Nullable enums
        /// </summary>
        /// <param name="fieldInfo"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private object GetValueByFieldInfo(PropertyInfo fieldInfo, object value)
        {
            if (value is string)
            {
                return ((string)(value)).Trim();
            }
            var propType = fieldInfo.PropertyType;


            if (propType.IsGenericType &&
                  propType.GetGenericTypeDefinition() ==
                  typeof(Nullable<>))
            {
                Type[] typeCol = propType.GetGenericArguments();
                Type nullableType;
                if (typeCol.Length > 0)
                {
                    nullableType = typeCol[0];
                    if (nullableType.BaseType == typeof(Enum))
                    {
                        return Enum.Parse(nullableType, value.ToString(), false);
                    }
                }
            }
            return value;
        }
    }
}
