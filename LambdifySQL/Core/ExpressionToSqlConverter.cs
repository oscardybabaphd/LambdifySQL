using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using LambdifySQL.Core;
using LambdifySQL.Resolver;

namespace LambdifySQL.Core
{
    /// <summary>
    /// Advanced expression visitor for converting lambda expressions to SQL
    /// </summary>
    public class ExpressionToSqlConverter
    {
        private readonly ExpressionContext _context;

        public ExpressionToSqlConverter(ExpressionContext? context = null)
        {
            _context = context ?? new ExpressionContext();
        }

        /// <summary>
        /// Converts a lambda expression to SQL
        /// </summary>
        public ExpressionResult Convert<T>(Expression<Func<T, bool>> expression)
        {
            var body = expression.Body;
            
            // Handle boolean member expressions like p => p.IsActive
            if (body is MemberExpression memberExpr && memberExpr.Type == typeof(bool))
            {
                // Check if this is a property of an entity type
                if (memberExpr.Member is PropertyInfo property && 
                    property.DeclaringType != null && 
                    HasTableNameAttribute(property.DeclaringType))
                {
                    // Convert p.IsActive to p.IsActive = @p0 where @p0 = true
                    var tableAlias = _context.GetTableAlias(property.DeclaringType);
                    var columnName = $"{tableAlias}.{_context.QuoteIdentifier(property.Name)}";
                    var trueParam = _context.AddParameter(true);
                    var sql = $"({columnName} = {trueParam})";
                    return new ExpressionResult(sql, _context.Parameters);
                }
            }
            
            // Handle unary expressions like p => !p.IsActive
            if (body is UnaryExpression unaryExpr && 
                unaryExpr.NodeType == ExpressionType.Not && 
                unaryExpr.Operand is MemberExpression unaryMemberExpr && 
                unaryMemberExpr.Type == typeof(bool))
            {
                // Check if this is a property of an entity type
                if (unaryMemberExpr.Member is PropertyInfo property && 
                    property.DeclaringType != null && 
                    HasTableNameAttribute(property.DeclaringType))
                {
                    // Convert !p.IsActive to p.IsActive = @p0 where @p0 = false
                    var tableAlias = _context.GetTableAlias(property.DeclaringType);
                    var columnName = $"{tableAlias}.{_context.QuoteIdentifier(property.Name)}";
                    var falseParam = _context.AddParameter(false);
                    var sql = $"({columnName} = {falseParam})";
                    return new ExpressionResult(sql, _context.Parameters);
                }
            }
            
            // Default handling for other expressions
            var defaultSql = Visit(body);
            return new ExpressionResult(defaultSql, _context.Parameters);
        }

        /// <summary>
        /// Converts a property selector expression to SQL column name
        /// </summary>
        public string ConvertPropertySelector<T, TProperty>(Expression<Func<T, TProperty>> expression)
        {
            var tableAlias = _context.GetTableAlias(typeof(T));

            switch (expression.Body)
            {
                case MemberExpression memberExpr:
                    return $"{tableAlias}.{_context.QuoteIdentifier(memberExpr.Member.Name)}";

                case UnaryExpression unaryExpr when unaryExpr.NodeType == ExpressionType.Convert:
                    if (unaryExpr.Operand is MemberExpression convertedMember)
                    {
                        return $"{tableAlias}.{_context.QuoteIdentifier(convertedMember.Member.Name)}";
                    }
                    break;
            }

            throw new NotSupportedException($"Expression type {expression.Body.GetType()} is not supported for property selection");
        }

        /// <summary>
        /// Converts multiple property selectors to SQL column names
        /// </summary>
        public List<string> ConvertPropertySelectors<T>(Expression<Func<T, object>> expression)
        {
            var columns = new List<string>();
            var tableAlias = _context.GetTableAlias(typeof(T));

            switch (expression.Body)
            {
                case MemberExpression memberExpr:
                    columns.Add($"{tableAlias}.{_context.QuoteIdentifier(memberExpr.Member.Name)}");
                    break;

                case NewExpression newExpr:
                    foreach (var arg in newExpr.Arguments)
                    {
                        if (arg is MemberExpression argMember)
                        {
                            columns.Add($"{tableAlias}.{_context.QuoteIdentifier(argMember.Member.Name)}");
                        }
                    }
                    break;

                case UnaryExpression unaryExpr when unaryExpr.NodeType == ExpressionType.Convert:
                    if (unaryExpr.Operand is MemberExpression convertedMember)
                    {
                        columns.Add($"{tableAlias}.{_context.QuoteIdentifier(convertedMember.Member.Name)}");
                    }
                    break;

                default:
                    throw new NotSupportedException($"Expression type {expression.Body.GetType()} is not supported for property selection");
            }

            return columns;
        }

        /// <summary>
        /// Visits an expression node and converts it to SQL
        /// </summary>
        public string Visit(Expression expression)
        {
            return expression switch
            {
                BinaryExpression binaryExpr => VisitBinary(binaryExpr),
                UnaryExpression unaryExpr => VisitUnary(unaryExpr),
                MemberExpression memberExpr => VisitMember(memberExpr),
                ConstantExpression constantExpr => VisitConstant(constantExpr),
                MethodCallExpression methodCallExpr => VisitMethodCall(methodCallExpr),
                _ => throw new NotSupportedException($"Expression type {expression.GetType()} is not supported")
            };
        }

        /// <summary>
        /// Visits a binary expression (e.g., ==, !=, >, <, AND, OR)
        /// </summary>
        private string VisitBinary(BinaryExpression expression)
        {
            var left = Visit(expression.Left);
            var right = Visit(expression.Right);
            var op = GetSqlOperator(expression.NodeType);

            return $"({left} {op} {right})";
        }

        /// <summary>
        /// Visits a unary expression (e.g., NOT)
        /// </summary>
        private string VisitUnary(UnaryExpression expression)
        {
            var operand = Visit(expression.Operand);

            return expression.NodeType switch
            {
                ExpressionType.Not => $"NOT ({operand})",
                ExpressionType.Convert => operand, // Skip conversion for SQL
                _ => throw new NotSupportedException($"Unary operator {expression.NodeType} is not supported")
            };
        }

        /// <summary>
        /// Visits a member expression (property access)
        /// </summary>
        private string VisitMember(MemberExpression expression)
        {
            // Handle property access on entity types
            if (expression.Member is PropertyInfo property)
            {
                // Check if this is a property of an entity type
                var declaringType = property.DeclaringType;
                if (declaringType != null && HasTableNameAttribute(declaringType))
                {
                    var tableAlias = _context.GetTableAlias(declaringType);
                    return $"{tableAlias}.{_context.QuoteIdentifier(property.Name)}";
                }

                // Handle member access on captured variables/fields
                var value = GetMemberValue(expression);
                return _context.AddParameter(value);
            }

            // Handle field access
            if (expression.Member is FieldInfo)
            {
                var value = GetMemberValue(expression);
                return _context.AddParameter(value);
            }

            throw new NotSupportedException($"Member {expression.Member.Name} is not supported");
        }

        /// <summary>
        /// Visits a constant expression
        /// </summary>
        private string VisitConstant(ConstantExpression expression)
        {
            return _context.AddParameter(expression.Value);
        }

        /// <summary>
        /// Visits a method call expression
        /// </summary>
        private string VisitMethodCall(MethodCallExpression expression)
        {
            var method = expression.Method;

            // String methods
            if (method.DeclaringType == typeof(string))
            {
                return method.Name switch
                {
                    "Contains" => HandleStringContains(expression),
                    "StartsWith" => HandleStringStartsWith(expression),
                    "EndsWith" => HandleStringEndsWith(expression),
                    "ToUpper" => HandleStringToUpper(expression),
                    "ToLower" => HandleStringToLower(expression),
                    _ => throw new NotSupportedException($"String method {method.Name} is not supported")
                };
            }

            // Collection methods
            if (method.Name == "Contains" && typeof(IEnumerable).IsAssignableFrom(method.DeclaringType))
            {
                return HandleCollectionContains(expression);
            }

            // Math methods
            if (method.DeclaringType == typeof(Math))
            {
                return HandleMathMethod(expression);
            }

            // DateTime methods
            if (method.DeclaringType == typeof(DateTime))
            {
                return HandleDateTimeMethod(expression);
            }

            throw new NotSupportedException($"Method {method.DeclaringType?.Name}.{method.Name} is not supported");
        }

        /// <summary>
        /// Handles string.Contains method
        /// </summary>
        private string HandleStringContains(MethodCallExpression expression)
        {
            var target = Visit(expression.Object!);
            var paramValue = GetParameterValue(expression.Arguments[0]);
            var likeParam = _context.AddParameter($"%{paramValue}%");
            
            return $"{target} LIKE {likeParam}";
        }

        /// <summary>
        /// Handles string.StartsWith method
        /// </summary>
        private string HandleStringStartsWith(MethodCallExpression expression)
        {
            var target = Visit(expression.Object!);
            var paramValue = GetParameterValue(expression.Arguments[0]);
            var likeParam = _context.AddParameter($"{paramValue}%");
            
            return $"{target} LIKE {likeParam}";
        }

        /// <summary>
        /// Handles string.EndsWith method
        /// </summary>
        private string HandleStringEndsWith(MethodCallExpression expression)
        {
            var target = Visit(expression.Object!);
            var paramValue = GetParameterValue(expression.Arguments[0]);
            var likeParam = _context.AddParameter($"%{paramValue}");
            
            return $"{target} LIKE {likeParam}";
        }

        /// <summary>
        /// Handles string.ToUpper method
        /// </summary>
        private string HandleStringToUpper(MethodCallExpression expression)
        {
            var target = Visit(expression.Object!);
            return $"UPPER({target})";
        }

        /// <summary>
        /// Handles string.ToLower method
        /// </summary>
        private string HandleStringToLower(MethodCallExpression expression)
        {
            var target = Visit(expression.Object!);
            return $"LOWER({target})";
        }

        /// <summary>
        /// Handles collection.Contains method for IN clauses
        /// </summary>
        private string HandleCollectionContains(MethodCallExpression expression)
        {
            string property;
            IEnumerable collection;

            // Handle both extension method and instance method forms
            if (expression.Method.IsStatic && expression.Arguments.Count == 2)
            {
                // Extension method: collection.Contains(item)
                collection = (IEnumerable)GetParameterValue(expression.Arguments[0]);
                property = Visit(expression.Arguments[1]);
            }
            else if (!expression.Method.IsStatic && expression.Arguments.Count == 1)
            {
                // Instance method: item.Contains(collection)
                collection = (IEnumerable)GetParameterValue(expression.Object!);
                property = Visit(expression.Arguments[0]);
            }
            else
            {
                throw new NotSupportedException("Unsupported Contains method call");
            }

            var values = collection.Cast<object>().ToList();
            if (!values.Any())
            {
                return "1 = 0"; // FALSE condition
            }

            var parameters = values.Select(v => _context.AddParameter(v));
            return $"{property} IN ({string.Join(", ", parameters)})";
        }

        /// <summary>
        /// Handles Math methods
        /// </summary>
        private string HandleMathMethod(MethodCallExpression expression)
        {
            return expression.Method.Name switch
            {
                "Abs" => $"ABS({Visit(expression.Arguments[0])})",
                "Ceiling" => $"CEILING({Visit(expression.Arguments[0])})",
                "Floor" => $"FLOOR({Visit(expression.Arguments[0])})",
                "Round" when expression.Arguments.Count == 1 => $"ROUND({Visit(expression.Arguments[0])}, 0)",
                "Round" when expression.Arguments.Count == 2 => $"ROUND({Visit(expression.Arguments[0])}, {Visit(expression.Arguments[1])})",
                _ => throw new NotSupportedException($"Math method {expression.Method.Name} is not supported")
            };
        }

        /// <summary>
        /// Handles DateTime methods and properties
        /// </summary>
        private string HandleDateTimeMethod(MethodCallExpression expression)
        {
            return expression.Method.Name switch
            {
                "AddDays" => $"DATEADD(day, {Visit(expression.Arguments[0])}, {Visit(expression.Object!)})",
                "AddMonths" => $"DATEADD(month, {Visit(expression.Arguments[0])}, {Visit(expression.Object!)})",
                "AddYears" => $"DATEADD(year, {Visit(expression.Arguments[0])}, {Visit(expression.Object!)})",
                _ => throw new NotSupportedException($"DateTime method {expression.Method.Name} is not supported")
            };
        }

        /// <summary>
        /// Gets the SQL operator for an expression type
        /// </summary>
        private string GetSqlOperator(ExpressionType nodeType)
        {
            return nodeType switch
            {
                ExpressionType.Equal => "=",
                ExpressionType.NotEqual => "<>",
                ExpressionType.GreaterThan => ">",
                ExpressionType.GreaterThanOrEqual => ">=",
                ExpressionType.LessThan => "<",
                ExpressionType.LessThanOrEqual => "<=",
                ExpressionType.AndAlso => "AND",
                ExpressionType.OrElse => "OR",
                ExpressionType.Add => "+",
                ExpressionType.Subtract => "-",
                ExpressionType.Multiply => "*",
                ExpressionType.Divide => "/",
                ExpressionType.Modulo => "%",
                _ => throw new NotSupportedException($"Binary operator {nodeType} is not supported")
            };
        }

        /// <summary>
        /// Gets the value from a member expression
        /// </summary>
        private object GetMemberValue(MemberExpression expression)
        {
            try
            {
                var objectMember = Expression.Convert(expression, typeof(object));
                var getterLambda = Expression.Lambda<Func<object>>(objectMember);
                var getter = getterLambda.Compile();
                return getter();
            }
            catch
            {
                throw new NotSupportedException($"Cannot evaluate member expression: {expression}");
            }
        }

        /// <summary>
        /// Gets the value from any expression
        /// </summary>
        private object GetParameterValue(Expression expression)
        {
            try
            {
                var objectMember = Expression.Convert(expression, typeof(object));
                var getterLambda = Expression.Lambda<Func<object>>(objectMember);
                var getter = getterLambda.Compile();
                return getter();
            }
            catch
            {
                throw new NotSupportedException($"Cannot evaluate expression: {expression}");
            }
        }

        /// <summary>
        /// Gets the property name from a lambda expression
        /// </summary>
        public string GetPropertyName<T, TProperty>(Expression<Func<T, TProperty>> expression)
        {
            if (expression.Body is MemberExpression memberExpr)
            {
                return memberExpr.Member.Name;
            }
            
            if (expression.Body is UnaryExpression unaryExpr && 
                unaryExpr.Operand is MemberExpression memberOperand)
            {
                return memberOperand.Member.Name;
            }
            
            throw new ArgumentException("Expression must be a property selector", nameof(expression));
        }

        /// <summary>
        /// Checks if a type has a TableName attribute
        /// </summary>
        private bool HasTableNameAttribute(Type type)
        {
            return type.GetCustomAttribute<TableNameAttribute>() != null;
        }
    }
}
