using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using LambdifySQL.Core;
using LambdifySQL.Enums;

namespace LambdifySQL.Builders
{
    /// <summary>
    /// Fluent SELECT query builder
    /// </summary>
    /// <typeparam name="T">The primary entity type</typeparam>
    public class SelectQueryBuilder<T> : ISelectQueryBuilder<T>, IJoinQueryBuilder<T>
    {
        protected readonly ExpressionContext _context;
        protected readonly ExpressionToSqlConverter _converter;
        protected readonly List<string> _selectColumns = new();
        protected readonly List<string> _whereConditions = new();
        protected readonly List<string> _orderByColumns = new();
        protected readonly List<string> _groupByColumns = new();
        protected readonly List<string> _havingConditions = new();
        protected readonly List<string> _joinClauses = new();
        protected int? _takeCount;
        protected string? _fromCTE;
        protected int? _skipCount;
        protected bool _distinct;
        protected bool _count;

        public SelectQueryBuilder(ExpressionContext context = null)
        {
            _context = context ?? new ExpressionContext();
            _converter = new ExpressionToSqlConverter(_context);
            
            // Register the primary table
            _context.GetTableAlias(typeof(T));
        }

        /// <summary>
        /// Adds a WHERE clause
        /// </summary>
        public ISelectQueryBuilder<T> Where(Expression<Func<T, bool>> predicate)
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
        /// Adds an OR WHERE clause
        /// </summary>
        public ISelectQueryBuilder<T> OrWhere(Expression<Func<T, bool>> predicate)
        {
            var result = _converter.Convert(predicate);
            foreach (var param in result.Parameters)
            {
                _context.Parameters[param.Key] = param.Value;
            }
            
            if (_whereConditions.Any())
            {
                var lastCondition = _whereConditions.Last();
                _whereConditions[_whereConditions.Count - 1] = $"({lastCondition}) OR ({result.Sql})";
            }
            else
            {
                _whereConditions.Add(result.Sql);
            }
            return this;
        }

        /// <summary>
        /// Adds an AND WHERE clause
        /// </summary>
        public ISelectQueryBuilder<T> AndWhere(Expression<Func<T, bool>> predicate)
        {
            return Where(predicate); // WHERE clauses are AND by default
        }

        /// <summary>
        /// Adds ORDER BY clause
        /// </summary>
        public ISelectQueryBuilder<T> OrderBy<TProperty>(Expression<Func<T, TProperty>> selector)
        {
            var column = _converter.ConvertPropertySelector(selector);
            _orderByColumns.Add($"{column} ASC");
            return this;
        }

        /// <summary>
        /// Adds ORDER BY DESC clause
        /// </summary>
        public ISelectQueryBuilder<T> OrderByDescending<TProperty>(Expression<Func<T, TProperty>> selector)
        {
            var column = _converter.ConvertPropertySelector(selector);
            _orderByColumns.Add($"{column} DESC");
            return this;
        }

        /// <summary>
        /// Adds additional ORDER BY clause
        /// </summary>
        public ISelectQueryBuilder<T> ThenBy<TProperty>(Expression<Func<T, TProperty>> selector)
        {
            var column = _converter.ConvertPropertySelector(selector);
            _orderByColumns.Add($"{column} ASC");
            return this;
        }

        /// <summary>
        /// Adds additional ORDER BY DESC clause
        /// </summary>
        public ISelectQueryBuilder<T> ThenByDescending<TProperty>(Expression<Func<T, TProperty>> selector)
        {
            var column = _converter.ConvertPropertySelector(selector);
            _orderByColumns.Add($"{column} DESC");
            return this;
        }

        /// <summary>
        /// Adds ORDER BY clause with string expression
        /// </summary>
        public ISelectQueryBuilder<T> ThenBy(string orderExpression)
        {
            _orderByColumns.Add($"{orderExpression} ASC");
            return this;
        }

        /// <summary>
        /// Adds GROUP BY clause
        /// </summary>
        public ISelectQueryBuilder<T> GroupBy<TProperty>(Expression<Func<T, TProperty>> selector)
        {
            var column = _converter.ConvertPropertySelector(selector);
            _groupByColumns.Add(column);
            return this;
        }

        /// <summary>
        /// Adds HAVING clause
        /// </summary>
        public ISelectQueryBuilder<T> Having(Expression<Func<T, bool>> predicate)
        {
            var result = _converter.Convert(predicate);
            foreach (var param in result.Parameters)
            {
                _context.Parameters[param.Key] = param.Value;
            }
            _havingConditions.Add(result.Sql);
            return this;
        }

        /// <summary>
        /// Adds LIMIT/TOP clause
        /// </summary>
        public ISelectQueryBuilder<T> Take(int count)
        {
            _takeCount = count;
            return this;
        }

        /// <summary>
        /// Selects specific columns using lambda expressions
        /// </summary>
        public ISelectQueryBuilder<T> Column<TProperty>(Expression<Func<T, TProperty>> selector)
        {
            var columnSql = _converter.ConvertPropertySelector(selector);
            _selectColumns.Add(columnSql);
            return this;
        }

        /// <summary>
        /// Selects specific columns using lambda expressions with alias
        /// </summary>
        public ISelectQueryBuilder<T> Column<TProperty>(Expression<Func<T, TProperty>> selector, string alias)
        {
            var columnSql = _converter.ConvertPropertySelector(selector);
            _selectColumns.Add($"{columnSql} AS [{alias}]");
            return this;
        }

        /// <summary>
        /// Adds OFFSET clause
        /// </summary>
        public ISelectQueryBuilder<T> Skip(int count)
        {
            _skipCount = count;
            return this;
        }

        /// <summary>
        /// Selects specific columns
        /// </summary>
        public ISelectQueryBuilder<T> Select<TResult>(Expression<Func<T, TResult>> selector)
        {
            // Convert the selector to an object expression for our converter
            var objectExpression = Expression.Lambda<Func<T, object>>(
                Expression.Convert(selector.Body, typeof(object)), 
                selector.Parameters);
            
            var columns = _converter.ConvertPropertySelectors(objectExpression);
            _selectColumns.AddRange(columns);
            return this;
        }

        /// <summary>
        /// Adds COUNT aggregate
        /// </summary>
        public ISelectQueryBuilder<T> Count()
        {
            _count = true;
            return this;
        }

        /// <summary>
        /// Adds DISTINCT clause
        /// </summary>
        public ISelectQueryBuilder<T> Distinct()
        {
            _distinct = true;
            return this;
        }

        /// <summary>
        /// Adds INNER JOIN
        /// </summary>
        public IJoinQueryBuilder<T> InnerJoin<TJoin>(Expression<Func<T, TJoin, bool>> joinCondition)
        {
            return AddJoin<TJoin>("INNER JOIN", joinCondition);
        }

        /// <summary>
        /// Adds LEFT JOIN
        /// </summary>
        public IJoinQueryBuilder<T> LeftJoin<TJoin>(Expression<Func<T, TJoin, bool>> joinCondition)
        {
            return AddJoin<TJoin>("LEFT JOIN", joinCondition);
        }

        /// <summary>
        /// Adds RIGHT JOIN
        /// </summary>
        public IJoinQueryBuilder<T> RightJoin<TJoin>(Expression<Func<T, TJoin, bool>> joinCondition)
        {
            return AddJoin<TJoin>("RIGHT JOIN", joinCondition);
        }

        /// <summary>
        /// Adds FULL OUTER JOIN
        /// </summary>
        public IJoinQueryBuilder<T> FullJoin<TJoin>(Expression<Func<T, TJoin, bool>> joinCondition)
        {
            return AddJoin<TJoin>("FULL OUTER JOIN", joinCondition);
        }

        /// <summary>
        /// Adds a JOIN clause
        /// </summary>
        protected IJoinQueryBuilder<T> AddJoin<TJoin>(string joinType, Expression<Func<T, TJoin, bool>> joinCondition)
        {
            // Register the join table
            var joinTableAlias = _context.GetTableAlias(typeof(TJoin));
            var joinTableName = _context.GetTableName(typeof(TJoin));
            
            // For now, we'll create a simple join condition
            // This is a simplified implementation that would need to be enhanced for production
            var conditionSql = ConvertJoinCondition(joinCondition);
            
            var joinClause = $"{joinType} {_context.QuoteIdentifier(joinTableName)} AS {joinTableAlias} ON {conditionSql}";
            _joinClauses.Add(joinClause);
            
            return this;
        }

        /// <summary>
        /// Converts a join condition expression to SQL
        /// </summary>
        private string ConvertJoinCondition<TJoin>(Expression<Func<T, TJoin, bool>> joinCondition)
        {
            // This is a simplified implementation
            // In a full implementation, this would properly parse the expression tree
            // For now, we'll return a placeholder that works for basic scenarios
            var primaryTableAlias = _context.GetTableAlias(typeof(T));
            var joinTableAlias = _context.GetTableAlias(typeof(TJoin));
            
            // This is a very basic implementation - would need enhancement for real-world use
            return $"{primaryTableAlias}.Id = {joinTableAlias}.Id";
        }

        /// <summary>
        /// Gets the generated SQL query
        /// </summary>
        public string GetSql()
        {
            var sql = new StringBuilder();
            
            // SELECT clause
            sql.Append("SELECT ");
            
            if (_distinct)
                sql.Append("DISTINCT ");
                
            if (_context.Dialect.UseTop && _takeCount.HasValue && !_skipCount.HasValue)
                sql.Append($"TOP ({_takeCount}) ");

            // Columns
            if (_count)
            {
                sql.Append("COUNT(*) AS [Count]");
            }
            else if (_selectColumns.Any())
            {
                sql.Append(string.Join(", ", _selectColumns));
            }
            else
            {
                // Select all columns from primary table
                var primaryTableAlias = _context.GetTableAlias(typeof(T));
                sql.Append($"{primaryTableAlias}.*");
            }

            // FROM clause
            var primaryTableName = _context.GetTableName(typeof(T));
            var primaryAlias = _context.GetTableAlias(typeof(T));
            sql.AppendLine();
            sql.Append($"FROM {_context.QuoteIdentifier(primaryTableName)} AS {primaryAlias}");

            // JOIN clauses
            foreach (var joinClause in _joinClauses)
            {
                sql.AppendLine();
                sql.Append(joinClause);
            }

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

            // LIMIT/OFFSET for non-SQL Server dialects
            if (_context.Dialect.UseLimit)
            {
                if (_takeCount.HasValue)
                {
                    sql.AppendLine();
                    sql.Append($"{_context.Dialect.LimitKeyword} {_takeCount}");
                }
                
                if (_skipCount.HasValue)
                {
                    sql.AppendLine();
                    sql.Append($"{_context.Dialect.OffsetKeyword} {_skipCount}");
                }
            }
            else if (_skipCount.HasValue) // SQL Server with OFFSET
            {
                if (!_orderByColumns.Any())
                {
                    // SQL Server requires ORDER BY for OFFSET
                    var primaryTableAlias = _context.GetTableAlias(typeof(T));
                    sql.AppendLine();
                    sql.Append($"ORDER BY {primaryTableAlias}.{_context.QuoteIdentifier("Id")}"); // Assume Id column
                }
                
                sql.AppendLine();
                sql.Append($"OFFSET {_skipCount} ROWS");
                
                if (_takeCount.HasValue)
                {
                    sql.Append($" FETCH NEXT {_takeCount} ROWS ONLY");
                }
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
            _groupByColumns.Clear();
            _havingConditions.Clear();
            _joinClauses.Clear();
            _context.Parameters.Clear();
            _context.ParameterCounter = 0;
            _takeCount = null;
            _skipCount = null;
            _distinct = false;
            _count = false;
        }

        /// <summary>
        /// Adds TOP clause
        /// </summary>
        public ISelectQueryBuilder<T> Top(int count)
        {
            _takeCount = count;
            return this;
        }

        /// <summary>
        /// Adds window function
        /// </summary>
        public ISelectQueryBuilder<T> SelectWindow(string windowFunction, string alias)
        {
            _selectColumns.Add($"{windowFunction} AS {alias}");
            return this;
        }

        /// <summary>
        /// Adds subquery in SELECT clause
        /// </summary>
        public ISelectQueryBuilder<T> SelectSubQuery(string alias, IQueryBuilder subQuery)
        {
            var subQuerySql = subQuery.GetSql();
            var subQueryParams = subQuery.GetParameters();
            
            // Merge parameters
            foreach (var param in subQueryParams)
            {
                _context.Parameters[param.Key] = param.Value;
            }
            
            _selectColumns.Add($"({subQuerySql}) AS {alias}");
            return this;
        }

        /// <summary>
        /// Adds WHERE IN clause with subquery
        /// </summary>
        public ISelectQueryBuilder<T> WhereIn<TProperty, TSubQuery>(Expression<Func<T, TProperty>> selector, IQueryBuilder subQuery, Expression<Func<TSubQuery, TProperty>> subQuerySelector)
        {
            var column = _converter.ConvertPropertySelector(selector);
            var subQuerySql = subQuery.GetSql();
            var subQueryParams = subQuery.GetParameters();
            
            // Merge parameters
            foreach (var param in subQueryParams)
            {
                _context.Parameters[param.Key] = param.Value;
            }
            
            // Create the subquery column selection
            var subQueryConverter = new ExpressionToSqlConverter();
            var subQueryColumn = subQueryConverter.ConvertPropertySelector(subQuerySelector);
            
            _whereConditions.Add($"{column} IN (SELECT {subQueryColumn} FROM ({subQuerySql}) subq)");
            return this;
        }

        /// <summary>
        /// Adds WHERE IN clause with collection of values
        /// </summary>
        public ISelectQueryBuilder<T> WhereIn<TProperty>(Expression<Func<T, TProperty>> selector, IEnumerable<TProperty> values)
        {
            var column = _converter.ConvertPropertySelector(selector);
            var valuesList = values?.ToList() ?? new List<TProperty>();
            
            if (!valuesList.Any())
            {
                // If no values provided, add a condition that will never match
                _whereConditions.Add("1 = 0");
                return this;
            }
            
            var parameterNames = new List<string>();
            foreach (var value in valuesList)
            {
                var paramName = _context.AddParameter(value);
                parameterNames.Add(paramName);
            }
            
            _whereConditions.Add($"{column} IN ({string.Join(", ", parameterNames)})");
            return this;
        }

        /// <summary>
        /// Adds raw WHERE clause
        /// </summary>
        public ISelectQueryBuilder<T> WhereRaw(string rawCondition)
        {
            _whereConditions.Add(rawCondition);
            return this;
        }

        /// <summary>
        /// Sets the table source from CTE
        /// </summary>
        public ISelectQueryBuilder<T> FromCTE(string cteName, string? alias = null)
        {
            // For now, we'll implement this as a simple table reference
            // In a full implementation, this would modify the FROM clause generation
            return this;
        }

        // Missing IJoinQueryBuilder interface methods
        
        /// <summary>
        /// Adds INNER JOIN with string alias for joined table
        /// </summary>
        public IJoinQueryBuilder<T> InnerJoin<TJoin>(string alias, Expression<Func<T, TJoin, bool>> joinCondition)
        {
            return AddJoinWithAlias<TJoin>("INNER JOIN", alias, joinCondition);
        }

        /// <summary>
        /// Adds LEFT JOIN with string alias for joined table
        /// </summary>
        public IJoinQueryBuilder<T> LeftJoin<TJoin>(string alias, Expression<Func<T, TJoin, bool>> joinCondition)
        {
            return AddJoinWithAlias<TJoin>("LEFT JOIN", alias, joinCondition);
        }

        /// <summary>
        /// Adds RIGHT JOIN with string alias for joined table
        /// </summary>
        public IJoinQueryBuilder<T> RightJoin<TJoin>(string alias, Expression<Func<T, TJoin, bool>> joinCondition)
        {
            return AddJoinWithAlias<TJoin>("RIGHT JOIN", alias, joinCondition);
        }

        /// <summary>
        /// Adds FULL OUTER JOIN
        /// </summary>
        public IJoinQueryBuilder<T> FullOuterJoin<TJoin>(Expression<Func<T, TJoin, bool>> joinCondition)
        {
            return AddJoin<TJoin>("FULL OUTER JOIN", joinCondition);
        }

        /// <summary>
        /// Adds FULL OUTER JOIN with string alias for joined table
        /// </summary>
        public IJoinQueryBuilder<T> FullOuterJoin<TJoin>(string alias, Expression<Func<T, TJoin, bool>> joinCondition)
        {
            return AddJoinWithAlias<TJoin>("FULL OUTER JOIN", alias, joinCondition);
        }

        /// <summary>
        /// Adds typed WHERE clause for specific entity
        /// </summary>
        public IJoinQueryBuilder<T> Where<TEntity>(Expression<Func<TEntity, bool>> predicate)
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
        /// Adds typed WHERE clause for specific entity with alias
        /// </summary>
        public IJoinQueryBuilder<T> Where<TEntity>(string alias, Expression<Func<TEntity, bool>> predicate)
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
        /// Adds typed OR WHERE clause for specific entity
        /// </summary>
        public IJoinQueryBuilder<T> OrWhere<TEntity>(Expression<Func<TEntity, bool>> predicate)
        {
            var result = _converter.Convert(predicate);
            foreach (var param in result.Parameters)
            {
                _context.Parameters[param.Key] = param.Value;
            }
            
            if (_whereConditions.Any())
            {
                var lastCondition = _whereConditions.Last();
                _whereConditions[_whereConditions.Count - 1] = $"({lastCondition}) OR ({result.Sql})";
            }
            else
            {
                _whereConditions.Add(result.Sql);
            }
            return this;
        }

        /// <summary>
        /// Adds typed OR WHERE clause for specific entity with alias
        /// </summary>
        public IJoinQueryBuilder<T> OrWhere<TEntity>(string alias, Expression<Func<TEntity, bool>> predicate)
        {
            var result = _converter.Convert(predicate);
            foreach (var param in result.Parameters)
            {
                _context.Parameters[param.Key] = param.Value;
            }
            
            if (_whereConditions.Any())
            {
                var lastCondition = _whereConditions.Last();
                _whereConditions[_whereConditions.Count - 1] = $"({lastCondition}) OR ({result.Sql})";
            }
            else
            {
                _whereConditions.Add(result.Sql);
            }
            return this;
        }

        /// <summary>
        /// Adds OR WHERE clause for base type
        /// </summary>
        IJoinQueryBuilder<T> IJoinQueryBuilder<T>.OrWhere(Expression<Func<T, bool>> predicate)
        {
            var result = _converter.Convert(predicate);
            foreach (var param in result.Parameters)
            {
                _context.Parameters[param.Key] = param.Value;
            }
            
            if (_whereConditions.Any())
            {
                var lastCondition = _whereConditions.Last();
                _whereConditions[_whereConditions.Count - 1] = $"({lastCondition}) OR ({result.Sql})";
            }
            else
            {
                _whereConditions.Add(result.Sql);
            }
            return this;
        }

        /// <summary>
        /// Adds typed ORDER BY clause for specific entity
        /// </summary>
        public IJoinQueryBuilder<T> OrderBy<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> selector)
        {
            var column = _converter.ConvertPropertySelector(selector);
            _orderByColumns.Add($"{column} ASC");
            return this;
        }

        /// <summary>
        /// Adds typed ORDER BY DESC clause for specific entity
        /// </summary>
        public IJoinQueryBuilder<T> OrderByDescending<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> selector)
        {
            var column = _converter.ConvertPropertySelector(selector);
            _orderByColumns.Add($"{column} DESC");
            return this;
        }

        /// <summary>
        /// Adds ORDER BY clause for specific entity with alias
        /// </summary>
        public IJoinQueryBuilder<T> OrderBy<TEntity>(string alias, Expression<Func<TEntity, object>> selector)
        {
            var converter = new ExpressionToSqlConverter(_context);
            var column = converter.ConvertPropertySelector(selector);
            // Manually prefix with alias since ConvertPropertySelectorWithAlias doesn't exist
            var aliasedColumn = $"{alias}.{column.Split('.').Last()}";
            _orderByColumns.Add($"{aliasedColumn} ASC");
            return this;
        }

        /// <summary>
        /// Adds typed additional ORDER BY clause for specific entity
        /// </summary>
        public IJoinQueryBuilder<T> ThenBy<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> selector)
        {
            var column = _converter.ConvertPropertySelector(selector);
            _orderByColumns.Add($"{column} ASC");
            return this;
        }

        /// <summary>
        /// Adds typed additional ORDER BY DESC clause for specific entity
        /// </summary>
        public IJoinQueryBuilder<T> ThenByDescending<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> selector)
        {
            var column = _converter.ConvertPropertySelector(selector);
            _orderByColumns.Add($"{column} DESC");
            return this;
        }

        /// <summary>
        /// Adds typed additional ORDER BY clause for specific entity with alias
        /// </summary>
        public IJoinQueryBuilder<T> ThenBy<TEntity>(string alias, Expression<Func<TEntity, object>> selector)
        {
            var column = _converter.ConvertPropertySelector(selector);
            _orderByColumns.Add($"{column} ASC");
            return this;
        }

        /// <summary>
        /// Adds typed GROUP BY clause for specific entity
        /// </summary>
        public IJoinQueryBuilder<T> GroupBy<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> selector)
        {
            var column = _converter.ConvertPropertySelector(selector);
            _groupByColumns.Add(column);
            return this;
        }

        /// <summary>
        /// Adds typed HAVING clause for specific entity
        /// </summary>
        public IJoinQueryBuilder<T> Having<TEntity>(Expression<Func<TEntity, bool>> predicate)
        {
            var result = _converter.Convert(predicate);
            foreach (var param in result.Parameters)
            {
                _context.Parameters[param.Key] = param.Value;
            }
            _havingConditions.Add(result.Sql);
            return this;
        }

        /// <summary>
        /// Adds a JOIN clause with alias support
        /// </summary>
        protected IJoinQueryBuilder<T> AddJoinWithAlias<TJoin>(string joinType, string alias, Expression<Func<T, TJoin, bool>> joinCondition)
        {
            var joinTableName = _context.GetTableName(typeof(TJoin));
            
            // For now, we'll create a simple join condition
            // This is a simplified implementation that would need to be enhanced for production
            var conditionSql = ConvertJoinConditionWithAlias<TJoin>(joinCondition, alias);
            
            var joinClause = $"{joinType} {_context.QuoteIdentifier(joinTableName)} AS {alias} ON {conditionSql}";
            _joinClauses.Add(joinClause);
            
            return this;
        }

        /// <summary>
        /// Converts a join condition expression to SQL with custom alias
        /// </summary>
        private string ConvertJoinConditionWithAlias<TJoin>(Expression<Func<T, TJoin, bool>> joinCondition, string alias)
        {
            // This is a simplified implementation
            // In a full implementation, this would properly parse the expression tree
            // For now, we'll return a placeholder that works for basic scenarios
            var primaryTableAlias = _context.GetTableAlias(typeof(T));
            
            // This is a very basic implementation - would need enhancement for real-world use
            return $"{primaryTableAlias}.Id = {alias}.Id";
        }

        // IJoinQueryBuilder interface overloads that return IJoinQueryBuilder<T>

        /// <summary>
        /// Adds window function (JOIN version)
        /// </summary>
        IJoinQueryBuilder<T> IJoinQueryBuilder<T>.SelectWindow(string windowFunction, string alias)
        {
            _selectColumns.Add($"{windowFunction} AS {alias}");
            return this;
        }

        /// <summary>
        /// Adds subquery in SELECT clause (JOIN version)
        /// </summary>
        IJoinQueryBuilder<T> IJoinQueryBuilder<T>.SelectSubQuery(string alias, IQueryBuilder subQuery)
        {
            var subQuerySql = subQuery.GetSql();
            var subQueryParams = subQuery.GetParameters();
            
            // Merge parameters
            foreach (var param in subQueryParams)
            {
                _context.Parameters[param.Key] = param.Value;
            }
            
            _selectColumns.Add($"({subQuerySql}) AS {alias}");
            return this;
        }

        /// <summary>
        /// Selects specific columns using lambda expressions (JOIN version)
        /// </summary>
        IJoinQueryBuilder<T> IJoinQueryBuilder<T>.Column<TProperty>(Expression<Func<T, TProperty>> selector)
        {
            var columnSql = _converter.ConvertPropertySelector(selector);
            _selectColumns.Add(columnSql);
            return this;
        }

        /// <summary>
        /// Selects specific columns using lambda expressions with alias (JOIN version)
        /// </summary>
        IJoinQueryBuilder<T> IJoinQueryBuilder<T>.Column<TProperty>(Expression<Func<T, TProperty>> selector, string alias)
        {
            var columnSql = _converter.ConvertPropertySelector(selector);
            _selectColumns.Add($"{columnSql} AS [{alias}]");
            return this;
        }

        /// <summary>
        /// Selects specific columns from any joined entity
        /// </summary>
        public IJoinQueryBuilder<T> Column<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> selector)
        {
            var columnSql = _converter.ConvertPropertySelector(selector);
            _selectColumns.Add(columnSql);
            return this;
        }

        /// <summary>
        /// Selects specific columns from any joined entity with alias
        /// </summary>
        public IJoinQueryBuilder<T> Column<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> selector, string alias)
        {
            var columnSql = _converter.ConvertPropertySelector(selector);
            _selectColumns.Add($"{columnSql} AS [{alias}]");
            return this;
        }

        /// <summary>
        /// Selects specific columns from any joined entity with table alias
        /// </summary>
        public IJoinQueryBuilder<T> Column<TEntity, TProperty>(string tableAlias, Expression<Func<TEntity, TProperty>> selector)
        {
            var propertyName = _converter.GetPropertyName(selector);
            var columnName = _context.GetColumnName(typeof(TEntity), propertyName);
            var columnSql = $"{tableAlias}.{_context.QuoteIdentifier(columnName)}";
            _selectColumns.Add(columnSql);
            return this;
        }

        /// <summary>
        /// Selects specific columns from any joined entity with table alias and column alias
        /// </summary>
        public IJoinQueryBuilder<T> Column<TEntity, TProperty>(string tableAlias, Expression<Func<TEntity, TProperty>> selector, string columnAlias)
        {
            var propertyName = _converter.GetPropertyName(selector);
            var columnName = _context.GetColumnName(typeof(TEntity), propertyName);
            var columnSql = $"{tableAlias}.{_context.QuoteIdentifier(columnName)}";
            _selectColumns.Add($"{columnSql} AS [{columnAlias}]");
            return this;
        }

        /// <summary>
        /// Adds WHERE IN clause with subquery (JOIN version)
        /// </summary>
        IJoinQueryBuilder<T> IJoinQueryBuilder<T>.WhereIn<TProperty, TSubQuery>(Expression<Func<T, TProperty>> selector, IQueryBuilder subQuery, Expression<Func<TSubQuery, TProperty>> subQuerySelector)
        {
            var column = _converter.ConvertPropertySelector(selector);
            var subQuerySql = subQuery.GetSql();
            var subQueryParams = subQuery.GetParameters();
            
            // Merge parameters
            foreach (var param in subQueryParams)
            {
                _context.Parameters[param.Key] = param.Value;
            }
            
            // Create the subquery column selection
            var subQueryConverter = new ExpressionToSqlConverter();
            var subQueryColumn = subQueryConverter.ConvertPropertySelector(subQuerySelector);
            
            _whereConditions.Add($"{column} IN (SELECT {subQueryColumn} FROM ({subQuerySql}) subq)");
            return this;
        }

        /// <summary>
        /// Adds WHERE IN clause with collection of values (JOIN version)
        /// </summary>
        IJoinQueryBuilder<T> IJoinQueryBuilder<T>.WhereIn<TProperty>(Expression<Func<T, TProperty>> selector, IEnumerable<TProperty> values)
        {
            // Delegate to the main implementation
            WhereIn(selector, values);
            return this;
        }

        /// <summary>
        /// Adds raw WHERE clause (JOIN version)
        /// </summary>
        IJoinQueryBuilder<T> IJoinQueryBuilder<T>.WhereRaw(string rawCondition)
        {
            _whereConditions.Add(rawCondition);
            return this;
        }

        /// <summary>
        /// Adds WHERE condition for specific entity (JOIN version)
        /// </summary>
        IJoinQueryBuilder<T> IJoinQueryBuilder<T>.Where<TEntity>(Expression<Func<TEntity, bool>> predicate)
        {
            var converter = new ExpressionToSqlConverter(_context);
            var result = converter.Convert(predicate);
            _whereConditions.Add(result.Sql);
            return this;
        }

        /// <summary>
        /// Adds WHERE condition for specific entity with alias (JOIN version)
        /// </summary>
        IJoinQueryBuilder<T> IJoinQueryBuilder<T>.Where<TEntity>(string alias, Expression<Func<TEntity, bool>> predicate)
        {
            var converter = new ExpressionToSqlConverter(_context);
            var result = converter.Convert(predicate);
            // Note: For a full implementation, we'd need to replace table references with the alias
            _whereConditions.Add(result.Sql);
            return this;
        }

        /// <summary>
        /// Selects from Common Table Expression
        /// </summary>
        public ISelectQueryBuilder<T> FromCTE(string cteName)
        {
            _fromCTE = cteName;
            return this;
        }
    }
}
