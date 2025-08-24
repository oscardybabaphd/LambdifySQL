// Examples Manager - Loads and renders examples from embedded data
class ExamplesManager {
    constructor() {
        this.examples = [
            {
                "id": 1,
                "title": "Basic SELECT Query",
                "description": "Simple SELECT with WHERE conditions and ORDER BY",
                "csharpCode": "var basicSelect = SqlQuery.Select<Product>(SqlDialectConfig.PostgreSql)\n    .Where(p => p.Price > 100 && p.IsActive)\n    .OrWhere(p => p.Qty < 5)\n    .OrderBy(p => p.Name)\n    .Take(10);",
                "sqlOutput": "SELECT product.*\nFROM \"Product\" AS product\nWHERE (((product.\"Price\" > @p0) AND product.\"IsActive\")) OR ((product.\"Qty\" < @p1))\nORDER BY product.\"Name\" ASC\nLIMIT 10\nParameters: @p0=100, @p1=5"
            },
            {
                "id": 2,
                "title": "Complex WHERE Conditions",
                "description": "Multiple WHERE conditions with collections and ranges",
                "csharpCode": "var searchTerms = new List<string> { \"laptop\", \"phone\", \"tablet\" };\nvar categoryIds = new List<int> { 1, 2, 3 };\n\nvar complexWhere = SqlQuery.Select<Product>()\n    .Where(p => p.Name.Contains(\"electronics\") || searchTerms.Contains(p.Name))\n    .Where(p => categoryIds.Contains(p.CategoryId))\n    .Where(p => p.Price >= 100 && p.Price <= 1000)\n    .OrderBy(p => p.Price);",
                "sqlOutput": "SELECT product.*\nFROM [Product] AS product\nWHERE (product.[Name] LIKE @p0 OR product.[Name] IN (@p1, @p2, @p3)) AND product.[CategoryId] IN (@p4, @p5, @p6) AND ((product.[Price] >= @p7) AND (product.[Price] <= @p8))\nORDER BY product.[Price] ASC\nParameters: @p0=%electronics%, @p1=laptop, @p2=phone, @p3=tablet, @p4=1, @p5=2, @p6=3, @p7=100, @p8=1000"
            },
            {
                "id": 3,
                "title": "UPDATE Query",
                "description": "Update records with conditional WHERE clause",
                "csharpCode": "var updateQuery = SqlQuery.Update<Product>()\n    .Set(p => p.Price, 199.99m)\n    .Set(p => p.IsActive, true)\n    .Where(p => p.CategoryId == 1 && p.Price < 200);",
                "sqlOutput": "UPDATE product\nSET product.[Price] = @p0, product.[IsActive] = @p1\nFROM [Product] AS product\nWHERE ((product.[CategoryId] = @p2) AND (product.[Price] < @p3))\nParameters: @p0=199.99, @p1=True, @p2=1, @p3=200"
            },
            {
                "id": 4,
                "title": "INSERT Query",
                "description": "Insert new records with entity objects",
                "csharpCode": "var newProduct = new Product\n{\n    Name = \"New Laptop\",\n    Qty = 10,\n    Price = 999.99m,\n    CategoryId = 1,\n    ExtraId = 1,\n    IsActive = true\n};\n\nvar insertQuery = SqlQuery.Insert<Product>()\n    .Values(newProduct);",
                "sqlOutput": "INSERT INTO [Product]\n([Name], [Qty], [Price], [CategoryId], [ExtraId], [CreatedAt], [IsActive])\nVALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6)\nParameters: @p0=New Laptop, @p1=10, @p2=999.99, @p3=1, @p4=1, @p5=01/01/0001 00:00:00, @p6=True"
            },
            {
                "id": 5,
                "title": "DELETE Query",
                "description": "Delete records with multiple conditions",
                "csharpCode": "var deleteQuery = SqlQuery.Delete<Product>()\n    .Where(p => p.IsActive == false)\n    .Where(p => p.CreatedAt < DateTime.Now.AddYears(-1));",
                "sqlOutput": "DELETE product\nFROM [Product] AS product\nWHERE (product.[IsActive] = @p0) AND (product.[CreatedAt] < DATEADD(year, @p1, @p2))\nParameters: @p0=False, @p1=-1, @p2=22/08/2025 17:34:39"
            },
            {
                "id": 6,
                "title": "Raw SQL Example",
                "description": "Execute raw SQL with parameters",
                "csharpCode": "var rawQuery = SqlQuery.Raw(\n    \"SELECT * FROM Product WHERE Price BETWEEN @minPrice AND @maxPrice\",\n    new { minPrice = 100, maxPrice = 500 }\n);",
                "sqlOutput": "SELECT * FROM Product WHERE Price BETWEEN @minPrice AND @maxPrice\nParameters: @minPrice=100, @maxPrice=500"
            },
            {
                "id": 7,
                "title": "Aggregate Queries",
                "description": "GROUP BY with aggregate functions",
                "csharpCode": "var queryAggregate = SqlQuery.Aggregate<Product>()\n    .GroupBy(p => p.CategoryId)\n    .Count(alias: \"ProductCount\")\n    .Sum(p => p.Price, \"TotalPrice\")\n    .Average(p => p.Price, \"AvgPrice\")\n    .Where(p => p.IsActive);",
                "sqlOutput": "SELECT product.[CategoryId], COUNT(*) AS [ProductCount], SUM(product.[Price]) AS [TotalPrice], AVG(product.[Price]) AS [AvgPrice]\nFROM [Product] AS product\nWHERE (product.[IsActive] = @p0)\nGROUP BY product.[CategoryId]\nParameters: @p0=True"
            },
            {
                "id": 8,
                "title": "Window Functions",
                "description": "Advanced window functions with partitioning",
                "csharpCode": "var queryWindowFunction = SqlQuery.WindowFunction<Product>()\n    .Select(p => p.Name)\n    .Select(p => p.Price)\n    .RowNumber(p => p.CategoryId, p => p.Price, \"RowNum\")\n    .Rank(p => p.CategoryId, p => p.Price, \"PriceRank\")\n    .Where(p => !p.IsActive);",
                "sqlOutput": "SELECT product.[Name], product.[Price], ROW_NUMBER() OVER (PARTITION BY product.[CategoryId] ORDER BY product.[Price]) AS [RowNum], RANK() OVER (PARTITION BY product.[CategoryId] ORDER BY product.[Price]) AS [PriceRank]\nFROM [Product] AS product\nWHERE (product.[IsActive] = @p0)\nParameters: @p0=False"
            },
            {
                "id": 9,
                "title": "Boolean WHERE Clauses",
                "description": "Simple and negated boolean conditions",
                "csharpCode": "// Simple boolean property\nvar activeBoolQuery = SqlQuery.Select<Product>()\n    .Where(p => p.IsActive);\n\n// Negated boolean property\nvar inactiveBoolQuery = SqlQuery.Select<Product>()\n    .Where(p => !p.IsActive);",
                "sqlOutput": "-- Simple boolean (p.IsActive):\nSELECT product.*\nFROM [Product] AS product\nWHERE (product.[IsActive] = @p0)\nParameters: @p0=True\n\n-- Negated boolean (!p.IsActive):\nSELECT product.*\nFROM [Product] AS product\nWHERE (product.[IsActive] = @p0)\nParameters: @p0=False"
            },
            {
                "id": 10,
                "title": "Advanced CTE Example",
                "description": "Common Table Expressions with complex queries",
                "csharpCode": "var cteQuery = SqlQuery.Select<Product>()\n    .Where(p => p.Qty >= 1)\n    .Top(10);\n\nvar mainQuery = SqlQuery.Select<Product>()\n    .Where(p => p.CategoryId == 2);\n\nvar advancedQuery = SqlQuery.Advanced<Product>()\n    .WithCTE(\"TopProducts\", cteQuery)\n    .Query(mainQuery);",
                "sqlOutput": "WITH TopProducts AS (SELECT TOP (10) product.*\nFROM [Product] AS product\nWHERE (product.[Qty] >= @p0))\nSELECT product.*\nFROM [Product] AS product\nWHERE (product.[CategoryId] = @p1)\nParameters: @p0=1, @p1=2"
            },
            {
                "id": 11,
                "title": "INNER JOIN - Products with Categories",
                "description": "Simple INNER JOIN between two tables",
                "csharpCode": "var innerJoinQuery = SqlQuery.SelectWithJoins<Product>()\n    .InnerJoin<Category>((p, c) => p.CategoryId == c.Id)\n    .Where<Product>(p => p.IsActive)\n    .OrderBy(p => p.Name);",
                "sqlOutput": "SELECT product.*\nFROM [Product] AS product\nINNER JOIN [Category] AS category ON product.Id = category.Id\nWHERE (product.[IsActive] = @p0)\nORDER BY product.[Name] ASC\nParameters: @p0=True"
            },
            {
                "id": 12,
                "title": "Multiple JOINs with Aliases",
                "description": "Multiple JOIN operations with custom aliases",
                "csharpCode": "var multiJoinQuery = SqlQuery.SelectWithJoins<Product>()\n    .InnerJoin<Category>(\"cat\", (p, c) => p.CategoryId == c.Id)\n    .LeftJoin<ProductExtraDetails>(\"details\", (p, d) => p.ExtraId == d.Id)\n    .Where<Product>(p => p.Price > 100)\n    .OrderBy(p => p.Price)\n    .ThenBy(p => p.Name);",
                "sqlOutput": "SELECT product.*\nFROM [Product] AS product\nINNER JOIN [Category] AS cat ON product.Id = cat.Id\nLEFT JOIN [ProductExtraDetails] AS details ON product.Id = details.Id\nWHERE (product.[Price] > @p0)\nORDER BY product.[Price] ASC, product.[Name] ASC\nParameters: @p0=100"
            },
            {
                "id": 13,
                "title": "WHERE IN with Subquery",
                "description": "Subquery with WHERE IN condition",
                "csharpCode": "var expensiveCategoriesSubquery = SqlQuery.Aggregate<Product>()\n    .Where(p => p.Price > 500)\n    .GroupBy(p => p.CategoryId);\n\nvar productsInExpensiveCategoriesQuery = SqlQuery.Select<Product>()\n    .WhereIn<int, Product>(p => p.CategoryId, expensiveCategoriesSubquery, sub => sub.CategoryId)\n    .OrderBy(p => p.Name);",
                "sqlOutput": "SELECT product.*\nFROM [Product] AS product\nWHERE product.[CategoryId] IN (SELECT product.[CategoryId] FROM (SELECT product.[CategoryId]\nFROM [Product] AS product\nWHERE (product.[Price] > @p0)\nGROUP BY product.[CategoryId]) subq)\nORDER BY product.[Name] ASC\nParameters: @p0=500"
            },
            {
                "id": 14,
                "title": "Window Functions with JOINs",
                "description": "Window functions combined with JOIN operations",
                "csharpCode": "var windowWithJoinQuery = SqlQuery.SelectWithJoins<Product>()\n    .InnerJoin<Category>((p, c) => p.CategoryId == c.Id)\n    .SelectWindow(\"ROW_NUMBER() OVER (PARTITION BY p.CategoryId ORDER BY p.Price DESC)\", \"PriceRank\")\n    .SelectWindow(\"AVG(p.Price) OVER (PARTITION BY p.CategoryId)\", \"AvgCategoryPrice\")\n    .Where<Product>(p => p.IsActive)\n    .OrderBy(p => p.CategoryId)\n    .ThenBy(p => p.Price);",
                "sqlOutput": "SELECT ROW_NUMBER() OVER (PARTITION BY p.CategoryId ORDER BY p.Price DESC) AS PriceRank, AVG(p.Price) OVER (PARTITION BY p.CategoryId) AS AvgCategoryPrice\nFROM [Product] AS product\nINNER JOIN [Category] AS category ON product.Id = category.Id\nWHERE (product.[IsActive] = @p0)\nORDER BY product.[CategoryId] ASC, product.[Price] ASC\nParameters: @p0=True"
            },
            {
                "id": 15,
                "title": "Ultimate Complex Query with CTEs and JOINs",
                "description": "Complex query combining CTEs, JOINs, and window functions",
                "csharpCode": "// CTE for high-value products\nvar highValueProductsCte = SqlQuery.Select<Product>()\n    .Where(p => p.Price > 300 && p.IsActive);\n\n// CTE for category statistics\nvar categoryStatsCte = SqlQuery.Aggregate<Product>()\n    .GroupBy(p => p.CategoryId)\n    .Count(null, \"ProductCount\")\n    .Average(p => p.Price, \"AvgPrice\")\n    .Sum(p => p.Qty, \"TotalQty\");\n\n// Main query with multiple joins and window functions\nvar complexMainQuery = SqlQuery.SelectWithJoins<Product>()\n    .InnerJoin<Category>(\"cat\", (p, c) => p.CategoryId == c.Id)\n    .LeftJoin<ProductExtraDetails>(\"details\", (p, d) => p.ExtraId == d.Id)\n    .SelectWindow(\"RANK() OVER (PARTITION BY p.CategoryId ORDER BY p.Price DESC)\", \"PriceRank\")\n    .SelectWindow(\"COUNT(*) OVER (PARTITION BY p.CategoryId)\", \"CategoryProductCount\")\n    .Where<Product>(p => p.IsActive)\n    .Where<Category>(\"cat\", c => c.IsActive)\n    .Having<Product>(p => p.Price > 100)\n    .OrderBy(p => p.CategoryId)\n    .ThenBy(p => p.Price);\n\nvar ultimateComplexQuery = SqlQuery.Advanced<Product>()\n    .WithCTE(\"HighValueProducts\", highValueProductsCte)\n    .WithCTE(\"CategoryStats\", categoryStatsCte)\n    .Query(complexMainQuery);",
                "sqlOutput": "WITH HighValueProducts AS (SELECT product.*\nFROM [Product] AS product\nWHERE ((product.[Price] > @p0) AND product.[IsActive])), CategoryStats AS (SELECT product.[CategoryId], COUNT(*) AS [ProductCount], AVG(product.[Price]) AS [AvgPrice], SUM(product.[Qty]) AS [TotalQty]\nFROM [Product] AS product\nGROUP BY product.[CategoryId])\nSELECT RANK() OVER (PARTITION BY p.CategoryId ORDER BY p.Price DESC) AS PriceRank, COUNT(*) OVER (PARTITION BY p.CategoryId) AS CategoryProductCount\nFROM [Product] AS product\nINNER JOIN [Category] AS cat ON product.Id = cat.Id\nLEFT JOIN [ProductExtraDetails] AS details ON product.Id = details.Id\nWHERE (product.[IsActive] = @p1) AND (category.[IsActive] = @p1)\nHAVING (product.[Price] > @p2)\nORDER BY product.[CategoryId] ASC, product.[Price] ASC\nParameters: @p0=300, @p1=True, @p2=100"
            },
            {
                "id": 16,
                "title": "Custom Raw SQL with Advanced Parameters",
                "description": "Complex raw SQL with CTEs, window functions, and parameters",
                "csharpCode": "var customRawQuery = SqlQuery.Raw(@\"\n    WITH RecentProducts AS (\n        SELECT p.*, c.Name as CategoryName\n        FROM Product p\n        INNER JOIN Category c ON p.CategoryId = c.Id\n        WHERE p.CreatedAt >= @startDate AND p.IsActive = @isActive\n    ),\n    CategoryTotals AS (\n        SELECT CategoryId, COUNT(*) as ProductCount, AVG(Price) as AvgPrice\n        FROM Product\n        WHERE IsActive = @isActive\n        GROUP BY CategoryId\n    )\n    SELECT rp.*, ct.ProductCount, ct.AvgPrice,\n           ROW_NUMBER() OVER (PARTITION BY rp.CategoryId ORDER BY rp.Price DESC) as PriceRank\n    FROM RecentProducts rp\n    INNER JOIN CategoryTotals ct ON rp.CategoryId = ct.CategoryId\n    WHERE rp.Price >= @minPrice\n    ORDER BY rp.CategoryId, rp.Price DESC\",\n    new \n    { \n        startDate = DateTime.Now.AddMonths(-6),\n        isActive = true,\n        minPrice = 50.00m\n    });",
                "sqlOutput": "WITH RecentProducts AS (\n    SELECT p.*, c.Name as CategoryName\n    FROM Product p\n    INNER JOIN Category c ON p.CategoryId = c.Id\n    WHERE p.CreatedAt >= @startDate AND p.IsActive = @isActive\n),\nCategoryTotals AS (\n    SELECT CategoryId, COUNT(*) as ProductCount, AVG(Price) as AvgPrice\n    FROM Product\n    WHERE IsActive = @isActive\n    GROUP BY CategoryId\n)\nSELECT rp.*, ct.ProductCount, ct.AvgPrice,\n       ROW_NUMBER() OVER (PARTITION BY rp.CategoryId ORDER BY rp.Price DESC) as PriceRank\nFROM RecentProducts rp\nINNER JOIN CategoryTotals ct ON rp.CategoryId = ct.CategoryId\nWHERE rp.Price >= @minPrice\nORDER BY rp.CategoryId, rp.Price DESC\nParameters: @startDate=22/02/2025 17:34:39, @isActive=True, @minPrice=50.00"
            }
        ];
        this.currentFilter = 'all';
    }

    // Load examples from embedded data
    async loadExamples() {
        try {
            this.renderExamples();
        } catch (error) {
            console.error('Error loading examples:', error);
            this.showError('Failed to load examples.');
        }
    }

    // Render all examples to the DOM
    renderExamples(filter = 'all') {
        const container = document.getElementById('examples-container');
        if (!container) {
            console.error('Examples container not found');
            return;
        }

        const filteredExamples = this.filterExamples(filter);
        container.innerHTML = '';

        filteredExamples.forEach(example => {
            const exampleElement = this.createExampleElement(example);
            container.appendChild(exampleElement);
        });

        // Re-initialize syntax highlighting
        if (typeof Prism !== 'undefined') {
            Prism.highlightAll();
        }

        // Re-initialize copy buttons
        this.initializeCopyButtons();
    }

    // Filter examples based on category or complexity
    filterExamples(filter) {
        if (filter === 'all') return this.examples;
        
        const basicExamples = [1, 2, 3, 4, 5, 6, 7]; // Basic CRUD and simple queries
        const advancedExamples = [8, 9, 10, 11, 12, 13, 14, 15, 16]; // Advanced features
        
        if (filter === 'basic') {
            return this.examples.filter(ex => basicExamples.includes(ex.id));
        } else if (filter === 'advanced') {
            return this.examples.filter(ex => advancedExamples.includes(ex.id));
        }
        
        return this.examples;
    }

    // Create DOM element for a single example
    createExampleElement(example) {
        const div = document.createElement('div');
        div.className = 'example-item';
        div.id = `example-${example.id}`;
        
        div.innerHTML = `
            <h3 class="example-title">
                <span class="example-number">${example.id}</span>
                ${example.title}
            </h3>
            
            <div class="example-description">
                ${example.description}
            </div>
            
            <div class="example-container">
                <div class="code-example">
                    <div class="code-header">
                        <span class="code-language">C# Code</span>
                        <button class="copy-btn" data-clipboard-target="#example-${example.id}-code">
                            <i class="fas fa-copy"></i>
                        </button>
                    </div>
                    <div class="code-editor">
                        <pre class="line-numbers"><code id="example-${example.id}-code" class="language-csharp">${this.escapeHtml(example.csharpCode)}</code></pre>
                    </div>
                </div>
                
                <div class="output-example">
                    <div class="output-header">
                        <span class="output-language">SQL Output</span>
                        <button class="copy-btn" data-clipboard-target="#example-${example.id}-output">
                            <i class="fas fa-copy"></i>
                        </button>
                    </div>
                    <div class="output-editor">
                        <pre class="line-numbers"><code id="example-${example.id}-output" class="language-sql">${this.escapeHtml(example.sqlOutput)}</code></pre>
                    </div>
                </div>
            </div>
        `;
        
        return div;
    }

    // Initialize copy to clipboard functionality
    initializeCopyButtons() {
        const copyButtons = document.querySelectorAll('.copy-btn');
        copyButtons.forEach(button => {
            button.addEventListener('click', (e) => {
                e.preventDefault();
                const targetId = button.getAttribute('data-clipboard-target');
                const targetElement = document.querySelector(targetId);
                
                if (targetElement) {
                    const text = targetElement.textContent;
                    this.copyToClipboard(text, button);
                }
            });
        });
    }

    // Copy text to clipboard with visual feedback
    async copyToClipboard(text, button) {
        try {
            await navigator.clipboard.writeText(text);
            
            // Visual feedback
            const originalIcon = button.innerHTML;
            button.innerHTML = '<i class="fas fa-check"></i>';
            button.style.color = '#28a745';
            
            setTimeout(() => {
                button.innerHTML = originalIcon;
                button.style.color = '';
            }, 2000);
            
        } catch (err) {
            console.error('Failed to copy text: ', err);
            
            // Fallback for older browsers
            const textArea = document.createElement('textarea');
            textArea.value = text;
            document.body.appendChild(textArea);
            textArea.select();
            
            try {
                document.execCommand('copy');
                // Visual feedback for fallback
                button.innerHTML = '<i class="fas fa-check"></i>';
                button.style.color = '#28a745';
                
                setTimeout(() => {
                    button.innerHTML = '<i class="fas fa-copy"></i>';
                    button.style.color = '';
                }, 2000);
            } catch (fallbackErr) {
                console.error('Fallback copy failed: ', fallbackErr);
            }
            
            document.body.removeChild(textArea);
        }
    }

    // Initialize filter functionality
    initializeFilters() {
        const filterButtons = document.querySelectorAll('.filter-btn');
        filterButtons.forEach(button => {
            button.addEventListener('click', (e) => {
                e.preventDefault();
                
                // Update active filter button
                filterButtons.forEach(btn => btn.classList.remove('active'));
                button.classList.add('active');
                
                // Get filter value and render
                const filter = button.getAttribute('data-filter');
                this.currentFilter = filter;
                this.renderExamples(filter);
            });
        });
    }

    // Initialize search functionality
    initializeSearch() {
        const searchInput = document.getElementById('examples-search');
        if (searchInput) {
            searchInput.addEventListener('input', (e) => {
                this.searchExamples(e.target.value);
            });
        }
    }

    // Search through examples
    searchExamples(query) {
        if (!query.trim()) {
            this.renderExamples(this.currentFilter);
            return;
        }

        const searchTerm = query.toLowerCase();
        const filteredExamples = this.examples.filter(example => {
            return example.title.toLowerCase().includes(searchTerm) ||
                   example.description.toLowerCase().includes(searchTerm) ||
                   example.csharpCode.toLowerCase().includes(searchTerm) ||
                   example.sqlOutput.toLowerCase().includes(searchTerm);
        });

        this.renderFilteredExamples(filteredExamples);
    }

    // Render specific set of examples
    renderFilteredExamples(examples) {
        const container = document.getElementById('examples-container');
        if (!container) return;

        container.innerHTML = '';

        if (examples.length === 0) {
            container.innerHTML = '<div class="no-results">No examples found matching your search.</div>';
            return;
        }

        examples.forEach(example => {
            const exampleElement = this.createExampleElement(example);
            container.appendChild(exampleElement);
        });

        // Re-initialize syntax highlighting and copy buttons
        if (typeof Prism !== 'undefined') {
            Prism.highlightAll();
        }
        this.initializeCopyButtons();
    }

    // Show error message
    showError(message) {
        const container = document.getElementById('examples-container');
        if (container) {
            container.innerHTML = `<div class="error-message">${message}</div>`;
        }
    }

    // Escape HTML characters
    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    // Initialize all functionality
    async init() {
        await this.loadExamples();
        this.initializeFilters();
        this.initializeSearch();
        
        // Add smooth scrolling to example links
        this.initializeSmoothScrolling();
    }

    // Initialize smooth scrolling for example navigation
    initializeSmoothScrolling() {
        document.querySelectorAll('a[href^="#example-"]').forEach(link => {
            link.addEventListener('click', (e) => {
                e.preventDefault();
                const targetId = link.getAttribute('href').substring(1);
                const targetElement = document.getElementById(targetId);
                
                if (targetElement) {
                    targetElement.scrollIntoView({
                        behavior: 'smooth',
                        block: 'start'
                    });
                }
            });
        });
    }
}

// Initialize examples manager when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    const examplesManager = new ExamplesManager();
    examplesManager.init();
    
    // Make it globally available for debugging
    window.examplesManager = examplesManager;
});
