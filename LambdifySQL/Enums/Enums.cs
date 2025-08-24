using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LambdifySQL.Enums
{
    /// <summary>
    /// Represents the order direction for ORDER BY clauses
    /// </summary>
    public enum OrderBy
    {
        ASC = 1,
        DESC = 2
    }

    /// <summary>
    /// Represents SQL command types
    /// </summary>
    public enum CMD
    {
        SELECT,
        UPDATE,
        DELETE,
        INSERT
    }

    /// <summary>
    /// Represents SQL JOIN types
    /// </summary>
    public enum JoinType
    {
        Inner,
        Left,
        Right,
        FullOuter,
        Cross
    }

    /// <summary>
    /// Represents SQL aggregate functions
    /// </summary>
    public enum AggregateFunction
    {
        Count,
        Sum,
        Avg,
        Min,
        Max,
        StdDev,
        Variance
    }

    /// <summary>
    /// Represents SQL comparison operators
    /// </summary>
    public enum ComparisonOperator
    {
        Equal,
        NotEqual,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        Like,
        NotLike,
        In,
        NotIn,
        IsNull,
        IsNotNull,
        Between,
        NotBetween
    }

    /// <summary>
    /// Represents SQL logical operators
    /// </summary>
    public enum LogicalOperator
    {
        And,
        Or,
        Not
    }

    /// <summary>
    /// Represents supported SQL dialects
    /// </summary>
    public enum SqlDialect
    {
        SqlServer,
        MySql,
        PostgreSql,
        SQLite,
        Oracle
    }

    /// <summary>
    /// Represents SQL data types
    /// </summary>
    public enum SqlDataType
    {
        // Numeric types
        TinyInt,
        SmallInt,
        Int,
        BigInt,
        Decimal,
        Numeric,
        Float,
        Real,
        Money,
        SmallMoney,

        // String types
        Char,
        VarChar,
        NChar,
        NVarChar,
        Text,
        NText,

        // Date/Time types
        Date,
        Time,
        DateTime,
        DateTime2,
        SmallDateTime,
        DateTimeOffset,
        Timestamp,

        // Binary types
        Binary,
        VarBinary,
        Image,

        // Other types
        Bit,
        UniqueIdentifier,
        Xml,
        Json,
        Geography,
        Geometry
    }
}
