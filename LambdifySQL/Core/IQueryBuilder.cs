using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LambdifySQL.Core
{
    /// <summary>
    /// Core interface for query building operations
    /// </summary>
    public interface IQueryBuilder
    {
        /// <summary>
        /// Gets the generated SQL query
        /// </summary>
        string GetSql();

        /// <summary>
        /// Gets the parameters for the query
        /// </summary>
        Dictionary<string, object> GetParameters();
    }

    /// <summary>
    /// Interface for basic query operations on type T
    /// </summary>
    public interface ISelectQueryBuilder<T> : IQueryBuilder
    {
        /// <summary>
        /// Adds a WHERE condition
        /// </summary>
        ISelectQueryBuilder<T> Where(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Adds an OR WHERE condition
        /// </summary>
        ISelectQueryBuilder<T> OrWhere(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Adds an ORDER BY clause
        /// </summary>
        ISelectQueryBuilder<T> OrderBy<TProperty>(Expression<Func<T, TProperty>> selector);

        /// <summary>
        /// Adds an ORDER BY clause in descending order
        /// </summary>
        ISelectQueryBuilder<T> OrderByDescending<TProperty>(Expression<Func<T, TProperty>> selector);

        /// <summary>
        /// Adds a THEN BY clause
        /// </summary>
        ISelectQueryBuilder<T> ThenBy<TProperty>(Expression<Func<T, TProperty>> selector);

        /// <summary>
        /// Adds a THEN BY clause in descending order
        /// </summary>
        ISelectQueryBuilder<T> ThenByDescending<TProperty>(Expression<Func<T, TProperty>> selector);

        /// <summary>
        /// Adds a THEN BY clause with raw expression
        /// </summary>
        ISelectQueryBuilder<T> ThenBy(string orderExpression);

        /// <summary>
        /// Adds a TOP clause
        /// </summary>
        ISelectQueryBuilder<T> Top(int count);


        /// <summary>
        /// Adds a Take clause
        /// </summary>
        ISelectQueryBuilder<T> Take(int count);

        /// <summary>
        /// Selects specific columns using lambda expressions
        /// </summary>
        ISelectQueryBuilder<T> Column<TProperty>(Expression<Func<T, TProperty>> selector);

        /// <summary>
        /// Selects specific columns using lambda expressions with alias
        /// </summary>
        ISelectQueryBuilder<T> Column<TProperty>(Expression<Func<T, TProperty>> selector, string alias);

        /// <summary>
        /// Adds window function to SELECT
        /// </summary>
        ISelectQueryBuilder<T> SelectWindow(string windowFunction, string alias);

        /// <summary>
        /// Adds subquery to SELECT
        /// </summary>
        ISelectQueryBuilder<T> SelectSubQuery(string alias, IQueryBuilder subQuery);

        /// <summary>
        /// Adds WHERE IN clause with subquery
        /// </summary>
        ISelectQueryBuilder<T> WhereIn<TProperty, TSubQuery>(Expression<Func<T, TProperty>> selector, IQueryBuilder subQuery, Expression<Func<TSubQuery, TProperty>> subQuerySelector);

        /// <summary>
        /// Adds WHERE IN clause with collection of values
        /// </summary>
        ISelectQueryBuilder<T> WhereIn<TProperty>(Expression<Func<T, TProperty>> selector, IEnumerable<TProperty> values);

        /// <summary>
        /// Adds raw WHERE clause
        /// </summary>
        ISelectQueryBuilder<T> WhereRaw(string rawCondition);

        /// <summary>
        /// Selects from CTE
        /// </summary>
        ISelectQueryBuilder<T> FromCTE(string cteName);
    }

    /// <summary>
    /// Interface for JOIN query operations
    /// </summary>
    public interface IJoinQueryBuilder<T> : ISelectQueryBuilder<T>
    {
        /// <summary>
        /// Adds an INNER JOIN
        /// </summary>
        IJoinQueryBuilder<T> InnerJoin<TJoin>(Expression<Func<T, TJoin, bool>> joinCondition);

        /// <summary>
        /// Adds an INNER JOIN with alias
        /// </summary>
        IJoinQueryBuilder<T> InnerJoin<TJoin>(string alias, Expression<Func<T, TJoin, bool>> joinCondition);

        /// <summary>
        /// Adds a LEFT JOIN
        /// </summary>
        IJoinQueryBuilder<T> LeftJoin<TJoin>(Expression<Func<T, TJoin, bool>> joinCondition);

        /// <summary>
        /// Adds a LEFT JOIN with alias
        /// </summary>
        IJoinQueryBuilder<T> LeftJoin<TJoin>(string alias, Expression<Func<T, TJoin, bool>> joinCondition);

        /// <summary>
        /// Adds a RIGHT JOIN
        /// </summary>
        IJoinQueryBuilder<T> RightJoin<TJoin>(Expression<Func<T, TJoin, bool>> joinCondition);

        /// <summary>
        /// Adds a RIGHT JOIN with alias
        /// </summary>
        IJoinQueryBuilder<T> RightJoin<TJoin>(string alias, Expression<Func<T, TJoin, bool>> joinCondition);

        /// <summary>
        /// Adds a FULL OUTER JOIN
        /// </summary>
        IJoinQueryBuilder<T> FullOuterJoin<TJoin>(Expression<Func<T, TJoin, bool>> joinCondition);

        /// <summary>
        /// Adds a FULL OUTER JOIN with alias
        /// </summary>
        IJoinQueryBuilder<T> FullOuterJoin<TJoin>(string alias, Expression<Func<T, TJoin, bool>> joinCondition);

        /// <summary>
        /// Adds a WHERE condition for any joined entity
        /// </summary>
        IJoinQueryBuilder<T> Where<TEntity>(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// Adds a WHERE condition for any joined entity with alias
        /// </summary>
        IJoinQueryBuilder<T> Where<TEntity>(string alias, Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// Adds an OR WHERE condition for any joined entity
        /// </summary>
        IJoinQueryBuilder<T> OrWhere<TEntity>(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// Adds an OR WHERE condition for any joined entity with alias
        /// </summary>
        IJoinQueryBuilder<T> OrWhere<TEntity>(string alias, Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// Adds an ORDER BY clause for any joined entity
        /// </summary>
        IJoinQueryBuilder<T> OrderBy<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> selector);

        /// <summary>
        /// Adds an ORDER BY clause for any joined entity with alias
        /// </summary>
        IJoinQueryBuilder<T> OrderBy<TEntity>(string alias, Expression<Func<TEntity, object>> selector);

        /// <summary>
        /// Adds a THEN BY clause for any joined entity
        /// </summary>
        IJoinQueryBuilder<T> ThenBy<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> selector);

        /// <summary>
        /// Adds a THEN BY clause in descending order for any joined entity
        /// </summary>
        IJoinQueryBuilder<T> ThenByDescending<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> selector);

        /// <summary>
        /// Adds a THEN BY clause for any joined entity with alias
        /// </summary>
        IJoinQueryBuilder<T> ThenBy<TEntity>(string alias, Expression<Func<TEntity, object>> selector);

        /// <summary>
        /// Adds a GROUP BY clause for any joined entity
        /// </summary>
        IJoinQueryBuilder<T> GroupBy<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> selector);

        /// <summary>
        /// Adds a HAVING clause for any joined entity
        /// </summary>
        IJoinQueryBuilder<T> Having<TEntity>(Expression<Func<TEntity, bool>> predicate);

        // Additional method overloads that return IJoinQueryBuilder<T> to maintain fluent chaining
        /// <summary>
        /// Selects specific columns using lambda expressions (JOIN version)
        /// </summary>
        new IJoinQueryBuilder<T> Column<TProperty>(Expression<Func<T, TProperty>> selector);

        /// <summary>
        /// Selects specific columns using lambda expressions with alias (JOIN version)
        /// </summary>
        new IJoinQueryBuilder<T> Column<TProperty>(Expression<Func<T, TProperty>> selector, string alias);

        /// <summary>
        /// Selects specific columns from any joined entity
        /// </summary>
        IJoinQueryBuilder<T> Column<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> selector);

        /// <summary>
        /// Selects specific columns from any joined entity with alias
        /// </summary>
        IJoinQueryBuilder<T> Column<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> selector, string alias);

        /// <summary>
        /// Selects specific columns from any joined entity with table alias
        /// </summary>
        IJoinQueryBuilder<T> Column<TEntity, TProperty>(string tableAlias, Expression<Func<TEntity, TProperty>> selector);

        /// <summary>
        /// Selects specific columns from any joined entity with table alias and column alias
        /// </summary>
        IJoinQueryBuilder<T> Column<TEntity, TProperty>(string tableAlias, Expression<Func<TEntity, TProperty>> selector, string columnAlias);

        /// <summary>
        /// Adds window function to SELECT (JOIN version)
        /// </summary>
        new IJoinQueryBuilder<T> SelectWindow(string windowFunction, string alias);

        /// <summary>
        /// Adds subquery in SELECT clause (JOIN version)
        /// </summary>
        new IJoinQueryBuilder<T> SelectSubQuery(string alias, IQueryBuilder subQuery);

        /// <summary>
        /// Adds WHERE IN clause with subquery (JOIN version)
        /// </summary>
        new IJoinQueryBuilder<T> WhereIn<TProperty, TSubQuery>(Expression<Func<T, TProperty>> selector, IQueryBuilder subQuery, Expression<Func<TSubQuery, TProperty>> subQuerySelector);

        /// <summary>
        /// Adds WHERE IN clause with collection of values (JOIN version)
        /// </summary>
        new IJoinQueryBuilder<T> WhereIn<TProperty>(Expression<Func<T, TProperty>> selector, IEnumerable<TProperty> values);

        /// <summary>
        /// Adds raw WHERE condition (JOIN version)
        /// </summary>
        new IJoinQueryBuilder<T> WhereRaw(string rawCondition);

        /// <summary>
        /// Adds OR WHERE condition (JOIN version)
        /// </summary>
        new IJoinQueryBuilder<T> OrWhere(Expression<Func<T, bool>> predicate);
    }

    /// <summary>
    /// Interface for Common Table Expression (CTE) operations
    /// </summary>
    public interface ICTEQueryBuilder : IQueryBuilder
    {
        /// <summary>
        /// Adds another CTE
        /// </summary>
        ICTEQueryBuilder WithCTE(string cteName, IQueryBuilder cteQuery);

        /// <summary>
        /// Adds a recursive CTE
        /// </summary>
        ICTEQueryBuilder WithRecursiveCTE(string cteName, IQueryBuilder anchorQuery, IQueryBuilder recursiveQuery);

        /// <summary>
        /// Creates a query from the CTEs
        /// </summary>
        IQueryBuilder Query(IQueryBuilder mainQuery);
    }

    /// <summary>
    /// Interface for aggregate operations
    /// </summary>
    public interface IAggregateQueryBuilder<T> : IQueryBuilder
    {
        /// <summary>
        /// Adds a GROUP BY clause
        /// </summary>
        IAggregateQueryBuilder<T> GroupBy<TProperty>(Expression<Func<T, TProperty>> selector);

        /// <summary>
        /// Adds a COUNT aggregate
        /// </summary>
        IAggregateQueryBuilder<T> Count(Expression<Func<T, object>> selector = null, string alias = "Count");

        /// <summary>
        /// Adds a SUM aggregate
        /// </summary>
        IAggregateQueryBuilder<T> Sum<TProperty>(Expression<Func<T, TProperty>> selector, string alias = "Sum");

        /// <summary>
        /// Adds an AVG aggregate
        /// </summary>
        IAggregateQueryBuilder<T> Average<TProperty>(Expression<Func<T, TProperty>> selector, string alias = "Average");

        /// <summary>
        /// Adds a MIN aggregate
        /// </summary>
        IAggregateQueryBuilder<T> Min<TProperty>(Expression<Func<T, TProperty>> selector, string alias = "Min");

        /// <summary>
        /// Adds a MAX aggregate
        /// </summary>
        IAggregateQueryBuilder<T> Max<TProperty>(Expression<Func<T, TProperty>> selector, string alias = "Max");

        /// <summary>
        /// Adds a HAVING clause
        /// </summary>
        IAggregateQueryBuilder<T> Having(Expression<Func<T, bool>> predicate);
    }

    /// <summary>
    /// Interface for UPDATE operations
    /// </summary>
    public interface IUpdateQueryBuilder<T> : IQueryBuilder
    {
        /// <summary>
        /// Sets a column value
        /// </summary>
        IUpdateQueryBuilder<T> Set<TProperty>(Expression<Func<T, TProperty>> selector, TProperty value);

        /// <summary>
        /// Sets a column value using an expression
        /// </summary>
        IUpdateQueryBuilder<T> Set<TProperty>(Expression<Func<T, TProperty>> selector, Expression<Func<T, TProperty>> valueExpression);

        /// <summary>
        /// Adds a WHERE condition
        /// </summary>
        IUpdateQueryBuilder<T> Where(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Adds an OR WHERE condition
        /// </summary>
        IUpdateQueryBuilder<T> OrWhere(Expression<Func<T, bool>> predicate);
    }

    /// <summary>
    /// Interface for INSERT operations
    /// </summary>
    public interface IInsertQueryBuilder<T> : IQueryBuilder
    {
        /// <summary>
        /// Sets values for insertion
        /// </summary>
        IInsertQueryBuilder<T> Values(T entity);

        /// <summary>
        /// Sets multiple values for bulk insertion
        /// </summary>
        IInsertQueryBuilder<T> Values(IEnumerable<T> entities);
    }

    /// <summary>
    /// Interface for DELETE operations
    /// </summary>
    public interface IDeleteQueryBuilder<T> : IQueryBuilder
    {
        /// <summary>
        /// Adds a WHERE condition
        /// </summary>
        IDeleteQueryBuilder<T> Where(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Adds an OR WHERE condition
        /// </summary>
        IDeleteQueryBuilder<T> OrWhere(Expression<Func<T, bool>> predicate);
    }
}
