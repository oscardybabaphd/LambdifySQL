using System.Collections.Generic;

namespace LambdifySQL.Core
{
    /// <summary>
    /// Represents the result of a query build operation
    /// </summary>
    public class QueryResult
    {
        /// <summary>
        /// The generated SQL query
        /// </summary>
        public string Sql { get; set; } = string.Empty;

        /// <summary>
        /// The parameters for the query
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Creates a new query result
        /// </summary>
        public QueryResult()
        {
        }

        /// <summary>
        /// Creates a new query result with SQL and parameters
        /// </summary>
        /// <param name="sql">The SQL query</param>
        /// <param name="parameters">The query parameters</param>
        public QueryResult(string sql, Dictionary<string, object> parameters)
        {
            Sql = sql;
            Parameters = parameters ?? new Dictionary<string, object>();
        }
    }
}
