using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using LambdifySQL.Core;
using LambdifySQL.Enums;

namespace LambdifySQL.Advanced
{
    /// <summary>
    /// Advanced query builder with additional functionality
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    public class AdvancedQueryBuilder<T> : IQueryBuilder
    {
        private readonly ExpressionContext _context;
        private readonly ExpressionToSqlConverter _converter;
        private readonly List<string> _cteDefinitions = new();
        private readonly List<string> _unionQueries = new();
        private string _mainQuery;

        public AdvancedQueryBuilder(ExpressionContext context = null)
        {
            _context = context ?? new ExpressionContext();
            _converter = new ExpressionToSqlConverter(_context);
        }

        /// <summary>
        /// Adds a Common Table Expression (CTE)
        /// </summary>
        public AdvancedQueryBuilder<T> WithCTE(string cteName, IQueryBuilder cteQuery)
        {
            var cteSQL = cteQuery.GetSql();
            var cteParams = cteQuery.GetParameters();
            
            // Merge parameters with conflict resolution
            var (mergedSql, mergedParams) = MergeQueryWithParametersV3(cteSQL, cteParams);
            
            // Add the merged parameters (which have resolved names) to our context
            foreach (var param in mergedParams)
            {
                _context.Parameters[param.Key] = param.Value;
            }
            
            _cteDefinitions.Add($"{cteName} AS ({mergedSql})");
            return this;
        }

        /// <summary>
        /// Adds a recursive CTE
        /// </summary>
        public AdvancedQueryBuilder<T> WithRecursiveCTE(string cteName, IQueryBuilder anchorQuery, IQueryBuilder recursiveQuery)
        {
            var anchorSQL = anchorQuery.GetSql();
            var recursiveSQL = recursiveQuery.GetSql();
            
            // Merge parameters for anchor query
            var (mergedAnchorSql, mergedAnchorParams) = MergeQueryWithParameters(anchorSQL, anchorQuery.GetParameters());
            foreach (var param in mergedAnchorParams)
            {
                _context.Parameters[param.Key] = param.Value;
            }
            
            // Merge parameters for recursive query
            var (mergedRecursiveSql, mergedRecursiveParams) = MergeQueryWithParameters(recursiveSQL, recursiveQuery.GetParameters());
            foreach (var param in mergedRecursiveParams)
            {
                _context.Parameters[param.Key] = param.Value;
            }
            
            var recursiveCTE = $"{cteName} AS ({mergedAnchorSql} UNION ALL {mergedRecursiveSql})";
            _cteDefinitions.Add(recursiveCTE);
            return this;
        }

        /// <summary>
        /// Sets the main query
        /// </summary>
        public AdvancedQueryBuilder<T> Query(IQueryBuilder mainQuery)
        {
            var mainSQL = mainQuery.GetSql();
            var mainParams = mainQuery.GetParameters();
            
            // Merge parameters with conflict resolution
            var (mergedSql, mergedParams) = MergeQueryWithParametersV3(mainSQL, mainParams);
            
            _mainQuery = mergedSql;
            
            // Add the merged parameters (which have resolved names) to our context
            foreach (var param in mergedParams)
            {
                _context.Parameters[param.Key] = param.Value;
            }
            
            return this;
        }

        /// <summary>
        /// Adds a UNION query
        /// </summary>
        public AdvancedQueryBuilder<T> Union(IQueryBuilder unionQuery, bool unionAll = false)
        {
            var unionSQL = unionQuery.GetSql();
            var unionType = unionAll ? "UNION ALL" : "UNION";
            
            // Merge parameters with conflict resolution
            var (mergedSql, mergedParams) = MergeQueryWithParameters(unionSQL, unionQuery.GetParameters());
            
            foreach (var param in mergedParams)
            {
                _context.Parameters[param.Key] = param.Value;
            }
            
            _unionQueries.Add($"{unionType} {mergedSql}");
            return this;
        }

        /// <summary>
        /// Gets the generated SQL query
        /// </summary>
        public string GetSql()
        {
            var sql = new StringBuilder();

            // CTEs
            if (_cteDefinitions.Any())
            {
                sql.Append("WITH ");
                sql.Append(string.Join(", ", _cteDefinitions));
                sql.AppendLine();
            }

            // Main query
            if (!string.IsNullOrEmpty(_mainQuery))
            {
                sql.Append(_mainQuery);
            }

            // Union queries
            foreach (var unionQuery in _unionQueries)
            {
                sql.AppendLine();
                sql.Append(unionQuery);
            }

            return sql.ToString();
        }

        /// <summary>
        /// Gets the parameters for the query
        /// </summary>
        public Dictionary<string, object> GetParameters()
        {
            return new Dictionary<string, object>(_context.Parameters);
        }

        /// <summary>
        /// Merges a query's SQL and parameters, resolving parameter name conflicts
        /// </summary>
        private (string sql, Dictionary<string, object> parameters) MergeQueryWithParameters(string sql, Dictionary<string, object> parameters)
        {
            var mergedParams = new Dictionary<string, object>();
            var updatedSql = sql;
            
            foreach (var param in parameters)
            {
                var originalParamName = param.Key;
                var newParamName = originalParamName;
                
                // Check if parameter name already exists in our context
                if (_context.Parameters.ContainsKey(originalParamName))
                {
                    // Generate a new unique parameter name
                    newParamName = $"p{_context.ParameterCounter++}";
                    
                    // Replace the parameter name in the SQL using word boundaries to avoid partial matches
                    var dialectPrefix = _context.Dialect.ParameterPrefix;
                    var oldParam = $"{dialectPrefix}{originalParamName}";
                    var newParam = $"{dialectPrefix}{newParamName}";
                    
                    // Use regex to ensure we only replace whole parameter names
                    updatedSql = System.Text.RegularExpressions.Regex.Replace(
                        updatedSql, 
                        $@"\{dialectPrefix}{System.Text.RegularExpressions.Regex.Escape(originalParamName)}\b", 
                        newParam);
                }
                
                mergedParams[newParamName] = param.Value;
            }
            
            return (updatedSql, mergedParams);
        }

        /// <summary>
        /// Merges a query's SQL and parameters with better conflict resolution
        /// </summary>
        private (string sql, Dictionary<string, object> parameters) MergeQueryWithParametersV2(string sql, Dictionary<string, object> parameters)
        {
            var mergedParams = new Dictionary<string, object>();
            var updatedSql = sql;
            
            // Create a mapping of old parameter names to new parameter names
            var parameterNameMapping = new Dictionary<string, string>();
            
            foreach (var param in parameters)
            {
                var originalParamName = param.Key;
                var newParamName = originalParamName;
                
                // Check if parameter name already exists in our context
                if (_context.Parameters.ContainsKey(originalParamName))
                {
                    // Generate a new unique parameter name
                    newParamName = $"p{_context.ParameterCounter++}";
                    parameterNameMapping[originalParamName] = newParamName;
                    
                    //Console.WriteLine($"DEBUG: Parameter conflict detected - {originalParamName} -> {newParamName}");
                }
                else
                {
                    ///Console.WriteLine($"DEBUG: No conflict for parameter - {originalParamName}");
                }
                
                mergedParams[newParamName] = param.Value;
            }
            
            // Replace all parameter names in the SQL at once
            foreach (var mapping in parameterNameMapping)
            {
                var dialectPrefix = _context.Dialect.ParameterPrefix;
                var oldParam = $"{dialectPrefix}{mapping.Key}";
                var newParam = $"{dialectPrefix}{mapping.Value}";
                //Console.WriteLine($"DEBUG: Replacing {oldParam} with {newParam} in SQL");
                updatedSql = updatedSql.Replace(oldParam, newParam);
            }
            
            //Console.WriteLine($"DEBUG: Final SQL: {updatedSql}");
            //Console.WriteLine($"DEBUG: Final Parameters: {string.Join(", ", mergedParams.Select(p => $"{p.Key}={p.Value}"))}");
            
            return (updatedSql, mergedParams);
        }

        /// <summary>
        /// Test method to debug parameter counter issues
        /// </summary>
        private (string sql, Dictionary<string, object> parameters) MergeQueryWithParametersV3(string sql, Dictionary<string, object> parameters)
        {
            var mergedParams = new Dictionary<string, object>();
            var updatedSql = sql;
            
            //Console.WriteLine($"DEBUG: Starting merge with ParameterCounter = {_context.ParameterCounter}");
            //Console.WriteLine($"DEBUG: Current context parameters: {string.Join(", ", _context.Parameters.Select(p => $"{p.Key}={p.Value}"))}");
            //Console.WriteLine($"DEBUG: Incoming parameters: {string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}"))}");
            
            // Create a mapping of old parameter names to new parameter names
            var parameterNameMapping = new Dictionary<string, string>();
            
            foreach (var param in parameters)
            {
                var originalParamName = param.Key;
                var newParamName = originalParamName;
                
                // Check if parameter name already exists in our context
                if (_context.Parameters.ContainsKey(originalParamName))
                {
                    // Generate a new unique parameter name - use ++counter for pre-increment
                    newParamName = $"p{++_context.ParameterCounter}";
                    parameterNameMapping[originalParamName] = newParamName;
                    
                    //Console.WriteLine($"DEBUG: Parameter conflict detected - {originalParamName} -> {newParamName} (counter now {_context.ParameterCounter})");
                }
                else
                {
                   // Console.WriteLine($"DEBUG: No conflict for parameter - {originalParamName}");
                }
                
                mergedParams[newParamName] = param.Value;
            }
            
            // Replace all parameter names in the SQL at once
            foreach (var mapping in parameterNameMapping)
            {
                var dialectPrefix = _context.Dialect.ParameterPrefix;
                var oldParam = $"{dialectPrefix}{mapping.Key}";
                var newParam = $"{dialectPrefix}{mapping.Value}";
              //  Console.WriteLine($"DEBUG: Replacing {oldParam} with {newParam} in SQL");
                updatedSql = updatedSql.Replace(oldParam, newParam);
            }
            
            //Console.WriteLine($"DEBUG: Final SQL: {updatedSql}");
            //Console.WriteLine($"DEBUG: Final Parameters: {string.Join(", ", mergedParams.Select(p => $"{p.Key}={p.Value}"))}");
            
            return (updatedSql, mergedParams);
        }

        /// <summary>
        /// Resets the builder to initial state
        /// </summary>
        public void Reset()
        {
            _cteDefinitions.Clear();
            _unionQueries.Clear();
            _context.Parameters.Clear();
            _context.ParameterCounter = 0;
            _mainQuery = null;
        }
    }

    /// <summary>
    /// Aggregate query builder for complex aggregations
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    public class AggregateQueryBuilder<T> : IQueryBuilder
    {
        private readonly ExpressionContext _context;
        private readonly ExpressionToSqlConverter _converter;
        private readonly List<string> _selectColumns = new();
        private readonly List<string> _groupByColumns = new();
        private readonly List<string> _whereConditions = new();
        private readonly List<string> _havingConditions = new();
        private readonly List<string> _orderByColumns = new();

        public AggregateQueryBuilder(ExpressionContext context = null)
        {
            _context = context ?? new ExpressionContext();
            _converter = new ExpressionToSqlConverter(_context);
        }

        /// <summary>
        /// Adds an aggregate function
        /// </summary>
        public AggregateQueryBuilder<T> Aggregate<TProperty>(
            AggregateFunction function, 
            Expression<Func<T, TProperty>> selector, 
            string alias = null)
        {
            var column = _converter.ConvertPropertySelector(selector);
            var functionName = function.ToString().ToUpper();
            var aggregateColumn = $"{functionName}({column})";
            
            if (!string.IsNullOrEmpty(alias))
            {
                aggregateColumn += $" AS {_context.QuoteIdentifier(alias)}";
            }
            
            _selectColumns.Add(aggregateColumn);
            return this;
        }

        /// <summary>
        /// Adds a COUNT aggregate
        /// </summary>
        public AggregateQueryBuilder<T> Count(Expression<Func<T, object>> selector = null, string alias = "Count")
        {
            if (selector != null)
            {
                return Aggregate(AggregateFunction.Count, selector, alias);
            }
            else
            {
                var countColumn = $"COUNT(*) AS {_context.QuoteIdentifier(alias)}";
                _selectColumns.Add(countColumn);
                return this;
            }
        }

        /// <summary>
        /// Adds a SUM aggregate
        /// </summary>
        public AggregateQueryBuilder<T> Sum<TProperty>(Expression<Func<T, TProperty>> selector, string alias = "Sum")
        {
            return Aggregate(AggregateFunction.Sum, selector, alias);
        }

        /// <summary>
        /// Adds an AVG aggregate
        /// </summary>
        public AggregateQueryBuilder<T> Average<TProperty>(Expression<Func<T, TProperty>> selector, string alias = "Average")
        {
            return Aggregate(AggregateFunction.Avg, selector, alias);
        }

        /// <summary>
        /// Adds a MIN aggregate
        /// </summary>
        public AggregateQueryBuilder<T> Min<TProperty>(Expression<Func<T, TProperty>> selector, string alias = "Min")
        {
            return Aggregate(AggregateFunction.Min, selector, alias);
        }

        /// <summary>
        /// Adds a MAX aggregate
        /// </summary>
        public AggregateQueryBuilder<T> Max<TProperty>(Expression<Func<T, TProperty>> selector, string alias = "Max")
        {
            return Aggregate(AggregateFunction.Max, selector, alias);
        }

        /// <summary>
        /// Adds GROUP BY clause
        /// </summary>
        public AggregateQueryBuilder<T> GroupBy<TProperty>(Expression<Func<T, TProperty>> selector)
        {
            var column = _converter.ConvertPropertySelector(selector);
            _groupByColumns.Add(column);
            _selectColumns.Add(column); // Also add to SELECT
            return this;
        }

        /// <summary>
        /// Adds WHERE clause
        /// </summary>
        public AggregateQueryBuilder<T> Where(Expression<Func<T, bool>> predicate)
        {
            var result = _converter.Convert(predicate);
            foreach (var param in result.Parameters)
            {
                _context.Parameters[param.Key] = param.Value;
            }
            _whereConditions.Add(result.Sql);
            return this;
        }

        /// <summary>
        /// Adds HAVING clause
        /// </summary>
        public AggregateQueryBuilder<T> Having(string havingExpression)
        {
            _havingConditions.Add(havingExpression);
            return this;
        }

        /// <summary>
        /// Adds ORDER BY clause
        /// </summary>
        public AggregateQueryBuilder<T> OrderBy(string column, bool ascending = true)
        {
            var direction = ascending ? "ASC" : "DESC";
            _orderByColumns.Add($"{column} {direction}");
            return this;
        }

        /// <summary>
        /// Gets the generated SQL query
        /// </summary>
        public string GetSql()
        {
            var sql = new StringBuilder();
            var tableName = _context.GetTableName(typeof(T));
            var tableAlias = _context.GetTableAlias(typeof(T));

            // SELECT clause
            sql.Append("SELECT ");
            if (_selectColumns.Any())
            {
                sql.Append(string.Join(", ", _selectColumns));
            }
            else
            {
                sql.Append("*");
            }

            // FROM clause
            sql.AppendLine();
            sql.Append($"FROM {_context.QuoteIdentifier(tableName)} AS {tableAlias}");

            // WHERE clause
            if (_whereConditions.Any())
            {
                sql.AppendLine();
                sql.Append($"WHERE {string.Join(" AND ", _whereConditions)}");
            }

            // GROUP BY clause
            if (_groupByColumns.Any())
            {
                sql.AppendLine();
                sql.Append($"GROUP BY {string.Join(", ", _groupByColumns)}");
            }

            // HAVING clause
            if (_havingConditions.Any())
            {
                sql.AppendLine();
                sql.Append($"HAVING {string.Join(" AND ", _havingConditions)}");
            }

            // ORDER BY clause
            if (_orderByColumns.Any())
            {
                sql.AppendLine();
                sql.Append($"ORDER BY {string.Join(", ", _orderByColumns)}");
            }

            return sql.ToString();
        }

        /// <summary>
        /// Gets the parameters for the query
        /// </summary>
        public Dictionary<string, object> GetParameters()
        {
            return new Dictionary<string, object>(_context.Parameters);
        }

        /// <summary>
        /// Resets the builder to initial state
        /// </summary>
        public void Reset()
        {
            _selectColumns.Clear();
            _groupByColumns.Clear();
            _whereConditions.Clear();
            _havingConditions.Clear();
            _orderByColumns.Clear();
            _context.Parameters.Clear();
            _context.ParameterCounter = 0;
        }
    }

    /// <summary>
    /// Window function query builder
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    public class WindowFunctionBuilder<T> : IQueryBuilder
    {
        private readonly ExpressionContext _context;
        private readonly ExpressionToSqlConverter _converter;
        private readonly List<string> _selectColumns = new();
        private readonly List<string> _whereConditions = new();
        private readonly List<string> _orderByColumns = new();

        public WindowFunctionBuilder(ExpressionContext context = null)
        {
            _context = context ?? new ExpressionContext();
            _converter = new ExpressionToSqlConverter(_context);
        }

        /// <summary>
        /// Adds a ROW_NUMBER window function
        /// </summary>
        public WindowFunctionBuilder<T> RowNumber<TProperty>(
            Expression<Func<T, TProperty>> partitionBy = null,
            Expression<Func<T, object>> orderBy = null,
            string alias = "RowNumber")
        {
            var windowFunction = BuildWindowFunction("ROW_NUMBER()", partitionBy, orderBy, alias);
            _selectColumns.Add(windowFunction);
            return this;
        }

        /// <summary>
        /// Adds a RANK window function
        /// </summary>
        public WindowFunctionBuilder<T> Rank<TProperty>(
            Expression<Func<T, TProperty>> partitionBy = null,
            Expression<Func<T, object>> orderBy = null,
            string alias = "Rank")
        {
            var windowFunction = BuildWindowFunction("RANK()", partitionBy, orderBy, alias);
            _selectColumns.Add(windowFunction);
            return this;
        }

        /// <summary>
        /// Adds a DENSE_RANK window function
        /// </summary>
        public WindowFunctionBuilder<T> DenseRank<TProperty>(
            Expression<Func<T, TProperty>> partitionBy = null,
            Expression<Func<T, object>> orderBy = null,
            string alias = "DenseRank")
        {
            var windowFunction = BuildWindowFunction("DENSE_RANK()", partitionBy, orderBy, alias);
            _selectColumns.Add(windowFunction);
            return this;
        }

        /// <summary>
        /// Adds a LAG window function
        /// </summary>
        public WindowFunctionBuilder<T> Lag<TProperty>(
            Expression<Func<T, TProperty>> selector,
            int offset = 1,
            object defaultValue = null,
            Expression<Func<T, object>> partitionBy = null,
            Expression<Func<T, object>> orderBy = null,
            string alias = "LagValue")
        {
            var column = _converter.ConvertPropertySelector(selector);
            var lagFunction = $"LAG({column}, {offset}";
            
            if (defaultValue != null)
            {
                var paramName = _context.AddParameter(defaultValue);
                lagFunction += $", {paramName}";
            }
            
            lagFunction += ")";
            
            var windowFunction = BuildWindowFunction(lagFunction, partitionBy, orderBy, alias);
            _selectColumns.Add(windowFunction);
            return this;
        }

        /// <summary>
        /// Adds a LEAD window function
        /// </summary>
        public WindowFunctionBuilder<T> Lead<TProperty>(
            Expression<Func<T, TProperty>> selector,
            int offset = 1,
            object defaultValue = null,
            Expression<Func<T, object>> partitionBy = null,
            Expression<Func<T, object>> orderBy = null,
            string alias = "LeadValue")
        {
            var column = _converter.ConvertPropertySelector(selector);
            var leadFunction = $"LEAD({column}, {offset}";
            
            if (defaultValue != null)
            {
                var paramName = _context.AddParameter(defaultValue);
                leadFunction += $", {paramName}";
            }
            
            leadFunction += ")";
            
            var windowFunction = BuildWindowFunction(leadFunction, partitionBy, orderBy, alias);
            _selectColumns.Add(windowFunction);
            return this;
        }

        /// <summary>
        /// Adds a regular column to the SELECT
        /// </summary>
        public WindowFunctionBuilder<T> Select<TProperty>(Expression<Func<T, TProperty>> selector)
        {
            var column = _converter.ConvertPropertySelector(selector);
            _selectColumns.Add(column);
            return this;
        }

        /// <summary>
        /// Adds WHERE clause
        /// </summary>
        public WindowFunctionBuilder<T> Where(Expression<Func<T, bool>> predicate)
        {
            var result = _converter.Convert(predicate);
            foreach (var param in result.Parameters)
            {
                _context.Parameters[param.Key] = param.Value;
            }
            _whereConditions.Add(result.Sql);
            return this;
        }

        /// <summary>
        /// Builds a window function SQL
        /// </summary>
        private string BuildWindowFunction<TPartition, TOrder>(
            string function,
            Expression<Func<T, TPartition>> partitionBy,
            Expression<Func<T, TOrder>> orderBy,
            string alias)
        {
            var windowClause = " OVER (";

            if (partitionBy != null)
            {
                var partitionColumn = _converter.ConvertPropertySelector(partitionBy);
                windowClause += $"PARTITION BY {partitionColumn}";
            }

            if (orderBy != null)
            {
                var orderColumn = _converter.ConvertPropertySelector(orderBy);
                if (partitionBy != null) windowClause += " ";
                windowClause += $"ORDER BY {orderColumn}";
            }

            windowClause += ")";

            return $"{function}{windowClause} AS {_context.QuoteIdentifier(alias)}";
        }

        /// <summary>
        /// Gets the generated SQL query
        /// </summary>
        public string GetSql()
        {
            var sql = new StringBuilder();
            var tableName = _context.GetTableName(typeof(T));
            var tableAlias = _context.GetTableAlias(typeof(T));

            // SELECT clause
            sql.Append("SELECT ");
            if (_selectColumns.Any())
            {
                sql.Append(string.Join(", ", _selectColumns));
            }
            else
            {
                sql.Append($"{tableAlias}.*");
            }

            // FROM clause
            sql.AppendLine();
            sql.Append($"FROM {_context.QuoteIdentifier(tableName)} AS {tableAlias}");

            // WHERE clause
            if (_whereConditions.Any())
            {
                sql.AppendLine();
                sql.Append($"WHERE {string.Join(" AND ", _whereConditions)}");
            }

            // ORDER BY clause
            if (_orderByColumns.Any())
            {
                sql.AppendLine();
                sql.Append($"ORDER BY {string.Join(", ", _orderByColumns)}");
            }

            return sql.ToString();
        }

        /// <summary>
        /// Gets the parameters for the query
        /// </summary>
        public Dictionary<string, object> GetParameters()
        {
            return new Dictionary<string, object>(_context.Parameters);
        }
        /// <summary>
        /// Resets the builder to initial state
        /// </summary>
        public void Reset()
        {
            _selectColumns.Clear();
            _whereConditions.Clear();
            _orderByColumns.Clear();
            _context.Parameters.Clear();
            _context.ParameterCounter = 0;
        }
    }
}
