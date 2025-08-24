using LambdifySQL;
using LambdifySQL.Core;
using LambdifySQL.Resolver;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyApp
{
    // Example entity classes with enhanced attributes
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

    [TableName(tableName: "Category", alias: "c")]
    public class Category
    {
        [Pk("Id")]
        [IgnoreMe]
        public int Id { get; set; }

        [Column("CategoryName")]
        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        [Column("Description")]
        [MaxLength(500)]
        public string Description { get; set; }

        [Column("IsActive")]
        [DefaultValue(true)]
        public bool IsActive { get; set; }
    }

    [TableName(tableName: "User", alias: "u")]
    public class User
    {
        [Pk("Id")]
        [IgnoreMe]
        public int Id { get; set; }

        [Column("Email")]
        [Required]
        [MaxLength(100)]
        public string Email { get; set; }

        [Column("FullName")]
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Column("CreatedAt")]
        [DefaultValue("GETDATE()")]
        public DateTime CreatedAt { get; set; }

        [Column("IsActive")]
        [DefaultValue(true)]
        public bool IsActive { get; set; }
    }

    [TableName(tableName: "UserCart", alias: "uc")]
    public class UserCart
    {
        [Pk("Id")]
        [IgnoreMe]
        public int Id { get; set; }

        [Relation(typeof(User))]
        public int UserId { get; set; }

        [Relation(typeof(Product))]
        public int ProductId { get; set; }

        [Column("Quantity")]
        public int Quantity { get; set; }

        [Column("AddedAt")]
        [DefaultValue("GETDATE()")]
        public DateTime AddedAt { get; set; }
    }

    [TableName(tableName: "ProductExtraDetails", alias: "ped")]
    public class ProductExtraDetails
    {
        [Pk("Id")]
        [IgnoreMe]
        public int Id { get; set; }

        [Column("Weight")]
        public decimal? Weight { get; set; }

        [Column("Dimensions")]
        [MaxLength(100)]
        public string Dimensions { get; set; }

        [Column("Color")]
        [MaxLength(50)]
        public string Color { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== LambdifySQL Fluent API Examples ===\n");


            var updateQuery_ = SqlQuery.Update<Product>()
                .Set(p => p.Price, 99.99m)
                .Set(p => p.CreatedAt, DateTime.Now)
                .Where(p => p.Id == 1);
            Console.WriteLine(updateQuery_.GetSql());
            Console.WriteLine($"Parameters: {string.Join(", ", updateQuery_.GetParameters().Select(p => $"@{p.Key}={p.Value}"))}\n");

            var bulkPriceUpdate = SqlQuery.Update<Product>()
                .Set(p => p.Price, p => p.Price * 1.1m) // 10% price increase
                .Where(p => p.CategoryId == 1)
                .Where(p => p.IsActive == true)
                .Where(p => p.CreatedAt < DateTime.Now.AddYears(-1));
           

            Console.WriteLine(bulkPriceUpdate.GetSql());
            Console.WriteLine($"Parameters: {string.Join(", ", bulkPriceUpdate.GetParameters().Select(p => $"@{p.Key}={p.Value}"))}\n");

            var products_ = new List<Product>
                    {
                        new Product { Name = "Keyboard", Price = 120.00m, CategoryId = 2 },
                        new Product { Name = "Monitor", Price = 350.00m, CategoryId = 3 },
                        new Product { Name = "Headset", Price = 89.99m, CategoryId = 2 }
                    };

            var bulkInsert_ = SqlQuery.BulkInsert<Product>()
                .Values(products_);
   
            Console.WriteLine(bulkInsert_.GetSql());
            Console.WriteLine($"Parameters: {string.Join(", ", bulkInsert_.GetParameters().Select(p => $"@{p.Key}={p.Value}"))}\n");

            var newProduct_ = new Product
            {
                Name = "Gaming Laptop",
                Price = 1500.00m,
                CategoryId = 1,
                IsActive = true
            };

            var insertQuery_ = SqlQuery.Insert<Product>()
                .Values(newProduct_);

            Console.WriteLine(insertQuery_.GetSql());
            Console.WriteLine($"Parameters: {string.Join(", ", insertQuery_.GetParameters().Select(p => $"@{p.Key}={p.Value}"))}\n");

            var activeProducts = SqlQuery.Select<Product>()
                  .Where(p => p.IsActive == true)
                  .Where(p => p.Price > 100)
                  .OrderBy(p => p.Name)
                  .Take(10);

            Console.WriteLine(activeProducts.GetSql());
            Console.WriteLine($"Parameters: {string.Join(", ", activeProducts.GetParameters().Select(p => $"@{p.Key}={p.Value}"))}\n");

            // SELECT with complex conditions
            var searchResults = SqlQuery.Select<Product>()
                .Where(p => p.Name.Contains("Laptop"))
                .WhereIn(p => p.CategoryId, new[] { 1, 2, 3 })
                .Where(p => p.CreatedAt >= DateTime.Now.AddMonths(-6))
                .OrderBy(p => p.Price);


            Console.WriteLine(searchResults.GetSql());
            Console.WriteLine($"Parameters: {string.Join(", ", searchResults.GetParameters().Select(p => $"@{p.Key}={p.Value}"))}\n");

            // SELECT specific columns
            var productSummary = SqlQuery.Select<Product>()
                .Column(p => p.Id)
                .Column(p => p.Name)
                .Column(p => p.Price)
                .Where(p => p.IsActive == true);


            Console.WriteLine(productSummary.GetSql());
            Console.WriteLine($"Parameters: {string.Join(", ", productSummary.GetParameters().Select(p => $"@{p.Key}={p.Value}"))}\n");




            var fullProductInfo = SqlQuery.SelectWithJoins<Product>()
                    .Column(p => p.Name, "ProductName")
                    .InnerJoin<Category>((p, c) => p.CategoryId == c.Id)
                    .Column<Category, string>(c => c.Name, "CategoryName")
                    .LeftJoin<ProductExtraDetails>((p, d) => p.Id == d.Id)
                    .Where(p => p.IsActive == true)
                    .OrderBy(p => p.Name);


            Console.WriteLine(fullProductInfo.GetSql());
            Console.WriteLine($"Parameters: {string.Join(", ", fullProductInfo.GetParameters().Select(p => $"@{p.Key}={p.Value}"))}\n");

            // Example 0: SELECT specific columns using Column<> method
            Console.WriteLine("0. SELECT specific columns with Column<> method:");
            var specificColumnsQuery = SqlQuery.Select<Product>()
                .Column(p => p.Name)
                .Column(p => p.Price, "ProductPrice")
                .Column(p => p.CategoryId)
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name);

            Console.WriteLine(specificColumnsQuery.GetSql());
            Console.WriteLine($"Parameters: {string.Join(", ", specificColumnsQuery.GetParameters().Select(p => $"@{p.Key}={p.Value}"))}\n");

            // Example 1: Basic SELECT query
            Console.WriteLine("1. Basic SELECT query:");
            var basicSelect = SqlQuery.Select<Product>(SqlDialectConfig.PostgreSql)
                .Where(p => p.Price > 100 && p.IsActive)
                .OrWhere(p => p.Qty < 5)
                .OrderBy(p => p.Name)
                .Take(10);

            Console.WriteLine(basicSelect.GetSql());
            Console.WriteLine($"Parameters: {string.Join(", ", basicSelect.GetParameters().Select(p => $"@{p.Key}={p.Value}"))}\n");

            // Example 2: Complex WHERE conditions
            Console.WriteLine("2. Complex WHERE conditions:");
            var searchTerms = new List<string> { "laptop", "phone", "tablet" };
            var categoryIds = new List<int> { 1, 2, 3 };

            var complexWhere = SqlQuery.Select<Product>()
                .Where(p => p.Name.Contains("electronics") || searchTerms.Contains(p.Name))
                .Where(p => categoryIds.Contains(p.CategoryId))
                .Where(p => p.Price >= 100 && p.Price <= 1000)
                .OrderBy(p => p.Price); // DESC

            Console.WriteLine(complexWhere.GetSql());
            Console.WriteLine($"Parameters: {string.Join(", ", complexWhere.GetParameters().Select(p => $"@{p.Key}={p.Value}"))}\n");

            // Example 2.1: WhereIn with collection of values
            Console.WriteLine("2.1. WhereIn with collection of values:");
            var targetCategoryIds = new List<int> { 1, 3, 5, 7 };
            var targetPrices = new List<decimal> { 99.99m, 199.99m, 299.99m };

            var whereInQuery = SqlQuery.Select<Product>()
                .Where(p => p.IsActive)
                .WhereIn(p => p.CategoryId, targetCategoryIds)
                .WhereIn(p => p.Price, targetPrices)
                .OrderBy(p => p.Name);

            Console.WriteLine(whereInQuery.GetSql());
            Console.WriteLine($"Parameters: {string.Join(", ", whereInQuery.GetParameters().Select(p => $"@{p.Key}={p.Value}"))}\n");

            // Example 2.2: WhereIn with empty collection (should generate 1 = 0)
            Console.WriteLine("2.2. WhereIn with empty collection:");
            var emptyIds = new List<int>();

            var emptyWhereInQuery = SqlQuery.Select<Product>()
                .Where(p => p.IsActive)
                .WhereIn(p => p.CategoryId, emptyIds);

            Console.WriteLine(emptyWhereInQuery.GetSql());
            Console.WriteLine($"Parameters: {string.Join(", ", emptyWhereInQuery.GetParameters().Select(p => $"@{p.Key}={p.Value}"))}\n");

            // Example 2.3: WhereIn with JOIN queries
            Console.WriteLine("2.3. WhereIn with JOIN queries:");
            var allowedCategoryIds = new List<int> { 1, 2, 3 };

            var joinWhereInQuery = SqlQuery.SelectWithJoins<Product>()
                .InnerJoin<Category>((p, c) => p.CategoryId == c.Id)
                .Where<Product>(p => p.IsActive)
                .WhereIn(p => p.CategoryId, allowedCategoryIds)
                .Column(p => p.Name)
                .Column(p => p.Price)
                .Column<Category, string>(c => c.Name, "CategoryName")
                .OrderBy(p => p.Name);

            Console.WriteLine(joinWhereInQuery.GetSql());
            Console.WriteLine($"Parameters: {string.Join(", ", joinWhereInQuery.GetParameters().Select(p => $"@{p.Key}={p.Value}"))}\n");

            // Example 3: UPDATE query
            Console.WriteLine("3. UPDATE query:");
            var updateQuery = SqlQuery.Update<Product>()
                .Set(p => p.Price, 199.99m)
                .Set(p => p.IsActive, true)
                .Where(p => p.CategoryId == 1 && p.Price < 200);

            Console.WriteLine(updateQuery.GetSql());
            Console.WriteLine($"Parameters: {string.Join(", ", updateQuery.GetParameters().Select(p => $"@{p.Key}={p.Value}"))}\n");

            // Example 3b: UPDATE with expression-based SET
            Console.WriteLine("3b. UPDATE with expression-based SET:");
            var updateWithExpressionQuery = SqlQuery.Update<Product>()
                .Set(p => p.Price, p => p.Price * 1.1m)  // Increase price by 10%
                .Set(p => p.Qty, p => p.Qty - 1)         // Decrease quantity by 1
                .Where(p => p.CategoryId == 1 && p.IsActive);

            Console.WriteLine(updateWithExpressionQuery.GetSql());
            Console.WriteLine($"Parameters: {string.Join(", ", updateWithExpressionQuery.GetParameters().Select(p => $"@{p.Key}={p.Value}"))}\n");

            // Example 3c: UPDATE with mathematical functions
            Console.WriteLine("3c. UPDATE with mathematical functions:");
            var updateWithMathQuery = SqlQuery.Update<Product>()
                .Set(p => p.Price, p => Math.Round(p.Price * 1.15m, 2))  // Increase by 15% and round to 2 decimals
                .Where(p => p.CategoryId == 2);

            Console.WriteLine(updateWithMathQuery.GetSql());
            Console.WriteLine($"Parameters: {string.Join(", ", updateWithMathQuery.GetParameters().Select(p => $"@{p.Key}={p.Value}"))}\n");

            // Example 4: INSERT query
            Console.WriteLine("4. INSERT query:");
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

            Console.WriteLine(insertQuery.GetSql());
            Console.WriteLine($"Parameters: {string.Join(", ", insertQuery.GetParameters().Select(p => $"@{p.Key}={p.Value}"))}\n");

            // Example 5: DELETE query
            Console.WriteLine("5. DELETE query:");
            var deleteQuery = SqlQuery.Delete<Product>()
                .Where(p => p.IsActive == false)
                .Where(p => p.CreatedAt < DateTime.Now.AddYears(-1));

            Console.WriteLine(deleteQuery.GetSql());
            Console.WriteLine($"Parameters: {string.Join(", ", deleteQuery.GetParameters().Select(p => $"@{p.Key}={p.Value}"))}\n");

            // Example 6: Raw SQL
            Console.WriteLine("6. Raw SQL example:");
            var rawQuery = SqlQuery.Raw(
                "SELECT * FROM Product WHERE Price BETWEEN @minPrice AND @maxPrice",
                new { minPrice = 100, maxPrice = 500 }
            );

            Console.WriteLine(rawQuery.Sql);
            Console.WriteLine($"Parameters: {string.Join(", ", rawQuery.Parameters.Select(p => $"@{p.Key}={p.Value}"))}\n");


            var queryAggregate = SqlQuery.Aggregate<Product>()
                        .GroupBy(p => p.CategoryId)
                        .Count(alias: "ProductCount")
                        .Sum(p => p.Price, "TotalPrice")
                        .Average(p => p.Price, "AvgPrice")
                        .Where(p => p.IsActive);

            Console.WriteLine(queryAggregate.GetSql());
            Console.WriteLine($"Parameters: {string.Join(", ", queryAggregate.GetParameters().Select(p => $"@{p.Key}={p.Value}"))}\n");




            var queryWindowFunction = SqlQuery.WindowFunction<Product>()
                .Select(p => p.Name)
                .Select(p => p.Price)
                .RowNumber(p => p.CategoryId, p => p.Price, "RowNum")
                .Rank(p => p.CategoryId, p => p.Price, "PriceRank")
                .Where(p => !p.IsActive);

            Console.WriteLine(queryWindowFunction.GetSql());
            Console.WriteLine($"Parameters: {string.Join(", ", queryWindowFunction.GetParameters().Select(p => $"@{p.Key}={p.Value}"))}\n");

            // Test boolean WHERE clauses
            Console.WriteLine("=== Boolean WHERE Clauses ===");

            // Simple boolean property
            var activeBoolQuery = SqlQuery.Select<Product>()
                .Where(p => p.IsActive);
            Console.WriteLine("Simple boolean (p.IsActive):");
            Console.WriteLine(activeBoolQuery.GetSql());
            Console.WriteLine($"Parameters: {string.Join(", ", activeBoolQuery.GetParameters().Select(p => $"@{p.Key}={p.Value}"))}\n");

            // Negated boolean property
            var inactiveBoolQuery = SqlQuery.Select<Product>()
                .Where(p => !p.IsActive);
            Console.WriteLine("Negated boolean (!p.IsActive):");
            Console.WriteLine(inactiveBoolQuery.GetSql());
            Console.WriteLine($"Parameters: {string.Join(", ", inactiveBoolQuery.GetParameters().Select(p => $"@{p.Key}={p.Value}"))}\n");

            Console.WriteLine("=== Advanced CTE Example ===");
            var cteQuery = SqlQuery.Select<Product>()
                    .Where(p => p.Qty >= 1)
                    .Top(10);

            var mainQuery = SqlQuery.Select<Product>()
                .Where(p => p.CategoryId == 2);

            var advancedQuery = SqlQuery.Advanced<Product>()
                .WithCTE("TopProducts", cteQuery)
                .Query(mainQuery);

            Console.WriteLine(advancedQuery.GetSql());
            Console.WriteLine($"Parameters: {string.Join(", ", advancedQuery.GetParameters().Select(p => $"@{p.Key}={p.Value}"))}\n");

            // ========== NEW ADVANCED EXAMPLES ==========

            Console.WriteLine("=== INNER JOIN Examples ===");

            // Example: INNER JOIN between Product and Category
            Console.WriteLine("7. INNER JOIN - Products with Categories using Column selection:");
            var innerJoinQuery = SqlQuery.SelectWithJoins<Product>()
                .Column(p => p.Name, "ProductName")
                .Column<Category, string>(c => c.Name, "CategoryName")
                .Column(p => p.Price)
                .InnerJoin<Category>((p, c) => p.CategoryId == c.Id)
                .Where<Product>(p => p.IsActive)
                .OrderBy(p => p.Name);

            Console.WriteLine(innerJoinQuery.GetSql());
            Console.WriteLine($"Parameters: {string.Join(", ", innerJoinQuery.GetParameters().Select(p => $"@{p.Key}={p.Value}"))}\n");

            // Example: Multiple JOINs with aliases
            Console.WriteLine("8. Multiple JOINs with Aliases and specific Column selection:");
            var multiJoinQuery = SqlQuery.SelectWithJoins<Product>()
                .Column(p => p.Name, "ProductName")
                .Column<Category, string>("cat", c => c.Name, "CategoryName")
                .Column<ProductExtraDetails, string>("details", d => d.Color, "ProductColor")
                .Column(p => p.Price)
                .InnerJoin<Category>("cat", (p, c) => p.CategoryId == c.Id)
                .LeftJoin<ProductExtraDetails>("details", (p, d) => p.ExtraId == d.Id)
                .Where<Product>(p => p.Price > 100)
                .OrderBy(p => p.Price)
                .ThenBy(p => p.Name);

            Console.WriteLine(multiJoinQuery.GetSql());
            Console.WriteLine($"Parameters: {string.Join(", ", multiJoinQuery.GetParameters().Select(p => $"@{p.Key}={p.Value}"))}\n");

            Console.WriteLine("=== Subquery Examples ===");

            // Example: WHERE IN with subquery
            Console.WriteLine("9. WHERE IN with Subquery:");
            var expensiveCategoriesSubquery = SqlQuery.Aggregate<Product>()
                .Where(p => p.Price > 500)
                .GroupBy(p => p.CategoryId);

            var productsInExpensiveCategoriesQuery = SqlQuery.Select<Product>()
                .WhereIn<int, Product>(p => p.CategoryId, expensiveCategoriesSubquery, sub => sub.CategoryId)
                .OrderBy(p => p.Name);

            Console.WriteLine(productsInExpensiveCategoriesQuery.GetSql());
            Console.WriteLine($"Parameters: {string.Join(", ", productsInExpensiveCategoriesQuery.GetParameters().Select(p => $"@{p.Key}={p.Value}"))}\n");

            // Example: Subquery in SELECT clause
            Console.WriteLine("10. Subquery in SELECT clause:");
            var categoryCountSubquery = SqlQuery.Aggregate<Product>()
                .Count()
                .Where(p => p.CategoryId == 1); // This would need to be dynamically bound

            var productWithCategoryCountQuery = SqlQuery.Select<Product>()
                .SelectSubQuery("CategoryProductCount", categoryCountSubquery)
                .Where(p => p.IsActive)
                .Top(5);

            Console.WriteLine(productWithCategoryCountQuery.GetSql());
            Console.WriteLine($"Parameters: {string.Join(", ", productWithCategoryCountQuery.GetParameters().Select(p => $"@{p.Key}={p.Value}"))}\n");

            Console.WriteLine("=== Advanced CTE Examples ===");

            // Example: Recursive CTE for hierarchical data
            Console.WriteLine("11. Recursive CTE Example:");
            var anchorQuery = SqlQuery.Select<Category>()
                .Where(c => c.Id == 1); // Root category

            var recursiveQuery = SqlQuery.SelectWithJoins<Category>()
                .InnerJoin<Category>((child, parent) => child.Id == parent.Id) // Simplified for demo
                .Where(c => c.IsActive);

            var recursiveCteQuery = SqlQuery.Advanced<Category>()
                .WithRecursiveCTE("CategoryHierarchy", anchorQuery, recursiveQuery)
                .Query(SqlQuery.Select<Category>().FromCTE("CategoryHierarchy"));

            Console.WriteLine(recursiveCteQuery.GetSql());
            Console.WriteLine($"Parameters: {string.Join(", ", recursiveCteQuery.GetParameters().Select(p => $"@{p.Key}={p.Value}"))}\n");

            // Example: Multiple CTEs
            Console.WriteLine("12. Multiple CTEs Example:");
            var activeProductsCte = SqlQuery.Select<Product>()
                .Where(p => p.IsActive && p.Price > 0);

            var activeCategoriesCte = SqlQuery.Select<Category>()
                .Where(c => c.IsActive);

            var finalQuery = SqlQuery.SelectWithJoins<Product>()
                .InnerJoin<Category>((p, c) => p.CategoryId == c.Id)
                .Where<Product>(p => p.Qty > 0);

            var multipleCteQuery = SqlQuery.Advanced<Product>()
                .WithCTE("ActiveProducts", activeProductsCte)
                .WithCTE("ActiveCategories", activeCategoriesCte)
                .Query(finalQuery);

            Console.WriteLine(multipleCteQuery.GetSql());
            Console.WriteLine($"Parameters: {string.Join(", ", multipleCteQuery.GetParameters().Select(p => $"@{p.Key}={p.Value}"))}\n");

            Console.WriteLine("=== Window Functions with JOINs ===");

            // Example: Window functions with joins
            Console.WriteLine("13. Window Functions with JOINs:");
            var windowWithJoinQuery = SqlQuery.SelectWithJoins<Product>()
                .InnerJoin<Category>((p, c) => p.CategoryId == c.Id)
                .SelectWindow("ROW_NUMBER() OVER (PARTITION BY p.CategoryId ORDER BY p.Price DESC)", "PriceRank")
                .SelectWindow("AVG(p.Price) OVER (PARTITION BY p.CategoryId)", "AvgCategoryPrice")
                .Where<Product>(p => p.IsActive)
                .OrderBy(p => p.CategoryId)
                .ThenBy(p => p.Price);

            Console.WriteLine(windowWithJoinQuery.GetSql());
            Console.WriteLine($"Parameters: {string.Join(", ", windowWithJoinQuery.GetParameters().Select(p => $"@{p.Key}={p.Value}"))}\n");

            Console.WriteLine("=== Complex Query with Everything ===");

            // Example: Complex query combining CTEs, JOINs, subqueries, and window functions
            Console.WriteLine("14. Ultimate Complex Query:");

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
                .Having<Product>(p => p.Price > 100) // This would typically be in HAVING with GROUP BY
                .OrderBy(p => p.CategoryId)
                .ThenBy(p => p.Price);

            var ultimateComplexQuery = SqlQuery.Advanced<Product>()
                .WithCTE("HighValueProducts", highValueProductsCte)
                .WithCTE("CategoryStats", categoryStatsCte)
                .Query(complexMainQuery);

            Console.WriteLine(ultimateComplexQuery.GetSql());
            Console.WriteLine($"Parameters: {string.Join(", ", ultimateComplexQuery.GetParameters().Select(p => $"@{p.Key}={p.Value}"))}\n");

            Console.WriteLine("=== Raw SQL with Custom JOINs ===");

            // Example: When you need complete control
            Console.WriteLine("15. Custom Raw SQL with Parameters:");
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

            Console.WriteLine(customRawQuery.Sql);
            Console.WriteLine($"Parameters: {string.Join(", ", customRawQuery.Parameters.Select(p => $"@{p.Key}={p.Value}"))}\n");


            Console.WriteLine("=== End of Examples ===");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
