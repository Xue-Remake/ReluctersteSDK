using ReluctersteSDK.PathHelper;
using ReluctersteSDK.PathHelper.Tools;
using ReluctersteSDK.SimpleSqliteORM;
using RowMod;

namespace ConsoleApp1
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // 1. 使用 PathHelper 确保路径安全
            RelativePath relativeDataDir = new RelativePath(@"Data\Databases");
            AbsolutePath? dbFolder = PhysicalPathHelper.EnsureDirectoryExists(relativeDataDir);

            if (dbFolder == null)
            {
                Console.WriteLine("创建数据库存储目录失败！");
                return;
            }

            string dbFileName = "rowmod_demo.db";

            // 2. 初始化 SqliteDataBase（不要加 await using）
            var sqliteDb = new SqliteDataBase(dbFolder.PathStr, dbFileName);
            await sqliteDb.EnsureInitializedAsync();

            // 3. 由 dbContext 统一负责异步释放（dbcontext 离开作用域时会调用 sqliteDb.DisposeAsync()）
            await using var dbContext = new RowDbContext(sqliteDb);

            RowTable userTable = dbContext["users"];

            Console.WriteLine("\n=== 1. 自动建表 ===");
            await userTable.CreateTableIfNotExistsAsync(
                ("id", "INTEGER PRIMARY KEY AUTOINCREMENT"),
                ("name", "TEXT NOT NULL"),
                ("age", "INTEGER"),
                ("email", "TEXT"),
                ("is_active", "INTEGER")
            );
            Console.WriteLine("users 表已就绪。");

            Console.WriteLine("\n=== 2. 插入单条数据 (AddAsync) ===");
            var user1 = new Row
            {
                ["name"] = "Alice",
                ["age"] = 25,
                ["email"] = "alice@example.com",
                ["is_active"] = 1
            };
            await userTable.AddAsync(user1);
            Console.WriteLine("成功插入单条数据: Alice");

            Console.WriteLine("\n=== 3. 批量插入数据 (AddRangeAsync) ===");
            var usersBatch = new[]
            {
                new Row { ["name"] = "Bob", ["age"] = 30, ["email"] = "bob@gmail.com", ["is_active"] = 1 },
                new Row { ["name"] = "Charlie", ["age"] = 17, ["email"] = "charlie@qq.com", ["is_active"] = 0 },
                new Row { ["name"] = "David", ["age"] = 28, ["email"] = "david@company.com", ["is_active"] = 1 },
                new Row { ["name"] = "Eva", ["age"] = 22, ["email"] = "eva@gmail.com", ["is_active"] = 1 }
            };
            await userTable.AddRangeAsync(usersBatch);
            Console.WriteLine("批量插入 4 条数据成功。");

            Console.WriteLine("\n=== 4. 统计与全量查询 ===");
            long totalCount = await userTable.CountAsync();
            Console.WriteLine($"当前表中总记录数: {totalCount}");

            Console.WriteLine("\n所有用户列表：");
            var allUsers = await userTable.ToListAsync();
            foreach (var row in allUsers)
            {
                PrintRow(row);
            }

            Console.WriteLine("\n=== 5. 表达式条件查询 (Where / OrderBy / Take) ===");
            var queryResult = await userTable.Where(r => r.Get<int>("age") >= 18 && r.Get<string>("email")!.Contains("gmail"))
                                             .OrderByDescending("age")
                                             .Take(2)
                                             .ToListAsync();

            Console.WriteLine("查询结果 (Age >= 18 且 Email 包含 gmail，按 Age 降序前2条)：");
            foreach (var row in queryResult)
            {
                PrintRow(row);
            }

            Console.WriteLine("\n=== 6. 单条数据查找 (FirstOrDefaultAsync) ===");
            Row? aliceRow = await userTable.Where(r => r.Get<string>("name") == "Alice").FirstOrDefaultAsync();
            if (aliceRow != null)
            {
                Console.WriteLine($"找到用户 Alice: Name={aliceRow.Get<string>("name")}, Age={aliceRow.Get<int>("age")}");
            }

            Console.WriteLine("\n=== 7. 更新数据 (UpdateAsync) ===");
            var updateFields = new Row
            {
                ["age"] = 26,
                ["email"] = "alice_new@example.com"
            };
            int updatedRows = await userTable.Where(r => r.Get<string>("name") == "Alice").UpdateAsync(updateFields);
            Console.WriteLine($"更新影响行数: {updatedRows}");

            // 验证更新
            var updatedAlice = await userTable.Where(r => r.Get<string>("name") == "Alice").FirstOrDefaultAsync();
            if (updatedAlice != null)
            {
                Console.WriteLine($"更新后的 Alice: Age={updatedAlice.Get<int>("age")}, Email={updatedAlice.Get<string>("email")}");
            }

            Console.WriteLine("\n=== 8. 删除数据 (DeleteAsync) ===");
            int deletedRows = await userTable.Where(r => r.Get<int>("age") < 18).DeleteAsync();
            Console.WriteLine($"成功删除未成年用户，影响行数: {deletedRows}");

            long remainingCount = await userTable.CountAsync();
            Console.WriteLine($"删除后剩余记录数: {remainingCount}");

            Console.WriteLine("\n用例执行完毕！");
        }

        private static void PrintRow(Row row)
        {
            long id = row.Get<long>("id");
            string name = row.Get<string>("name") ?? "N/A";
            int age = row.Get<int>("age");
            string email = row.Get<string>("email") ?? "N/A";
            int isActive = row.Get<int>("is_active");

            Console.WriteLine($"  [ID: {id}] Name: {name}, Age: {age}, Email: {email}, IsActive: {isActive}");
        }
    }
}
