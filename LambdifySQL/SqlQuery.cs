using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LambdifySQL.Builders;
using LambdifySQL.Core;
using LambdifySQL.Advanced;

namespace LambdifySQL
{
    /// <summary>
    /// Main entry point for the fluent SQL builder
    /// </summary>
    public static class SqlQuery
    {
        /// <summary>
        /// Creates a SELECT query builder for the specified entity type
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="dialect">The SQL dialect to use (optional)</param>
        /// <returns>A SELECT query builder</returns>
        public static ISelectQueryBuilder<T> Select<T>(SqlDialectConfig dialect = null)
        {
            var context = new ExpressionContext { Dialect = dialect ?? SqlDialectConfig.SqlServer };
            return new SelectQueryBuilder<T>(context);
        }

        /// <summary>
        /// Creates a SELECT query builder with JOIN capabilities
        /// </summary>
        /// <typeparam name="T">The primary entity type</typeparam>
        /// <param name="dialect">The SQL dialect to use (optional)</param>
        /// <returns>A JOIN query builder</returns>
        public static IJoinQueryBuilder<T> SelectWithJoins<T>(SqlDialectConfig dialect = null)
        {
            var context = new ExpressionContext { Dialect = dialect ?? SqlDialectConfig.SqlServer };
            return new SelectQueryBuilder<T>(context);
        }

        /// <summary>
        /// Creates an UPDATE query builder for the specified entity type
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="dialect">The SQL dialect to use (optional)</param>
        /// <returns>An UPDATE query builder</returns>
        public static IUpdateQueryBuilder<T> Update<T>(SqlDialectConfig dialect = null)
        {
            var context = new ExpressionContext { Dialect = dialect ?? SqlDialectConfig.SqlServer };
            return new UpdateQueryBuilder<T>(context);
        }

        /// <summary>
        /// Creates an INSERT query builder for the specified entity type
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="dialect">The SQL dialect to use (optional)</param>
        /// <returns>An INSERT query builder</returns>
        public static IInsertQueryBuilder<T> Insert<T>(SqlDialectConfig dialect = null)
        {
            var context = new ExpressionContext { Dialect = dialect ?? SqlDialectConfig.SqlServer };
            return new InsertQueryBuilder<T>(context);
        }

        /// <summary>
        /// Creates a bulk INSERT query builder for the specified entity type
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="dialect">The SQL dialect to use (optional)</param>
        /// <returns>A bulk INSERT query builder</returns>
        public static BulkInsertQueryBuilder<T> BulkInsert<T>(SqlDialectConfig dialect = null)
        {
            var context = new ExpressionContext { Dialect = dialect ?? SqlDialectConfig.SqlServer };
            return new BulkInsertQueryBuilder<T>(context);
        }

        /// <summary>
        /// Creates a DELETE query builder for the specified entity type
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="dialect">The SQL dialect to use (optional)</param>
        /// <returns>A DELETE query builder</returns>
        public static IDeleteQueryBuilder<T> Delete<T>(SqlDialectConfig dialect = null)
        {
            var context = new ExpressionContext { Dialect = dialect ?? SqlDialectConfig.SqlServer };
            return new DeleteQueryBuilder<T>(context);
        }

        /// <summary>
        /// Creates an advanced query builder with CTE and UNION support
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="dialect">The SQL dialect to use (optional)</param>
        /// <returns>An advanced query builder</returns>
        public static AdvancedQueryBuilder<T> Advanced<T>(SqlDialectConfig dialect = null)
        {
            var context = new ExpressionContext { Dialect = dialect ?? SqlDialectConfig.SqlServer };
            return new AdvancedQueryBuilder<T>(context);
        }

        /// <summary>
        /// Creates an aggregate query builder for complex aggregations
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="dialect">The SQL dialect to use (optional)</param>
        /// <returns>An aggregate query builder</returns>
        public static AggregateQueryBuilder<T> Aggregate<T>(SqlDialectConfig dialect = null)
        {
            var context = new ExpressionContext { Dialect = dialect ?? SqlDialectConfig.SqlServer };
            return new AggregateQueryBuilder<T>(context);
        }

        /// <summary>
        /// Creates a window function query builder
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="dialect">The SQL dialect to use (optional)</param>
        /// <returns>A window function query builder</returns>
        public static WindowFunctionBuilder<T> WindowFunction<T>(SqlDialectConfig dialect = null)
        {
            var context = new ExpressionContext { Dialect = dialect ?? SqlDialectConfig.SqlServer };
            return new WindowFunctionBuilder<T>(context);
        }

        /// <summary>
        /// Creates a raw SQL query with parameters
        /// </summary>
        /// <param name="sql">The raw SQL query</param>
        /// <param name="parameters">The parameters for the query</param>
        /// <returns>A SQL component representing the raw query</returns>
        public static SqlComponent Raw(string sql, object parameters = null)
        {
            var sqlComponent = new SqlComponent(sql);
            
            if (parameters != null)
            {
                var properties = parameters.GetType().GetProperties();
                foreach (var property in properties)
                {
                    var value = property.GetValue(parameters);
                    sqlComponent.Parameters[property.Name] = value;
                }
            }
            
            return sqlComponent;
        }

        /// <summary>
        /// Creates a stored procedure call
        /// </summary>
        /// <param name="procedureName">The name of the stored procedure</param>
        /// <param name="parameters">The parameters for the procedure</param>
        /// <returns>A SQL component representing the procedure call</returns>
        public static SqlComponent StoredProcedure(string procedureName, object parameters = null)
        {
            var sql = $"EXEC {procedureName}";
            var sqlComponent = new SqlComponent();
            
            if (parameters != null)
            {
                var paramList = new List<string>();
                var properties = parameters.GetType().GetProperties();
                
                foreach (var property in properties)
                {
                    var value = property.GetValue(parameters);
                    sqlComponent.Parameters[property.Name] = value;
                    paramList.Add($"@{property.Name}");
                }
                
                if (paramList.Any())
                {
                    sql += " " + string.Join(", ", paramList);
                }
            }
            
            sqlComponent.Sql = sql;
            return sqlComponent;
        }
    }

    /// <summary>
    /// Extension methods for fluent query building
    /// </summary>
    public static class QueryExtensions
    {
        /// <summary>
        /// Executes the query and returns the SQL and parameters
        /// </summary>
        /// <param name="builder">The query builder</param>
        /// <returns>A tuple containing the SQL and parameters</returns>
        public static (string Sql, Dictionary<string, object> Parameters) Build(this IQueryBuilder builder)
        {
            return (builder.GetSql(), builder.GetParameters());
        }

        /// <summary>
        /// Gets the SQL string from the query builder
        /// </summary>
        /// <param name="builder">The query builder</param>
        /// <returns>The generated SQL</returns>
        public static string ToSql(this IQueryBuilder builder)
        {
            return builder.GetSql();
        }

        /// <summary>
        /// Gets the parameters from the query builder
        /// </summary>
        /// <param name="builder">The query builder</param>
        /// <returns>The query parameters</returns>
        public static Dictionary<string, object> GetParams(this IQueryBuilder builder)
        {
            return builder.GetParameters();
        }

        /// <summary>
        /// Creates a debug string with SQL and parameters
        /// </summary>
        /// <param name="builder">The query builder</param>
        /// <returns>A formatted debug string</returns>
        public static string ToDebugString(this IQueryBuilder builder)
        {
            var sql = builder.GetSql();
            var parameters = builder.GetParameters();
            
            var debug = new StringBuilder();
            debug.AppendLine("SQL:");
            debug.AppendLine(sql);
            
            if (parameters.Any())
            {
                debug.AppendLine();
                debug.AppendLine("Parameters:");
                foreach (var param in parameters)
                {
                    debug.AppendLine($"  @{param.Key} = {param.Value ?? "NULL"}");
                }
            }
            
            return debug.ToString();
        }

        /// <summary>
        /// Converts the query to a parameterized SQL string (replaces parameters with values)
        /// </summary>
        /// <param name="builder">The query builder</param>
        /// <returns>SQL with parameter values substituted</returns>
        public static string ToParameterizedSql(this IQueryBuilder builder)
        {
            var sql = builder.GetSql();
            var parameters = builder.GetParameters();
            
            foreach (var param in parameters)
            {
                var paramName = $"@{param.Key}";
                var value = param.Value;
                
                string valueString;
                if (value == null)
                {
                    valueString = "NULL";
                }
                else if (value is string || value is DateTime || value is Guid)
                {
                    valueString = $"'{value.ToString().Replace("'", "''")}'";
                }
                else if (value is bool)
                {
                    valueString = ((bool)value) ? "1" : "0";
                }
                else
                {
                    valueString = value.ToString();
                }
                
                sql = sql.Replace(paramName, valueString);
            }
            
            return sql;
        }

        /// <summary>
        /// Validates the query for common issues
        /// </summary>
        /// <param name="builder">The query builder</param>
        /// <returns>A list of validation messages</returns>
        public static List<string> Validate(this IQueryBuilder builder)
        {
            var issues = new List<string>();
            var sql = builder.GetSql();
            var parameters = builder.GetParameters();
            
            // Check for SQL injection patterns
            var suspiciousPatterns = new[] { "--", "/*", "*/", "xp_", "sp_", "DROP", "ALTER", "TRUNCATE" };
            foreach (var pattern in suspiciousPatterns)
            {
                if (sql.ToUpper().Contains(pattern.ToUpper()))
                {
                    issues.Add($"Potentially dangerous SQL pattern detected: {pattern}");
                }
            }
            
            // Check for unparameterized values
            if (sql.Contains("'") && !parameters.Any())
            {
                issues.Add("String literals detected but no parameters found. Consider using parameterized queries.");
            }
            
            // Check for missing WHERE clause in UPDATE/DELETE
            if ((sql.ToUpper().StartsWith("UPDATE") || sql.ToUpper().StartsWith("DELETE")) 
                && !sql.ToUpper().Contains("WHERE"))
            {
                issues.Add("UPDATE or DELETE statement without WHERE clause detected. This may affect all rows.");
            }
            
            return issues;
        }
    }
}
