using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using LambdifySQL.Core;
using LambdifySQL.Resolver;

namespace LambdifySQL.Builders
{
    /// <summary>
    /// Fluent INSERT query builder
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    public class InsertQueryBuilder<T> : IInsertQueryBuilder<T>
    {
        private readonly ExpressionContext _context;
        private readonly Dictionary<string, object> _values = new();
        private string _conflictAction;

        public InsertQueryBuilder(ExpressionContext context = null)
        {
            _context = context ?? new ExpressionContext();

            // Register the table
            _context.GetTableAlias(typeof(T));
        }

        /// <summary>
        /// Sets the values to insert from an entity
        /// </summary>
        public IInsertQueryBuilder<T> Values(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var properties = typeof(T).GetProperties()
                .Where(p => !HasIgnoreAttribute(p) && !HasPrimaryKeyAttribute(p));

            foreach (var property in properties)
            {
                var value = property.GetValue(entity);
                //check if the property has DefaultValue attribute and get the value 
                var defaultValueAttr = property.GetCustomAttribute<DefaultValueAttribute>();
                if (defaultValueAttr != null && value is DateTime dt && dt == default(DateTime))
                {
                    value = DateTime.Now; //defaultValueAttr.Value;
                }
                _values[property.Name] = value;
            }

            return this;
        }

        /// <summary>
        /// Sets values using an anonymous object
        /// </summary>
        public IInsertQueryBuilder<T> Values(object values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            var properties = values.GetType().GetProperties();
            foreach (var property in properties)
            {
                var value = property.GetValue(values);
                _values[property.Name] = value;
            }

            return this;
        }

        /// <summary>
        /// Sets multiple values for bulk insertion (returns the first entity for interface compliance)
        /// </summary>
        public IInsertQueryBuilder<T> Values(IEnumerable<T> entities)
        {
            // For this basic implementation, we'll just take the first entity
            // A full implementation would need to handle bulk operations differently
            if (entities != null)
            {
                var firstEntity = entities.FirstOrDefault();
                if (firstEntity != null)
                {
                    return Values(firstEntity);
                }
            }
            return this;
        }

        /// <summary>
        /// Sets a specific column value
        /// </summary>
        public IInsertQueryBuilder<T> Value<TProperty>(Expression<Func<T, TProperty>> selector, TProperty value)
        {
            var columnName = GetColumnName(selector);
            _values[columnName] = value;
            return this;
        }

        /// <summary>
        /// Adds ON CONFLICT clause for upsert operations
        /// </summary>
        public IInsertQueryBuilder<T> OnConflict(string conflictAction)
        {
            _conflictAction = conflictAction;
            return this;
        }

        /// <summary>
        /// Gets the generated SQL query
        /// </summary>
        public string GetSql()
        {
            if (!_values.Any())
                throw new InvalidOperationException("INSERT query must have at least one value");

            var sql = new StringBuilder();
            var tableName = _context.GetTableName(typeof(T));

            // INSERT clause
            sql.Append($"INSERT INTO {_context.QuoteIdentifier(tableName)}");
            sql.AppendLine();

            // Columns
            var columns = _values.Keys.Select(k => _context.QuoteIdentifier(k));
            sql.Append($"({string.Join(", ", columns)})");
            sql.AppendLine();

            // VALUES clause
            var parameters = new List<string>();
            foreach (var kvp in _values)
            {
                var paramName = _context.AddParameter(kvp.Value);
                parameters.Add(paramName);
            }

            sql.Append($"VALUES ({string.Join(", ", parameters)})");

            // ON CONFLICT clause (for databases that support it)
            if (!string.IsNullOrEmpty(_conflictAction))
            {
                sql.AppendLine();
                sql.Append(_conflictAction);
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
            _values.Clear();
            _context.Parameters.Clear();
            _context.ParameterCounter = 0;
            _conflictAction = null;
        }

        /// <summary>
        /// Gets the column name from a property selector
        /// </summary>
        private string GetColumnName<TProperty>(Expression<Func<T, TProperty>> selector)
        {
            if (selector.Body is MemberExpression memberExpr && memberExpr.Member is PropertyInfo property)
            {
                return property.Name;
            }

            throw new ArgumentException("Selector must be a property expression", nameof(selector));
        }

        /// <summary>
        /// Checks if a property has the IgnoreMe attribute
        /// </summary>
        private bool HasIgnoreAttribute(PropertyInfo property)
        {
            return property.GetCustomAttribute<IgnoreMeAttribute>() != null;
        }

        /// <summary>
        /// Checks if a property has the Pk attribute
        /// </summary>
        private bool HasPrimaryKeyAttribute(PropertyInfo property)
        {
            return property.GetCustomAttribute<PkAttribute>() != null;
        }
    }

    /// <summary>
    /// Batch INSERT query builder for inserting multiple records
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    public class BulkInsertQueryBuilder<T> : IQueryBuilder
    {
        private readonly ExpressionContext _context;
        private readonly List<T> _entities = new();
        private readonly List<string> _columns = new();

        public BulkInsertQueryBuilder(ExpressionContext context = null)
        {
            _context = context ?? new ExpressionContext();
        }

        /// <summary>
        /// Adds entities to insert
        /// </summary>
        public BulkInsertQueryBuilder<T> Values(IEnumerable<T> entities)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            _entities.AddRange(entities);
            return this;
        }

        /// <summary>
        /// Adds a single entity to insert
        /// </summary>
        public BulkInsertQueryBuilder<T> Values(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _entities.Add(entity);
            return this;
        }

        /// <summary>
        /// Gets the generated SQL query
        /// </summary>
        public string GetSql()
        {
            if (!_entities.Any())
                throw new InvalidOperationException("Bulk INSERT query must have at least one entity");

            var sql = new StringBuilder();
            var tableName = _context.GetTableName(typeof(T));

            // Get columns from first entity (excluding ignored and PK columns)
            var properties = typeof(T).GetProperties()
                .Where(p => !HasIgnoreAttribute(p) && !HasPrimaryKeyAttribute(p))
                .ToList();

            if (!properties.Any())
                throw new InvalidOperationException("No insertable properties found");

            // INSERT clause
            sql.Append($"INSERT INTO {_context.QuoteIdentifier(tableName)}");
            sql.AppendLine();

            // Columns
            var columns = properties.Select(p => _context.QuoteIdentifier(p.Name));
            sql.Append($"({string.Join(", ", columns)})");
            sql.AppendLine();

            // VALUES clauses
            sql.Append("VALUES");
            sql.AppendLine();

            var valuesClauses = new List<string>();
            foreach (var entity in _entities)
            {
                var values = new List<string>();
                foreach (var property in properties)
                {
                    var value = property.GetValue(entity);
                    var defaultValueAttr = property.GetCustomAttribute<DefaultValueAttribute>();
                    if (defaultValueAttr != null && value is DateTime dt && dt == default(DateTime))
                    {
                        value = DateTime.Now;
                    }
                    var paramName = _context.AddParameter(value);
                    values.Add(paramName);
                }
                valuesClauses.Add($"({string.Join(", ", values)})");
            }

            sql.Append(string.Join("," + Environment.NewLine, valuesClauses));

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
            _entities.Clear();
            _columns.Clear();
            _context.Parameters.Clear();
            _context.ParameterCounter = 0;
        }

        /// <summary>
        /// Checks if a property has the IgnoreMe attribute
        /// </summary>
        private bool HasIgnoreAttribute(PropertyInfo property)
        {
            return property.GetCustomAttribute<IgnoreMeAttribute>() != null;
        }

        /// <summary>
        /// Checks if a property has the Pk attribute
        /// </summary>
        private bool HasPrimaryKeyAttribute(PropertyInfo property)
        {
            return property.GetCustomAttribute<PkAttribute>() != null;
        }
    }
}
