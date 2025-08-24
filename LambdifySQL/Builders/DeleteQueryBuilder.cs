using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using LambdifySQL.Core;

namespace LambdifySQL.Builders
{
    /// <summary>
    /// Fluent DELETE query builder
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    public class DeleteQueryBuilder<T> : IDeleteQueryBuilder<T>
    {
        private readonly ExpressionContext _context;
        private readonly ExpressionToSqlConverter _converter;
        private readonly List<string> _whereConditions = new();

        public DeleteQueryBuilder(ExpressionContext context = null)
        {
            _context = context ?? new ExpressionContext();
            _converter = new ExpressionToSqlConverter(_context);
            
            // Register the table
            _context.GetTableAlias(typeof(T));
        }

        /// <summary>
        /// Adds a WHERE clause
        /// </summary>
        public IDeleteQueryBuilder<T> Where(Expression<Func<T, bool>> predicate)
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
        public IDeleteQueryBuilder<T> OrWhere(Expression<Func<T, bool>> predicate)
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
        public IDeleteQueryBuilder<T> AndWhere(Expression<Func<T, bool>> predicate)
        {
            return Where(predicate); // WHERE clauses are AND by default
        }

        /// <summary>
        /// Gets the generated SQL query
        /// </summary>
        public string GetSql()
        {
            var sql = new StringBuilder();
            var tableName = _context.GetTableName(typeof(T));
            var tableAlias = _context.GetTableAlias(typeof(T));

            // DELETE clause
            sql.Append($"DELETE {tableAlias}");
            sql.AppendLine();

            // FROM clause
            sql.Append($"FROM {_context.QuoteIdentifier(tableName)} AS {tableAlias}");

            // WHERE clause
            if (_whereConditions.Any())
            {
                sql.AppendLine();
                sql.Append($"WHERE {string.Join(" AND ", _whereConditions)}");
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
            _whereConditions.Clear();
            _context.Parameters.Clear();
            _context.ParameterCounter = 0;
        }
    }
}
