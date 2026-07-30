using ReluctersteSDK.SimpleSqliteORM;
using RowMod;
using SQLitePCL;

namespace TestProject
{
    public class RowTests
    {
        [Fact]
        public void Row字典基本操作与不区分大小写测试()
        {
            // Arrange
            var row = new Row();

            // Act
            row["Id"] = 1;
            row["Name"] = "Alice";
            row["Age"] = "25";

            // Assert
            Assert.Equal(1, row["id"]);
            Assert.Equal("Alice", row["NAME"]);
            Assert.True(row.ContainsKey("age"));
            Assert.Equal(3, row.Count);
        }
        [Fact]
        public void Row泛型Get方法类型转换测试()
        {
            // 准备
            var row = new Row
            {
                ["id"] = 100L,
                ["is_active"] = 1L,
                ["score"] = "99.5",
                ["null_val"] = null
            };

            // 断言
            Assert.Equal(100, row.Get<int>("id"));
            Assert.True(row.Get<bool>("is_active"));
            Assert.Equal(99.5m, row.Get<decimal>("score"));
            Assert.Equal(0, row.Get<int>("null_val"));
        }
    }

    public class RowDbContextAndTableTests : IAsyncLifetime
    {
        private string _dbFolder = null!;
        private string _dbName = null!;
        private SqliteDataBase _sqliteDb = null!;
        private RowDbContext _context = null!;

        public async Task InitializeAsync()
        {
            _dbFolder = Path.Combine(Path.GetTempPath(), "RowModTests_" + Guid.NewGuid().ToString("N"));
            _dbName = "test.db";

            _sqliteDb = new SqliteDataBase(_dbFolder, _dbName);
            await _sqliteDb.EnsureInitializedAsync();
            _context = new RowDbContext(_sqliteDb);
        }

        public async Task DisposeAsync()
        {
            if (_context != null)
            {
                await _context.DisposeAsync();
            }
            if (Directory.Exists(_dbFolder))
            {
                try
                {
                    Directory.Delete(_dbFolder, true);
                }
                catch { }
            }
        }

        private async Task 准备数据表(string tableName = "users")
        {
            var table = _context.Table(tableName);
            await table.CreateTableIfNotExistsAsync(
                ("id", "INTEGER PRIMARY KEY AUTOINCREMENT"),
                ("name", "TEXT"),
                ("age", "INTEGER"),
                ("email", "TEXT")
                );
        }
        [Fact]
        public async Task 自动建表与插入单行数据测试()
        {
            // 准备
            await 准备数据表();
            var table = _context["users"];

            var row = new Row
            {
                ["name"] = "张三",
                ["age"] = 18,
                ["email"] = "zhangsan@example.com"
            };

            // 执行
            int affected = await table.AddAsync(row);
            long count = await table.CountAsync();

            // 断言
            Assert.Equal(1, affected);
            Assert.Equal(1L, count);
        }
        [Fact]
        public async Task 事务中批量插入多行数据测试()
        {
            // 准备
            await 准备数据表();
            var table = _context.Table("users");

            var rows = new List<Row>
            {
                new Row { ["name"] = "用户1", ["age"] = 20 },
                new Row { ["name"] = "用户2", ["age"] = 25 },
                new Row { ["name"] = "用户3", ["age"] = 30 }
            };

            // 执行
            await table.AddRangeAsync(rows);
            long count = await table.CountAsync();

            // 断言
            Assert.Equal(3L, count);
        }
        [Fact]
        public async Task 条件查询与Contains模糊查询测试()
        {
            // 准备
            await 准备数据表();
            var table = _context.Table("users");
            await table.AddRangeAsync(new[]
            {
                new Row { ["name"] = "李四", ["age"] = 15, ["email"] = "lisi@test.com" },
                new Row { ["name"] = "王五", ["age"] = 25, ["email"] = "wangwu@test.com" },
                new Row { ["name"] = "赵六", ["age"] = 35, ["email"] = "zhaoliu@domain.com" }
            });

            // 执行：数值比较过滤
            var adults = await table.Where(r => r.Get<int>("age") >= 18).ToListAsync();

            // 执行：Contains 表达式过滤 (转换为 LIKE %test.com%)
            var testEmailUsers = await table.Where(r => r.Get<string>("email")!.Contains("test.com")).ToListAsync();

            // 断言
            Assert.Equal(2, adults.Count);
            Assert.All(adults, r => Assert.True(r.Get<int>("age") >= 18));

            Assert.Equal(2, testEmailUsers.Count);
            Assert.All(testEmailUsers, r => Assert.Contains("test.com", r.Get<string>("email")));
        }
        [Fact]
        public async Task 排序与SkipTake分页查询测试()
        {
            // 准备
            await 准备数据表();
            var table = _context.Table("users");
            await table.AddRangeAsync(new[]
            {
                new Row { ["name"] = "用户A", ["age"] = 10 },
                new Row { ["name"] = "用户B", ["age"] = 20 },
                new Row { ["name"] = "用户C", ["age"] = 30 },
                new Row { ["name"] = "用户D", ["age"] = 40 }
            });

            // 执行：按 age 降序，跳过 1 条，获取 2 条
            var result = await new RowQuery(_sqliteDb, "users")
                .OrderByDescending("age")
                .Skip(1)
                .Take(2)
                .ToListAsync();

            // 断言：降序为 40, [30], [20], 10
            Assert.Equal(2, result.Count);
            Assert.Equal("用户C", result[0]["name"]);
            Assert.Equal("用户B", result[1]["name"]);
        }

        [Fact]
        public async Task 根据条件更新数据测试()
        {
            // 准备
            await 准备数据表();
            var table = _context.Table("users");
            await table.AddRangeAsync(new[]
            {
                new Row { ["name"] = "小明", ["age"] = 17 },
                new Row { ["name"] = "小红", ["age"] = 17 }
            });

            // 执行：将 age == 17 的所有记录更新为 age = 18
            var updateData = new Row { ["age"] = 18 };
            int updatedCount = await table.Where(r => r.Get<int>("age") == 17).UpdateAsync(updateData);

            var updatedList = await table.ToListAsync();

            // 断言
            Assert.Equal(2, updatedCount);
            Assert.All(updatedList, r => Assert.Equal(18, r.Get<int>("age")));
        }

        [Fact]
        public async Task 根据条件删除数据测试()
        {
            // 准备
            await 准备数据表();
            var table = _context.Table("users");
            await table.AddRangeAsync(new[]
            {
                new Row { ["name"] = "待删除用户", ["age"] = 20 },
                new Row { ["name"] = "保留用户", ["age"] = 30 }
            });

            // 执行：删除 age == 20 的记录
            int deletedCount = await table.Where(r => r.Get<int>("age") == 20).DeleteAsync();
            long remainingCount = await table.CountAsync();
            var remainingRow = await table.Where(r => r.Get<int>("age") == 30).FirstOrDefaultAsync();

            // 断言
            Assert.Equal(1, deletedCount);
            Assert.Equal(1L, remainingCount);
            Assert.NotNull(remainingRow);
            Assert.Equal("保留用户", remainingRow!["name"]);
        }
    }
}
