using LambdifySQL.Resolver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace LambdifySQL.Core
{
    /// <summary>
    /// Represents a SQL query component with its SQL text and parameters
    /// </summary>
    public class SqlComponent
    {
        public string Sql { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();

        public SqlComponent() { }

        public SqlComponent(string sql, Dictionary<string, object> parameters = null)
        {
            Sql = sql;
            Parameters = parameters ?? new Dictionary<string, object>();
        }

        /// <summary>
        /// Combines two SQL components
        /// </summary>
        public static SqlComponent Combine(SqlComponent left, string separator, SqlComponent right)
        {
            var combinedParams = new Dictionary<string, object>(left.Parameters);
            foreach (var param in right.Parameters)
            {
                combinedParams[param.Key] = param.Value;
            }

            return new SqlComponent
            {
                Sql = $"{left.Sql}{separator}{right.Sql}",
                Parameters = combinedParams
            };
        }

        /// <summary>
        /// Wraps the SQL in parentheses
        /// </summary>
        public SqlComponent Wrap()
        {
            return new SqlComponent($"({Sql})", Parameters);
        }
    }

    /// <summary>
    /// Configuration for SQL generation
    /// </summary>
    public class SqlDialectConfig
    {
        public string ParameterPrefix { get; set; } = "@";
        public string IdentifierQuote { get; set; } = "[";
        public string IdentifierQuoteEnd { get; set; } = "]";
        public bool UseLimit { get; set; } = false; // true for MySQL/PostgreSQL, false for SQL Server
        public bool UseTop { get; set; } = true; // true for SQL Server, false for MySQL/PostgreSQL
        public string LimitKeyword { get; set; } = "LIMIT";
        public string OffsetKeyword { get; set; } = "OFFSET";
        public string TopKeyword { get; set; } = "TOP";

        /// <summary>
        /// SQL Server dialect
        /// </summary>
        public static SqlDialectConfig SqlServer => new()
        {
            ParameterPrefix = "@",
            IdentifierQuote = "[",
            IdentifierQuoteEnd = "]",
            UseLimit = false,
            UseTop = true,
            TopKeyword = "TOP"
        };

        /// <summary>
        /// MySQL dialect
        /// </summary>
        public static SqlDialectConfig MySql => new()
        {
            ParameterPrefix = "@",
            IdentifierQuote = "`",
            IdentifierQuoteEnd = "`",
            UseLimit = true,
            UseTop = false,
            LimitKeyword = "LIMIT",
            OffsetKeyword = "OFFSET"
        };

        /// <summary>
        /// PostgreSQL dialect
        /// </summary>
        public static SqlDialectConfig PostgreSql => new()
        {
            ParameterPrefix = "@",
            IdentifierQuote = "\"",
            IdentifierQuoteEnd = "\"",
            UseLimit = true,
            UseTop = false,
            LimitKeyword = "LIMIT",
            OffsetKeyword = "OFFSET"
        };
    }

    /// <summary>
    /// Context for expression to SQL conversion
    /// </summary>
    public class ExpressionContext
    {
        public Dictionary<string, object> Parameters { get; } = new();
        public Dictionary<Type, string> TableAliases { get; } = new();
        public SqlDialectConfig Dialect { get; set; } = SqlDialectConfig.SqlServer;
        public int ParameterCounter { get; set; } = 0;

        /// <summary>
        /// Adds a parameter and returns the parameter name
        /// </summary>
        public string AddParameter(object value)
        {
            var paramName = $"p{ParameterCounter++}";
            Parameters[paramName] = value;
            return $"{Dialect.ParameterPrefix}{paramName}";
        }

        /// <summary>
        /// Gets or creates an alias for a type
        /// </summary>
        public string GetTableAlias(Type type)
        {
            if (!TableAliases.ContainsKey(type))
            {
                var tableName = GetTableName(type);
                var alias = tableName.ToLower();
                
                // Ensure unique alias
                if (TableAliases.ContainsValue(alias))
                {
                    var counter = 1;
                    var baseAlias = alias;
                    while (TableAliases.ContainsValue(alias))
                    {
                        alias = $"{baseAlias}{counter++}";
                    }
                }
                
                TableAliases[type] = alias;
            }
            
            return TableAliases[type];
        }

        /// <summary>
        /// Gets the table name for a type
        /// </summary>
        public string GetTableName(Type type)
        {
            var tableAttr = type.GetCustomAttribute<TableNameAttribute>();
            return tableAttr?.tableName ?? type.Name;
        }

        /// <summary>
        /// Gets the column name for a property
        /// </summary>
        public string GetColumnName(Type type, string propertyName)
        {
            var property = type.GetProperty(propertyName);
            if (property != null)
            {
                var columnAttr = property.GetCustomAttribute<ColumnAttribute>();
                if (columnAttr != null)
                {
                    return columnAttr.ColumnName;
                }
            }
            return propertyName;
        }

        /// <summary>
        /// Quotes an identifier
        /// </summary>
        public string QuoteIdentifier(string identifier)
        {
            return $"{Dialect.IdentifierQuote}{identifier}{Dialect.IdentifierQuoteEnd}";
        }
    }

    /// <summary>
    /// Result of expression to SQL conversion
    /// </summary>
    public class ExpressionResult
    {
        public string Sql { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();

        public ExpressionResult() { }

        public ExpressionResult(string sql, Dictionary<string, object> parameters = null)
        {
            Sql = sql;
            Parameters = parameters ?? new Dictionary<string, object>();
        }

        /// <summary>
        /// Combines two expression results
        /// </summary>
        public static ExpressionResult Combine(ExpressionResult left, string separator, ExpressionResult right)
        {
            var combinedParams = new Dictionary<string, object>(left.Parameters);
            foreach (var param in right.Parameters)
            {
                combinedParams[param.Key] = param.Value;
            }

            return new ExpressionResult
            {
                Sql = $"{left.Sql}{separator}{right.Sql}",
                Parameters = combinedParams
            };
        }
    }
}
