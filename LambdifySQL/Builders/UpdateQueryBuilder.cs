using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using LambdifySQL.Core;
using System.Reflection;
using LambdifySQL.Resolver;

namespace LambdifySQL.Builders
{
    /// <summary>
    /// Fluent UPDATE query builder
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    public class UpdateQueryBuilder<T> : IUpdateQueryBuilder<T>
    {
        private readonly ExpressionContext _context;
        private readonly ExpressionToSqlConverter _converter;
        private readonly List<string> _setColumns = new();
        private readonly List<string> _whereConditions = new();

        public UpdateQueryBuilder(ExpressionContext context = null)
        {
            _context = context ?? new ExpressionContext();
            _converter = new ExpressionToSqlConverter(_context);
            
            // Register the table
            _context.GetTableAlias(typeof(T));
        }

        /// <summary>
        /// Sets a specific property value
        /// </summary>
        public IUpdateQueryBuilder<T> Set<TProperty>(Expression<Func<T, TProperty>> selector, TProperty value)
        {
            var column = GetColumnName(selector);
            var paramName = _context.AddParameter(value);
            _setColumns.Add($"{column} = {paramName}");
            return this;
        }

        /// <summary>
        /// Sets a property value using an expression
        /// </summary>
        public IUpdateQueryBuilder<T> Set<TProperty>(Expression<Func<T, TProperty>> selector, Expression<Func<T, TProperty>> valueExpression)
        {
            var column = GetColumnName(selector);
            var valueConverter = new ExpressionToSqlConverter(_context);
            
            // Convert the value expression to SQL
            // For expressions like p => p.Price * 1.1m, we need to handle different types
            var valueSql = ConvertValueExpression(valueExpression);
            
            _setColumns.Add($"{column} = {valueSql}");
            return this;
        }

        /// <summary>
        /// Sets multiple values using an object
        /// </summary>
        public IUpdateQueryBuilder<T> Set(object values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            var properties = values.GetType().GetProperties();
            var tableAlias = _context.GetTableAlias(typeof(T));

            foreach (var property in properties)
            {
                var value = property.GetValue(values);
                var paramName = _context.AddParameter(value);
                var columnName = $"{tableAlias}.{_context.QuoteIdentifier(property.Name)}";
                _setColumns.Add($"{columnName} = {paramName}");
            }

            return this;
        }

        /// <summary>
        /// Adds a WHERE clause
        /// </summary>
        public IUpdateQueryBuilder<T> Where(Expression<Func<T, bool>> predicate)
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
        public IUpdateQueryBuilder<T> OrWhere(Expression<Func<T, bool>> predicate)
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
        public IUpdateQueryBuilder<T> AndWhere(Expression<Func<T, bool>> predicate)
        {
            return Where(predicate); // WHERE clauses are AND by default
        }

        /// <summary>
        /// Gets the generated SQL query
        /// </summary>
        public string GetSql()
        {
            if (!_setColumns.Any())
                throw new InvalidOperationException("UPDATE query must have at least one SET clause");

            var sql = new StringBuilder();
            var tableName = _context.GetTableName(typeof(T));
            var tableAlias = _context.GetTableAlias(typeof(T));

            // UPDATE clause
            sql.Append($"UPDATE {tableAlias}");
            sql.AppendLine();

            // SET clause
            sql.Append($"SET {string.Join(", ", _setColumns)}");
            sql.AppendLine();

            // FROM clause (for alias)
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
            _setColumns.Clear();
            _whereConditions.Clear();
            _context.Parameters.Clear();
            _context.ParameterCounter = 0;
        }

        /// <summary>
        /// Gets the column name from a property selector
        /// </summary>
        private string GetColumnName<TProperty>(Expression<Func<T, TProperty>> selector)
        {
            if (selector.Body is MemberExpression memberExpr && memberExpr.Member is PropertyInfo)
            {
                var tableAlias = _context.GetTableAlias(typeof(T));
                return $"{tableAlias}.{_context.QuoteIdentifier(memberExpr.Member.Name)}";
            }

            throw new ArgumentException("Selector must be a property expression", nameof(selector));
        }

        /// <summary>
        /// Converts a value expression to SQL
        /// </summary>
        private string ConvertValueExpression<TProperty>(Expression<Func<T, TProperty>> valueExpression)
        {
            return ConvertExpressionToSql(valueExpression.Body);
        }

        /// <summary>
        /// Converts an expression tree to SQL
        /// </summary>
        private string ConvertExpressionToSql(Expression expression)
        {
            switch (expression)
            {
                case MemberExpression memberExpr:
                    // Handle property access like p.Price
                    if (memberExpr.Member is PropertyInfo property)
                    {
                        var tableAlias = _context.GetTableAlias(typeof(T));
                        return $"{tableAlias}.{_context.QuoteIdentifier(property.Name)}";
                    }
                    break;

                case BinaryExpression binaryExpr:
                    // Handle operations like p.Price * 1.1m
                    var left = ConvertExpressionToSql(binaryExpr.Left);
                    var right = ConvertExpressionToSql(binaryExpr.Right);
                    var op = GetSqlOperator(binaryExpr.NodeType);
                    return $"({left} {op} {right})";

                case ConstantExpression constantExpr:
                    // Handle constant values like 1.1m
                    var paramName = _context.AddParameter(constantExpr.Value);
                    return paramName;

                case UnaryExpression unaryExpr:
                    // Handle unary operations like -p.Price
                    if (unaryExpr.NodeType == ExpressionType.Negate)
                    {
                        var operand = ConvertExpressionToSql(unaryExpr.Operand);
                        return $"-{operand}";
                    }
                    break;

                case MethodCallExpression methodExpr:
                    // Handle method calls like Math.Round(p.Price, 2)
                    return ConvertMethodCall(methodExpr);
            }

            throw new NotSupportedException($"Expression type {expression.NodeType} is not supported in SET clauses");
        }

        /// <summary>
        /// Gets the SQL operator for a binary expression type
        /// </summary>
        private string GetSqlOperator(ExpressionType nodeType)
        {
            return nodeType switch
            {
                ExpressionType.Add => "+",
                ExpressionType.Subtract => "-",
                ExpressionType.Multiply => "*",
                ExpressionType.Divide => "/",
                ExpressionType.Modulo => "%",
                _ => throw new NotSupportedException($"Binary operator {nodeType} is not supported")
            };
        }

        /// <summary>
        /// Converts method call expressions to SQL
        /// </summary>
        private string ConvertMethodCall(MethodCallExpression methodExpr)
        {
            // Handle common mathematical functions
            if (methodExpr.Method.DeclaringType == typeof(Math))
            {
                switch (methodExpr.Method.Name)
                {
                    case "Round":
                        var value = ConvertExpressionToSql(methodExpr.Arguments[0]);
                        if (methodExpr.Arguments.Count == 2)
                        {
                            var digits = ConvertExpressionToSql(methodExpr.Arguments[1]);
                            return $"ROUND({value}, {digits})";
                        }
                        return $"ROUND({value})";

                    case "Abs":
                        var absValue = ConvertExpressionToSql(methodExpr.Arguments[0]);
                        return $"ABS({absValue})";

                    case "Ceiling":
                        var ceilValue = ConvertExpressionToSql(methodExpr.Arguments[0]);
                        return $"CEILING({ceilValue})";

                    case "Floor":
                        var floorValue = ConvertExpressionToSql(methodExpr.Arguments[0]);
                        return $"FLOOR({floorValue})";
                }
            }

            throw new NotSupportedException($"Method {methodExpr.Method.Name} is not supported in SET clauses");
        }
    }
}
