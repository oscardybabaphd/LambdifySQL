using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LambdifySQL.Resolver
{
    /// <summary>
    /// Specifies the table name and alias for an entity class
    /// </summary>
    [System.AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class TableNameAttribute : Attribute
    {
        private string _alias;

        public TableNameAttribute(string tableName, string alias = null)
        {
            this.tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
            this._alias = alias;
        }

        public string tableName { get; set; }

        public string alias
        {
            get
            {
                if (string.IsNullOrEmpty(_alias))
                {
                    return tableName.ToLower();
                }
                return _alias;
            }
            set
            {
                _alias = value;
            }
        }
    }

    /// <summary>
    /// Marks a property as a primary key
    /// </summary>
    [System.AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class PkAttribute : Attribute
    {
        public PkAttribute(string pk = null)
        {
            this.pk = pk;
        }

        public string pk { get; set; }
        public bool IsAutoIncrement { get; set; } = true;
    }

    /// <summary>
    /// Defines a relationship between entities
    /// </summary>
    [System.AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    public class RelationAttribute : Attribute
    {
        public RelationAttribute(Type type)
        {
            this.type = type?.Name ?? throw new ArgumentNullException(nameof(type));
            this.RelatedType = type;
        }

        public string type { get; set; }
        public Type RelatedType { get; set; }
        public string ForeignKey { get; set; }
        public string ReferencedKey { get; set; } = "Id";
    }

    /// <summary>
    /// Marks a property to be ignored during SQL generation
    /// </summary>
    [System.AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class IgnoreMeAttribute : Attribute
    {
        public IgnoreMeAttribute()
        {
            this.isIgnore = true;
        }

        public bool isIgnore { get; set; }
    }

    /// <summary>
    /// Specifies a custom column name for a property
    /// </summary>
    [System.AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class ColumnAttribute : Attribute
    {
        public ColumnAttribute(string columnName)
        {
            this.ColumnName = columnName ?? throw new ArgumentNullException(nameof(columnName));
        }

        public string ColumnName { get; set; }
        public bool IsNullable { get; set; } = true;
        public int MaxLength { get; set; } = -1;
        public string DataType { get; set; }
    }

    /// <summary>
    /// Marks a property as required (NOT NULL)
    /// </summary>
    [System.AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class RequiredAttribute : Attribute
    {
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Specifies the maximum length for string properties
    /// </summary>
    [System.AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class MaxLengthAttribute : Attribute
    {
        public MaxLengthAttribute(int maxLength)
        {
            this.MaxLength = maxLength;
        }

        public int MaxLength { get; set; }
    }

    /// <summary>
    /// Marks a property for database indexing
    /// </summary>
    [System.AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    public class IndexAttribute : Attribute
    {
        public IndexAttribute(string indexName = null)
        {
            this.IndexName = indexName;
        }

        public string IndexName { get; set; }
        public bool IsUnique { get; set; } = false;
        public bool IsClustered { get; set; } = false;
        public int Order { get; set; } = 0;
    }

    /// <summary>
    /// Defines a default value for a property
    /// </summary>
    [System.AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class DefaultValueAttribute : Attribute
    {
        public DefaultValueAttribute(object value)
        {
            this.Value = value;
        }

        public object Value { get; set; }
    }

    /// <summary>
    /// Marks a property as computed (read-only)
    /// </summary>
    [System.AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class ComputedAttribute : Attribute
    {
        public ComputedAttribute(string expression = null)
        {
            this.Expression = expression;
        }

        public string Expression { get; set; }
    }
}
