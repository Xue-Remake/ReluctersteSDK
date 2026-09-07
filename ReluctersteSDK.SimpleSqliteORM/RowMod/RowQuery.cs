using Microsoft.Data.Sqlite;
using System.Linq.Expressions;
using System.Text;

namespace ReluctersteSDK.SimpleSqliteORM.RowMod
{
    /// <summary>
    /// 表示针对纯行模式（<see cref="Row"/>）的 SQL 查询构建与执行器。
    /// </summary>
    public class RowQuery
    {
        private readonly SqliteDataBase _db;
        private readonly string _tableName;

        private readonly List<string> _whereClauses = new();
        private readonly List<string> _orderByClauses = new();
        private readonly List<string> _selectColumns = new();
        private readonly List<SqliteParameter> _parameters = new();
        private int _paramCounter = 0;

        private int? _limit;
        private int? _offset;
        /// <summary>
        /// 初始化 <see cref="RowQuery"/> 实例。
        /// </summary>
        /// <param name="db">底层 SQLite 数据库操作实例。</param>
        /// <param name="tableName">要查询的表名。</param>
        public RowQuery(SqliteDataBase db, string tableName)
        {
            _db = db;
            _tableName = tableName;
        }
        /// <summary>
        /// 指定要检索的列。若不指定则默认检索全部列（*）。
        /// </summary>
        /// <param name="columns">列名数组。</param>
        /// <returns>当前的查询构建器实例。</returns>
        public RowQuery Select(params string[] columns)
        {
            _selectColumns.AddRange(columns);
            return this;
        }
        /// <summary>
        /// 添加基于表达式树的 WHERE 过滤条件。
        /// </summary>
        /// <param name="predicate">过滤条件表达式（支持二元运算、Contains 等）。</param>
        /// <returns>当前的查询构建器实例。</returns>
        public RowQuery Where(Expression<Func<Row, bool>> predicate)
        {
            var sql = ParseExpression(predicate.Body);
            if (!string.IsNullOrWhiteSpace(sql))
            {
                _whereClauses.Add(sql);
            }
            return this;
        }
        /// <summary>
        /// 指定按某一列进行升序排序（ASC）。
        /// </summary>
        /// <param name="columnName">排序的列名。</param>
        /// <returns>当前的查询构建器实例。</returns>
        public RowQuery OrderBy(string columnName)
        {
            _orderByClauses.Add($"{SqliteUtils.QuoteIdentifier(columnName)} ASC");
            return this;
        }
        /// <summary>
        /// 指定按某一列进行降序排序（DESC）。
        /// </summary>
        /// <param name="columnName">排序的列名。</param>
        /// <returns>当前的查询构建器实例。</returns>
        public RowQuery OrderByDescending(string columnName)
        {
            _orderByClauses.Add($"{SqliteUtils.QuoteIdentifier(columnName)} DESC");
            return this;
        }
        /// <summary>
        /// 跳过指定数量的记录（OFFSET）。
        /// </summary>
        /// <param name="count">跳过的行数。</param>
        /// <returns>当前的查询构建器实例。</returns>
        public RowQuery Skip(int count)
        {
            _offset = count;
            return this;
        }
        /// <summary>
        /// 限制返回的记录数量（LIMIT）。
        /// </summary>
        /// <param name="count">最大获取的行数。</param>
        /// <returns>当前的查询构建器实例。</returns>
        public RowQuery Take(int count)
        {
            _limit = count;
            return this;
        }
        /// <summary>
        /// 异步执行查询并返回匹配的行列表。
        /// </summary>
        /// <returns>包含结果行的列表。</returns>
        public async Task<List<Row>> ToListAsync()
        {
            var (sql, parameters) = BuildSelectSql();
            return await _db.ExecuteQueryAsync(sql, parameters, reader =>
            {
                var row = new Row();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var name = reader.GetName(i);
                    var val = reader.GetValue(i);
                    row[name] = val == DBNull.Value ? null : val;
                }
                return row;
            });
        }
        /// <summary>
        /// 异步执行查询并返回第一条匹配的记录；若无匹配项则返回 <c>null</c>。
        /// </summary>
        /// <returns>第一行数据或 <c>null</c>。</returns>
        public async Task<Row?> FirstOrDefaultAsync()
        {
            _limit = 1;
            var list = await ToListAsync();
            return list.FirstOrDefault();
        }
        /// <summary>
        /// 异步获取符合当前过滤条件的记录总数。
        /// </summary>
        /// <returns>记录总行数。</returns>
        public async Task<long> CountAsync()
        {
            var sb = new StringBuilder();
            sb.Append("SELECT COUNT(*) FROM ").Append(SqliteUtils.QuoteIdentifier(_tableName));

            if (_whereClauses.Count > 0)
            {
                sb.Append(" WHERE ").Append(string.Join(" AND ", _whereClauses));
            }

            var result = await _db.ExecuteScalarAsync(sb.ToString(), _parameters);
            return Convert.ToInt64(result);
        }
        /// <summary>
        /// 异步删除符合当前过滤条件的全部记录。
        /// </summary>
        /// <returns>受影响的行数。</returns>
        public async Task<int> DeleteAsync()
        {
            var sb = new StringBuilder();
            sb.Append("DELETE FROM ").Append(SqliteUtils.QuoteIdentifier(_tableName));

            if (_whereClauses.Count > 0)
            {
                sb.Append(" WHERE ").Append(string.Join(" AND ", _whereClauses));
            }

            var dict = _parameters.ToDictionary(p => p.ParameterName, p => (object?)p.Value);
            return await _db.ExecuteNonQueryAsync(sb.ToString(), dict);
        }
        /// <summary>
        /// 异步更新符合当前过滤条件的所有记录。
        /// </summary>
        /// <param name="updateData">包含待更新列及其新值的行数据。</param>
        /// <returns>受影响的行数。</returns>
        public async Task<int> UpdateAsync(Row updateData)
        {
            if (updateData == null || updateData.Count == 0) return 0;

            var sb = new StringBuilder();
            sb.Append("UPDATE ").Append(SqliteUtils.QuoteIdentifier(_tableName)).Append(" SET ");

            var setClauses = new List<string>();
            foreach (var kv in updateData)
            {
                var pName = AddParameter(kv.Value);
                setClauses.Add($"{SqliteUtils.QuoteIdentifier(kv.Key)} = {pName}");
            }
            sb.Append(string.Join(", ", setClauses));

            if (_whereClauses.Count > 0)
            {
                sb.Append(" WHERE ").Append(string.Join(" AND ", _whereClauses));
            }

            var dict = _parameters.ToDictionary(p => p.ParameterName, p => (object?)p.Value);
            return await _db.ExecuteNonQueryAsync(sb.ToString(), dict);
        }

        private (string Sql, List<SqliteParameter> Parameters) BuildSelectSql()
        {
            var sb = new StringBuilder();
            sb.Append("SELECT ");
            if (_selectColumns.Count > 0)
            {
                sb.Append(string.Join(", ", _selectColumns.Select(SqliteUtils.QuoteIdentifier)));
            }
            else
            {
                sb.Append("*");
            }

            sb.Append(" FROM ").Append(SqliteUtils.QuoteIdentifier(_tableName));

            if (_whereClauses.Count > 0)
            {
                sb.Append(" WHERE ").Append(string.Join(" AND ", _whereClauses));
            }

            if (_orderByClauses.Count > 0)
            {
                sb.Append(" ORDER BY ").Append(string.Join(", ", _orderByClauses));
            }

            if (_limit.HasValue)
            {
                sb.Append(" LIMIT ").Append(_limit.Value);
                if (_offset.HasValue)
                {
                    sb.Append(" OFFSET ").Append(_offset.Value);
                }
            }

            return (sb.ToString(), _parameters);
        }

        private string AddParameter(object? value)
        {
            var pName = $"@p{_paramCounter++}";
            var param = new SqliteParameter(pName, SqliteTypeMapper.ToDbValue(value));
            _parameters.Add(param);
            return pName;
        }

        private string ParseExpression(Expression exp)
        {
            if (exp is BinaryExpression binary)
            {
                var left = ParseExpression(binary.Left);
                var right = ParseExpression(binary.Right);

                var op = binary.NodeType switch
                {
                    ExpressionType.Equal => "=",
                    ExpressionType.NotEqual => "<>",
                    ExpressionType.GreaterThan => ">",
                    ExpressionType.GreaterThanOrEqual => ">=",
                    ExpressionType.LessThan => "<",
                    ExpressionType.LessThanOrEqual => "<=",
                    ExpressionType.AndAlso => "AND",
                    ExpressionType.OrElse => "OR",
                    _ => throw new NotSupportedException($"Unsupported expression type: {binary.NodeType}")
                };

                return $"({left} {op} {right})";
            }

            if (exp is UnaryExpression unary)
            {
                if (unary.NodeType == ExpressionType.Not)
                    return $"NOT ({ParseExpression(unary.Operand)})";

                return ParseExpression(unary.Operand);
            }

            if (exp is MethodCallExpression methodCall)
            {
                if (methodCall.Method.Name == "Get" && methodCall.Arguments.Count > 0)
                {
                    var colName = EvaluateExpression(methodCall.Arguments[0])?.ToString();
                    return SqliteUtils.QuoteIdentifier(colName!);
                }

                if (methodCall.Method.Name == nameof(string.Contains) && methodCall.Object != null)
                {
                    var col = ParseExpression(methodCall.Object);
                    var val = EvaluateExpression(methodCall.Arguments[0])?.ToString();
                    var param = AddParameter($"%{val}%");
                    return $"{col} LIKE {param}";
                }
            }

            if (exp is IndexExpression indexExp)
            {
                var colName = EvaluateExpression(indexExp.Arguments[0])?.ToString();
                return SqliteUtils.QuoteIdentifier(colName!);
            }

            var value = EvaluateExpression(exp);
            return AddParameter(value);
        }

        private static object? EvaluateExpression(Expression expr)
        {
            if (expr is ConstantExpression constant)
                return constant.Value;

            var lambda = Expression.Lambda(expr);
            return lambda.Compile().DynamicInvoke();
        }
    }
}
