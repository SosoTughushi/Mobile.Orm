using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Troyx.Mobile.Orm
{
    public enum QueryFilterOperator
    {
        Equals,
        GreaterThen,
        GreaterThenOrEqualTo,
        LessThen,
        LessThenOrEqualTo,
        Contains
    }

    public class QueryFilter<TFieldKey>
    {
        public TFieldKey Field { get; private set; }
        public object Value { get; private set; }
        public QueryFilterOperator Operator { get; private set; }

        public QueryFilter(TFieldKey field, object filterValue, QueryFilterOperator filterOperator)
        {
            Field = field;
            Value = filterValue;
            Operator = filterOperator;
        }
    }

    public class TableBindingInfo<TFieldKey>
    {
        public Type Type;
        public string TableName;
        public Dictionary<TFieldKey, PropertyInfo> Fields;
        public Dictionary<TFieldKey, TableFieldDescriptor<TFieldKey>> Bindings;
        public PropertyInfo PrimaryField;
        public string PrimaryFieldDbName;
        public TFieldKey PrimaryFieldKey;
        public bool IsIdentity;
    }

    public class TableFieldDescriptor<TFieldKey>
    {
        public TFieldKey FieldKey { get; private set; }
        public string ClassFieldName { get; private set; }
        public string TableFieldName { get; private set; }

        public TableFieldDescriptor(TFieldKey fieldKey, string classFieldName, string tableFieldName)
        {
            if (fieldKey == null) throw new ArgumentNullException("fieldKey");
            if (classFieldName == null) throw new ArgumentNullException("classFieldName");

            FieldKey = fieldKey;
            ClassFieldName = classFieldName;
            TableFieldName = tableFieldName ?? classFieldName;
        }

        public TableFieldDescriptor(TFieldKey fieldKey, string classFieldName)
            : this(fieldKey, classFieldName, null)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class PrimaryKey : Attribute
    {
        public bool IsIdentity { get; private set; }

        public PrimaryKey(bool isIdentity)
        {
            IsIdentity = isIdentity;
        }
    }
}
