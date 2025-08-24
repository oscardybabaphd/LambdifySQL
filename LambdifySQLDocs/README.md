# LambdifySQL - Fluent SQL Query Builder

A powerful, fluent C# library that converts lambda expressions to SQL queries with support for multiple database dialects and advanced SQL features.

## Features

- **Fluent API**: Intuitive method chaining for building SQL queries
- **Type-Safe**: Leverage C# type system for compile-time query validation
- **Multiple SQL Dialects**: Support for SQL Server, MySQL, PostgreSQL, and SQLite
- **Advanced Query Support**: CTEs, Window Functions, Joins, Aggregates, Subqueries, and more
- **Parameter Safety**: Automatic parameterization prevents SQL injection
- **Rich Expression Support**: Complex lambda expressions with method calls
- **OrWhere Support**: Full support for OR conditions in WHERE clauses
- **.NET 8 Ready**: Built for modern .NET with the latest language features

## Installation

Add the NuGet package to your project:

```bash
dotnet add package LambdifySQL
```

## Quick Start

```csharp
using LambdifySQL;
using LambdifySQL.Core;

var query = SqlQuery.Select<Product>()
    .Where(p => p.Price > 100 && p.IsActive)
    .OrWhere(p => p.Qty < 5)
    .OrderBy(p => p.Name)
    .Take(10);

Console.WriteLine(query.GetSql());
```

## Comprehensive Examples

### 1. Basic SELECT Query

```csharp
var basicSelect = SqlQuery.Select<Product>(SqlDialectConfig.PostgreSql)
    .Where(p => p.Price > 100 && p.IsActive)
    .OrWhere(p => p.Qty < 5)
    .OrderBy(p => p.Name)
    .Take(10);
```

**Output:**
```sql
SELECT product.*
FROM "Product" AS product
WHERE (((product."Price" > @p0) AND product."IsActive")) OR ((product."Qty" < @p1))
ORDER BY product."Name" ASC
LIMIT 10
Parameters: @p0=100, @p1=5
```

### 2. Complex WHERE Conditions

```csharp
var searchTerms = new List<string> { "laptop", "phone", "tablet" };
var categoryIds = new List<int> { 1, 2, 3 };

var complexWhere = SqlQuery.Select<Product>()
    .Where(p => p.Name.Contains("electronics") || searchTerms.Contains(p.Name))
    .Where(p => categoryIds.Contains(p.CategoryId))
    .Where(p => p.Price >= 100 && p.Price <= 1000)
    .OrderBy(p => p.Price);
```

**Output:**
```sql
SELECT product.*
FROM [Product] AS product
WHERE (product.[Name] LIKE @p0 OR product.[Name] IN (@p1, @p2, @p3)) AND product.[CategoryId] IN (@p4, @p5, @p6) AND ((product.[Price] >= @p7) AND (product.[Price] <= @p8))
ORDER BY product.[Price] ASC
Parameters: @p0=%electronics%, @p1=laptop, @p2=phone, @p3=tablet, @p4=1, @p5=2, @p6=3, @p7=100, @p8=1000
```

### 3. UPDATE Query

```csharp
var updateQuery = SqlQuery.Update<Product>()
    .Set(p => p.Price, 199.99m)
    .Set(p => p.IsActive, true)
    .Where(p => p.CategoryId == 1 && p.Price < 200);
```

**Output:**
```sql
UPDATE product
SET product.[Price] = @p0, product.[IsActive] = @p1
FROM [Product] AS product
WHERE ((product.[CategoryId] = @p2) AND (product.[Price] < @p3))
Parameters: @p0=199.99, @p1=True, @p2=1, @p3=200
```

### 4. INSERT Query

```csharp
var newProduct = new Product
{
    Name = "New Laptop",
    Qty = 10,
    Price = 999.99m,
    CategoryId = 1,
    ExtraId = 1,
    IsActive = true
};

var insertQuery = SqlQuery.Insert<Product>()
    .Values(newProduct);
```

**Output:**
```sql
INSERT INTO [Product]
([Name], [Qty], [Price], [CategoryId], [ExtraId], [CreatedAt], [IsActive])
VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6)
Parameters: @p0=New Laptop, @p1=10, @p2=999.99, @p3=1, @p4=1, @p5=01/01/0001 00:00:00, @p6=True
```

### 5. DELETE Query

```csharp
var deleteQuery = SqlQuery.Delete<Product>()
    .Where(p => p.IsActive == false)
    .Where(p => p.CreatedAt < DateTime.Now.AddYears(-1));
```

**Output:**
```sql
DELETE product
FROM [Product] AS product
WHERE (product.[IsActive] = @p0) AND (product.[CreatedAt] < DATEADD(year, @p1, @p2))
Parameters: @p0=False, @p1=-1, @p2=22/08/2025 17:34:39
```

### 6. Raw SQL Example

```csharp
var rawQuery = SqlQuery.Raw(
    "SELECT * FROM Product WHERE Price BETWEEN @minPrice AND @maxPrice",
    new { minPrice = 100, maxPrice = 500 }
);
```

**Output:**
```sql
SELECT * FROM Product WHERE Price BETWEEN @minPrice AND @maxPrice
Parameters: @minPrice=100, @maxPrice=500
```

### 7. Aggregate Queries

```csharp
var queryAggregate = SqlQuery.Aggregate<Product>()
    .GroupBy(p => p.CategoryId)
    .Count(alias: "ProductCount")
    .Sum(p => p.Price, "TotalPrice")
    .Average(p => p.Price, "AvgPrice")
    .Where(p => p.IsActive);
```

**Output:**
```sql
SELECT product.[CategoryId], COUNT(*) AS [ProductCount], SUM(product.[Price]) AS [TotalPrice], AVG(product.[Price]) AS [AvgPrice]
FROM [Product] AS product
WHERE (product.[IsActive] = @p0)
GROUP BY product.[CategoryId]
Parameters: @p0=True
```

### 8. Window Functions

```csharp
var queryWindowFunction = SqlQuery.WindowFunction<Product>()
    .Select(p => p.Name)
    .Select(p => p.Price)
    .RowNumber(p => p.CategoryId, p => p.Price, "RowNum")
    .Rank(p => p.CategoryId, p => p.Price, "PriceRank")
    .Where(p => !p.IsActive);
```

**Output:**
```sql
SELECT product.[Name], product.[Price], ROW_NUMBER() OVER (PARTITION BY product.[CategoryId] ORDER BY product.[Price]) AS [RowNum], RANK() OVER (PARTITION BY product.[CategoryId] ORDER BY product.[Price]) AS [PriceRank]
FROM [Product] AS product
WHERE (product.[IsActive] = @p0)
Parameters: @p0=False
```

### 9. Boolean WHERE Clauses

```csharp
// Simple boolean property
var activeBoolQuery = SqlQuery.Select<Product>()
    .Where(p => p.IsActive);

// Negated boolean property
var inactiveBoolQuery = SqlQuery.Select<Product>()
    .Where(p => !p.IsActive);
```

**Output:**
```sql
-- Simple boolean (p.IsActive):
SELECT product.*
FROM [Product] AS product
WHERE (product.[IsActive] = @p0)
Parameters: @p0=True

-- Negated boolean (!p.IsActive):
SELECT product.*
FROM [Product] AS product
WHERE (product.[IsActive] = @p0)
Parameters: @p0=False
```

### 10. Advanced CTE Example

```csharp
var cteQuery = SqlQuery.Select<Product>()
    .Where(p => p.Qty >= 1)
    .Top(10);

var mainQuery = SqlQuery.Select<Product>()
    .Where(p => p.CategoryId == 2);

var advancedQuery = SqlQuery.Advanced<Product>()
    .WithCTE("TopProducts", cteQuery)
    .Query(mainQuery);
```

**Output:**
```sql
WITH TopProducts AS (SELECT TOP (10) product.*
FROM [Product] AS product
WHERE (product.[Qty] >= @p0))
SELECT product.*
FROM [Product] AS product
WHERE (product.[CategoryId] = @p1)
Parameters: @p0=1, @p1=2
```

### 11. INNER JOIN - Products with Categories

```csharp
var innerJoinQuery = SqlQuery.SelectWithJoins<Product>()
    .InnerJoin<Category>((p, c) => p.CategoryId == c.Id)
    .Where<Product>(p => p.IsActive)
    .OrderBy(p => p.Name);
```

**Output:**
```sql
SELECT product.*
FROM [Product] AS product
INNER JOIN [Category] AS category ON product.Id = category.Id
WHERE (product.[IsActive] = @p0)
ORDER BY product.[Name] ASC
Parameters: @p0=True
```

### 12. Multiple JOINs with Aliases

```csharp
var multiJoinQuery = SqlQuery.SelectWithJoins<Product>()
    .InnerJoin<Category>("cat", (p, c) => p.CategoryId == c.Id)
    .LeftJoin<ProductExtraDetails>("details", (p, d) => p.ExtraId == d.Id)
    .Where<Product>(p => p.Price > 100)
    .OrderBy(p => p.Price)
    .ThenBy(p => p.Name);
```

**Output:**
```sql
SELECT product.*
FROM [Product] AS product
INNER JOIN [Category] AS cat ON product.Id = cat.Id
LEFT JOIN [ProductExtraDetails] AS details ON product.Id = details.Id
WHERE (product.[Price] > @p0)
ORDER BY product.[Price] ASC, product.[Name] ASC
Parameters: @p0=100
```

### 13. WHERE IN with Subquery

```csharp
var expensiveCategoriesSubquery = SqlQuery.Aggregate<Product>()
    .Where(p => p.Price > 500)
    .GroupBy(p => p.CategoryId);

var productsInExpensiveCategoriesQuery = SqlQuery.Select<Product>()
    .WhereIn<int, Product>(p => p.CategoryId, expensiveCategoriesSubquery, sub => sub.CategoryId)
    .OrderBy(p => p.Name);
```

**Output:**
```sql
SELECT product.*
FROM [Product] AS product
WHERE product.[CategoryId] IN (SELECT product.[CategoryId] FROM (SELECT product.[CategoryId]
FROM [Product] AS product
WHERE (product.[Price] > @p0)
GROUP BY product.[CategoryId]) subq)
ORDER BY product.[Name] ASC
Parameters: @p0=500
```

### 14. Window Functions with JOINs

```csharp
var windowWithJoinQuery = SqlQuery.SelectWithJoins<Product>()
    .InnerJoin<Category>((p, c) => p.CategoryId == c.Id)
    .SelectWindow("ROW_NUMBER() OVER (PARTITION BY p.CategoryId ORDER BY p.Price DESC)", "PriceRank")
    .SelectWindow("AVG(p.Price) OVER (PARTITION BY p.CategoryId)", "AvgCategoryPrice")
    .Where<Product>(p => p.IsActive)
    .OrderBy(p => p.CategoryId)
    .ThenBy(p => p.Price);
```

**Output:**
```sql
SELECT ROW_NUMBER() OVER (PARTITION BY p.CategoryId ORDER BY p.Price DESC) AS PriceRank, AVG(p.Price) OVER (PARTITION BY p.CategoryId) AS AvgCategoryPrice
FROM [Product] AS product
INNER JOIN [Category] AS category ON product.Id = category.Id
WHERE (product.[IsActive] = @p0)
ORDER BY product.[CategoryId] ASC, product.[Price] ASC
Parameters: @p0=True
```

### 15. Ultimate Complex Query with CTEs and JOINs

```csharp
// CTE for high-value products
var highValueProductsCte = SqlQuery.Select<Product>()
    .Where(p => p.Price > 300 && p.IsActive);

// CTE for category statistics
var categoryStatsCte = SqlQuery.Aggregate<Product>()
    .GroupBy(p => p.CategoryId)
    .Count(null, "ProductCount")
    .Average(p => p.Price, "AvgPrice")
    .Sum(p => p.Qty, "TotalQty");

// Main query with multiple joins and window functions
var complexMainQuery = SqlQuery.SelectWithJoins<Product>()
    .InnerJoin<Category>("cat", (p, c) => p.CategoryId == c.Id)
    .LeftJoin<ProductExtraDetails>("details", (p, d) => p.ExtraId == d.Id)
    .SelectWindow("RANK() OVER (PARTITION BY p.CategoryId ORDER BY p.Price DESC)", "PriceRank")
    .SelectWindow("COUNT(*) OVER (PARTITION BY p.CategoryId)", "CategoryProductCount")
    .Where<Product>(p => p.IsActive)
    .Where<Category>("cat", c => c.IsActive)
    .Having<Product>(p => p.Price > 100)
    .OrderBy(p => p.CategoryId)
    .ThenBy(p => p.Price);

var ultimateComplexQuery = SqlQuery.Advanced<Product>()
    .WithCTE("HighValueProducts", highValueProductsCte)
    .WithCTE("CategoryStats", categoryStatsCte)
    .Query(complexMainQuery);
```

**Output:**
```sql
WITH HighValueProducts AS (SELECT product.*
FROM [Product] AS product
WHERE ((product.[Price] > @p0) AND product.[IsActive])), CategoryStats AS (SELECT product.[CategoryId], COUNT(*) AS [ProductCount], AVG(product.[Price]) AS [AvgPrice], SUM(product.[Qty]) AS [TotalQty]
FROM [Product] AS product
GROUP BY product.[CategoryId])
SELECT RANK() OVER (PARTITION BY p.CategoryId ORDER BY p.Price DESC) AS PriceRank, COUNT(*) OVER (PARTITION BY p.CategoryId) AS CategoryProductCount
FROM [Product] AS product
INNER JOIN [Category] AS cat ON product.Id = cat.Id
LEFT JOIN [ProductExtraDetails] AS details ON product.Id = details.Id
WHERE (product.[IsActive] = @p1) AND (category.[IsActive] = @p1)
HAVING (product.[Price] > @p2)
ORDER BY product.[CategoryId] ASC, product.[Price] ASC
Parameters: @p0=300, @p1=True, @p2=100
```

### 16. Custom Raw SQL with Advanced Parameters

```csharp
var customRawQuery = SqlQuery.Raw(@"
    WITH RecentProducts AS (
        SELECT p.*, c.Name as CategoryName
        FROM Product p
        INNER JOIN Category c ON p.CategoryId = c.Id
        WHERE p.CreatedAt >= @startDate AND p.IsActive = @isActive
    ),
    CategoryTotals AS (
        SELECT CategoryId, COUNT(*) as ProductCount, AVG(Price) as AvgPrice
        FROM Product
        WHERE IsActive = @isActive
        GROUP BY CategoryId
    )
    SELECT rp.*, ct.ProductCount, ct.AvgPrice,
           ROW_NUMBER() OVER (PARTITION BY rp.CategoryId ORDER BY rp.Price DESC) as PriceRank
    FROM RecentProducts rp
    INNER JOIN CategoryTotals ct ON rp.CategoryId = ct.CategoryId
    WHERE rp.Price >= @minPrice
    ORDER BY rp.CategoryId, rp.Price DESC",
    new 
    { 
        startDate = DateTime.Now.AddMonths(-6),
        isActive = true,
        minPrice = 50.00m
    });
```

**Output:**
```sql
WITH RecentProducts AS (
    SELECT p.*, c.Name as CategoryName
    FROM Product p
    INNER JOIN Category c ON p.CategoryId = c.Id
    WHERE p.CreatedAt >= @startDate AND p.IsActive = @isActive
),
CategoryTotals AS (
    SELECT CategoryId, COUNT(*) as ProductCount, AVG(Price) as AvgPrice
    FROM Product
    WHERE IsActive = @isActive
    GROUP BY CategoryId
)
SELECT rp.*, ct.ProductCount, ct.AvgPrice,
       ROW_NUMBER() OVER (PARTITION BY rp.CategoryId ORDER BY rp.Price DESC) as PriceRank
FROM RecentProducts rp
INNER JOIN CategoryTotals ct ON rp.CategoryId = ct.CategoryId
WHERE rp.Price >= @minPrice
ORDER BY rp.CategoryId, rp.Price DESC
Parameters: @startDate=22/02/2025 17:34:39, @isActive=True, @minPrice=50.00
```

## Entity Configuration

Use attributes to configure your entity classes:

```csharp
[TableName(tableName: "Product", alias: "p")]
public class Product
{
    [Pk("Id")]
    [IgnoreMe]
    public int Id { get; set; }

    [Column("ProductName")]
    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    [Column("Quantity")]
    public int Qty { get; set; }

    [Column("Price")]
    public decimal Price { get; set; }

    [Relation(typeof(Category))]
    public int CategoryId { get; set; }

    [Relation(typeof(ProductExtraDetails))]
    public int ExtraId { get; set; }

    [Column("CreatedAt")]
    [DefaultValue("GETDATE()")]
    public DateTime CreatedAt { get; set; }

    [Column("IsActive")]
    [DefaultValue(true)]
    public bool IsActive { get; set; }
}
```

## Supported Attributes

- `[TableName]` - Specify table name and alias
- `[Pk]` - Mark primary key properties
- `[Column]` - Specify column name and constraints
- `[Relation]` - Define foreign key relationships
- `[IgnoreMe]` - Exclude properties from SQL generation
- `[Required]` - Mark properties as NOT NULL
- `[MaxLength]` - Specify maximum length for strings
- `[DefaultValue]` - Set default values

## SQL Dialect Support

```csharp
// SQL Server (default)
var query = SqlQuery.Select<Product>(SqlDialectConfig.SqlServer);

// MySQL
var query = SqlQuery.Select<Product>(SqlDialectConfig.MySql);

// PostgreSQL
var query = SqlQuery.Select<Product>(SqlDialectConfig.PostgreSql);

// SQLite
var query = SqlQuery.Select<Product>(SqlDialectConfig.SQLite);
```

## Advanced Features

### OrWhere Support
LambdifySQL provides full support for OR conditions:

```csharp
var query = SqlQuery.Select<Product>()
    .Where(p => p.Price > 100)
    .OrWhere(p => p.Qty < 5)
    .OrWhere(p => p.CategoryId == 1);
```

### Parameterization
All values are automatically parameterized to prevent SQL injection:

```csharp
var query = SqlQuery.Select<Product>()
    .Where(p => p.Name.Contains(userInput)); // Automatically parameterized
```

### Method Chaining
Fluent interface allows for intuitive query building:

```csharp
var query = SqlQuery.Select<Product>()
    .Where(p => p.IsActive)
    .OrWhere(p => p.Price > 100)
    .OrderBy(p => p.Name)
    .ThenBy(p => p.Price)
    .Take(10);
```

## Requirements

- .NET 8.0 or higher
- C# 12.0 language features

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.

## License

This project is licensed under the MIT License.
```

### Aggregate Queries

```csharp
var query = SqlQuery.Aggregate<Product>()
    .GroupBy(p => p.CategoryId)
    .Count(alias: "ProductCount")
    .Sum(p => p.Price, "TotalPrice")
    .Average(p => p.Price, "AvgPrice")
    .Where(p => p.IsActive);
```

### Window Functions

```csharp
var query = SqlQuery.WindowFunction<Product>()
    .Select(p => p.Name)
    .Select(p => p.Price)
    .RowNumber(p => p.CategoryId, p => p.Price, "RowNum")
    .Rank(p => p.CategoryId, p => p.Price, "PriceRank")
    .Where(p => p.IsActive);
```

### Advanced Queries with CTEs

```csharp
// Create separate queries with their own parameters
var cteQuery = SqlQuery.Select<Product>()
    .Where(p => p.CategoryId == 1)  // This creates @p0 with value 1
    .Take(10);

var mainQuery = SqlQuery.Select<Product>()
    .Where(p => p.CategoryId == 2); // This also creates @p0 with value 2

// When combined, parameter conflicts are automatically resolved
var advancedQuery = SqlQuery.Advanced<Product>()
    .WithCTE("TopProducts", cteQuery)  // CTE keeps @p0 = 1
    .Query(mainQuery);                 // Main query gets @p1 = 2

// The final SQL will have:
// WITH TopProducts AS (SELECT ... WHERE CategoryId = @p0)  -- @p0 = 1
// SELECT ... WHERE CategoryId = @p1                        -- @p1 = 2

string sql = advancedQuery.GetSql();
var parameters = advancedQuery.GetParameters(); // Contains both @p0=1 and @p1=2
```

## Entity Configuration

Use attributes to configure your entity classes:

```csharp
[TableName("Product", "p")]
public class Product
{
    [Pk("Id")]
    [IgnoreMe]
    public int Id { get; set; }

    [Column("ProductName")]
    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    [Column("Price")]
    public decimal Price { get; set; }

    [Relation(typeof(Category))]
    public int CategoryId { get; set; }

    [Index("IX_Product_CreatedAt")]
    public DateTime CreatedAt { get; set; }

    [DefaultValue(true)]
    public bool IsActive { get; set; }
}
```

## Supported Attributes

- `[TableName]` - Specify table name and alias
- `[Pk]` - Mark primary key properties
- `[Column]` - Specify column name and constraints
- `[Relation]` - Define foreign key relationships
- `[IgnoreMe]` - Exclude properties from SQL generation
- `[Required]` - Mark properties as NOT NULL
- `[MaxLength]` - Specify maximum length for strings
- `[Index]` - Define database indexes
- `[DefaultValue]` - Set default values
- `[Computed]` - Mark computed/calculated columns

## SQL Dialect Support

```csharp
// SQL Server (default)
var query = SqlQuery.Select<Product>(SqlDialectConfig.SqlServer);

// MySQL
var query = SqlQuery.Select<Product>(SqlDialectConfig.MySql);

// PostgreSQL
var query = SqlQuery.Select<Product>(SqlDialectConfig.PostgreSql);
```

## Query Validation

```csharp
var query = SqlQuery.Select<Product>()
    .Where(p => p.Name.Contains("'; DROP TABLE Product; --"));

var issues = query.Validate();
if (issues.Any())
{
    foreach (var issue in issues)
    {
        Console.WriteLine($"Security issue: {issue}");
    }
}
```

## Debug and Testing

```csharp
var query = SqlQuery.Select<Product>()
    .Where(p => p.Price > 100);

// Get SQL with debug information
Console.WriteLine(query.ToDebugString());

// Get parameterized SQL (for testing)
Console.WriteLine(query.ToParameterizedSql());

// Get SQL and parameters separately
var (sql, parameters) = query.Build();
```

## Raw SQL Support

```csharp
var rawQuery = SqlQuery.Raw(
    "SELECT * FROM Product WHERE Price BETWEEN @minPrice AND @maxPrice",
    new { minPrice = 100, maxPrice = 500 }
);
```

## Stored Procedure Calls

```csharp
var procCall = SqlQuery.StoredProcedure("GetProductsByCategory", 
    new { CategoryId = 1, IsActive = true });
```

## Requirements

- .NET 8.0 or higher
- C# 12.0 language features

## Installation

Add the NuGet package to your project:

```
dotnet add package LambdifySQL
```

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.

## License

This project is licensed under the MIT License.
