using ReluctersteSDK.Paths.Tools;
using System.Text;

namespace ReluctersteSDK.Paths
{
    /// <summary>
    /// 路径抽象基类：由 <see cref="PathSegment"/> 数组/列表驱动。
    /// </summary>
    public abstract class FdPath : IEquatable<FdPath>
    {
        private readonly PathSegment[] _segments;

        /// <summary>
        /// 路径元数组（对外只读）
        /// </summary>
        public IReadOnlyList<PathSegment> Segments => _segments;

        /// <summary>
        /// 格式化后的路径字符串
        /// </summary>
        public string PathStr => ToString();

        /// <summary>
        /// 使用路径元集合初始化 <see cref="FdPath"/> 类的新实例。
        /// </summary>
        /// <param name="segments">路径元集合。</param>
        /// <exception cref="ArgumentNullException">当 <paramref name="segments"/> 为 null 时抛出。</exception>
        protected FdPath(IEnumerable<PathSegment> segments)
        {
            ArgumentNullException.ThrowIfNull(segments);
            _segments = segments.ToArray();
        }

        /// <summary>
        /// 运算符 * ：组合/拼接两个路径对象。
        /// 例如："A" * "./B/C" => "A/B/C"
        /// </summary>
        /// <param name="left">基础路径。</param>
        /// <param name="right">待追加的路径。</param>
        /// <returns>组合后的新路径。</returns>
        /// <exception cref="ArgumentNullException">当 <paramref name="left"/> 或 <paramref name="right"/> 为 null 时抛出。</exception>
        public static FdPath operator *(FdPath left, FdPath right)
        {
            ArgumentNullException.ThrowIfNull(left);
            ArgumentNullException.ThrowIfNull(right);
            return Combine(left, right);
        }
        /// <summary>
        /// 运算符 * ：将路径字符串拼接到路径对象后。
        /// </summary>
        /// <param name="left">基础路径。</param>
        /// <param name="right">待追加的路径字符串。</param>
        /// <returns>组合后的新路径。</returns>
        public static FdPath operator *(FdPath left, string right)
        {
            return left * Parse(right);
        }
        /// <summary>
        /// 运算符 * ：将基础路径字符串与路径对象拼接。
        /// </summary>
        /// <param name="left">基础路径字符串。</param>
        /// <param name="right">待追加的路径。</param>
        /// <returns>组合后的新路径。</returns>
        public static FdPath operator *(string left, FdPath right)
        {
            return Parse(left) * right;
        }

        /// <summary>
        /// 运算符 / ：计算目标路径相对于基准路径的相对路径 (target / base)。
        /// 例如："A/B/C" / "A/B" => "./C"
        /// </summary>
        /// <param name="target">目标路径。</param>
        /// <param name="base">基准路径。</param>
        /// <returns>目标路径相对于基准路径的 <see cref="RelativePath"/>。</returns>
        /// <exception cref="ArgumentNullException">当 <paramref name="target"/> 或 <paramref name="base"/> 为 null 时抛出。</exception>
        /// <exception cref="InvalidOperationException">当两者为跨盘符/跨根目录绝对路径无法计算相对路径时抛出。</exception>
        public static RelativePath operator /(FdPath target, FdPath @base)
        {
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(@base);
            return MakeRelative(target, @base);
        }
        /// <summary>
        /// 运算符 / ：计算目标路径相对于基准路径字符串的相对路径。
        /// </summary>
        /// <param name="target">目标路径。</param>
        /// <param name="base">基准路径字符串。</param>
        /// <returns>相对路径实例。</returns>
        public static RelativePath operator /(FdPath target, string @base)
        {
            return target / Parse(@base);
        }
        /// <summary>
        /// 运算符 / ：计算目标路径字符串相对于基准路径的相对路径。
        /// </summary>
        /// <param name="target">目标路径字符串。</param>
        /// <param name="base">基准路径。</param>
        /// <returns>相对路径实例。</returns>
        public static RelativePath operator /(string target, FdPath @base)
        {
            return Parse(target) / @base;
        }
        /// <summary>
        /// 解析路径字符串，自动识别并构造 <see cref="AbsolutePath"/> 或 <see cref="RelativePath"/>。
        /// </summary>
        /// <param name="path">待解析的路径字符串。</param>
        /// <returns>解析出的 <see cref="FdPath"/> 具体子类实例。</returns>
        public static FdPath Parse(string path)
        {
            if (PathValidator.IsValidAbsolutePath(path))
            {
                return new AbsolutePath(path);
            }
            return new RelativePath(path);
        }

        /// <summary>
        /// 执行路径组合与向上/向下回溯逻辑。
        /// </summary>
        /// <param name="left">左侧基础路径。</param>
        /// <param name="right">右侧待追加路径。</param>
        /// <returns>组合规约后的新路径实例。</returns>
        protected static FdPath Combine(FdPath left, FdPath right)
        {
            // 如果右操作数是绝对路径，组合结果直接为右操作数
            if (right is AbsolutePath) return right;

            var resultSegments = new List<PathSegment>(left.Segments);

            foreach (var seg in right.Segments)
            {
                if (seg.Kind == PathSegmentKind.Current)
                {
                    continue; // 忽略 "."
                }
                else if (seg.Kind == PathSegmentKind.Parent)
                {
                    // ".." 弹出上一级目录（如果上一级不是根且可弹出）
                    if (resultSegments.Count > 0 &&
                        resultSegments[^1].Kind != PathSegmentKind.Root &&
                        resultSegments[^1].Kind != PathSegmentKind.Parent)
                    {
                        resultSegments.RemoveAt(resultSegments.Count - 1);
                    }
                    else
                    {
                        resultSegments.Add(seg);
                    }
                }
                else
                {
                    resultSegments.Add(seg);
                }
            }

            if (left is AbsolutePath)
            {
                return new AbsolutePath(resultSegments);
            }
            return new RelativePath(resultSegments);
        }

        /// <summary>
        /// 计算目标路径 target 相对于基准路径 base 的相对路径元。
        /// </summary>
        /// <param name="target">目标路径。</param>
        /// <param name="base">基准路径。</param>
        /// <returns>计算出的相对路径。</returns>
        /// <exception cref="InvalidOperationException">当两者跨盘符或跨根目录无法求相对路径时抛出。</exception>
        protected static RelativePath MakeRelative(FdPath target, FdPath @base)
        {
            // 归一化提取真实对比路径片段（去除 . 等无用片段）
            var baseSegs = NormalizeSegments(@base.Segments);
            var targetSegs = NormalizeSegments(target.Segments);

            // 检查跨盘符或绝对/相对不兼容情况
            if (baseSegs.Count > 0 && targetSegs.Count > 0)
            {
                if (baseSegs[0].Kind == PathSegmentKind.Root && targetSegs[0].Kind == PathSegmentKind.Root)
                {
                    if (!string.Equals(baseSegs[0].Name, targetSegs[0].Name, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException($"无法跨根目录/跨盘符计算相对路径: {@base.PathStr} 与 {target.PathStr}");
                    }
                }
            }

            // 寻找公共前缀长度
            int commonLength = 0;
            int minLen = Math.Min(baseSegs.Count, targetSegs.Count);
            while (commonLength < minLen &&
                   string.Equals(baseSegs[commonLength].Name, targetSegs[commonLength].Name, StringComparison.OrdinalIgnoreCase))
            {
                commonLength++;
            }

            var rel = new List<PathSegment>();

            // base 剩余部分需要通过 ".." 退回
            int upSteps = baseSegs.Count - commonLength;
            for (int i = 0; i < upSteps; i++)
            {
                rel.Add(new PathSegment("..", PathSegment.FolderType, PathSegmentKind.Parent));
            }

            // 添加 target 的剩余目录
            for (int i = commonLength; i < targetSegs.Count; i++)
            {
                rel.Add(targetSegs[i]);
            }

            // 如果没有退回步骤且有后续路径，为了保证相对路径标明，补齐一个 "."
            if (upSteps == 0)
            {
                rel.Insert(0, new PathSegment(".", PathSegment.FolderType, PathSegmentKind.Current));
            }

            return new RelativePath(rel);
        }
        /// <summary>
        /// 归一化路径片段列表：剥离当前目录（"."）并折叠可消解的父目录（".."）。
        /// </summary>
        /// <param name="segments">原始片段集合。</param>
        /// <returns>归一化后的片段列表。</returns>
        protected static List<PathSegment> NormalizeSegments(IReadOnlyList<PathSegment> segments)
        {
            var list = new List<PathSegment>();
            foreach (var seg in segments)
            {
                if (seg.Kind == PathSegmentKind.Current) continue;
                if (seg.Kind == PathSegmentKind.Parent)
                {
                    if (list.Count > 0 && list[^1].Kind != PathSegmentKind.Root && list[^1].Kind != PathSegmentKind.Parent)
                    {
                        list.RemoveAt(list.Count - 1);
                        continue;
                    }
                }
                list.Add(seg);
            }
            return list;
        }
        /// <summary>
        /// 将路径格式化为规范的平台路径字符串。
        /// </summary>
        /// <returns>路径字符串表示。</returns>
        public abstract override string ToString();
        /// <summary>
        /// 判断当前路径是否与另一个路径相等（基于格式化字符串，忽略大小写）。
        /// </summary>
        /// <param name="other">用于比较的另一个路径对象。</param>
        /// <returns>如果相等则为 true；否则为 false。</returns>
        public bool Equals(FdPath? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return PathStr.Equals(other.PathStr, StringComparison.OrdinalIgnoreCase);
        }
        /// <summary>
        /// 确定指定对象是否等于当前路径对象。
        /// </summary>
        /// <param name="obj">要与当前对象比较的对象。</param>
        /// <returns>如果指定对象是 <see cref="FdPath"/> 且相等，则为 true；否则为 false。</returns>
        public override bool Equals(object? obj) => Equals(obj as FdPath);
        /// <summary>获取当前路径的哈希代码。</summary>
        /// <returns>基于路径字符串（忽略大小写）生成的哈希代码。</returns>
        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(PathStr);
    }

    /// <summary>
    /// 相对路径类型
    /// </summary>
    public class RelativePath : FdPath
    {
        /// <summary>
        /// 从字符串解析并初始化 <see cref="RelativePath"/> 类的新实例。
        /// </summary>
        public RelativePath(string path) : base(ParseStringToSegments(path, false)) { }
        /// <summary>
        /// 从片段集合初始化 <see cref="RelativePath"/> 类的新实例。
        /// </summary>
        public RelativePath(IEnumerable<PathSegment> segments) : base(segments) { }
        /// <summary>
        /// 将相对路径输出为规范的字符串表现形式。
        /// </summary>
        public override string ToString()
        {
            if (Segments.Count == 0) return ".";

            var sb = new StringBuilder();
            for (int i = 0; i < Segments.Count; i++)
            {
                sb.Append(Segments[i].Name);
                if (i < Segments.Count - 1) sb.Append(Path.DirectorySeparatorChar);
            }
            return sb.ToString();
        }
        /// <summary>
        /// 将原始路径字符串切分并解析为路径元集合。
        /// </summary>
        /// <param name="path">路径字符串。</param>
        /// <param name="isAbsolute">是否为绝对路径模式（相对路径若不以 "." 或 ".." 开头会自动前置补齐 "."）。</param>
        /// <returns>解析出的片段列表。</returns>
        public static List<PathSegment> ParseStringToSegments(string path, bool isAbsolute)
        {
            var list = new List<PathSegment>();
            if (string.IsNullOrWhiteSpace(path)) return list;

            bool trailingSlash = path.EndsWith('\\') || path.EndsWith('/');
            string[] rawParts = path.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < rawParts.Length; i++)
            {
                bool isLast = (i == rawParts.Length - 1);
                list.Add(PathSegment.Create(rawParts[i], isLast, trailingSlash));
            }

            // 规范化相对路径开头的 "."
            if (!isAbsolute && list.Count > 0)
            {
                if (list[0].Kind != PathSegmentKind.Current && list[0].Kind != PathSegmentKind.Parent)
                {
                    list.Insert(0, new PathSegment(".", PathSegment.FolderType, PathSegmentKind.Current));
                }
            }

            return list;
        }
    }

    /// <summary>
    /// 绝对路径类型
    /// </summary>
    public class AbsolutePath : FdPath
    {
        /// <summary>
        /// 从字符串解析并初始化 <see cref="AbsolutePath"/> 类的新实例。
        /// </summary>
        public AbsolutePath(string path) : base(ParseAbsoluteToSegments(path)) { }
        /// <summary>
        /// 从片段集合初始化 <see cref="AbsolutePath"/> 类的新实例。
        /// </summary>
        public AbsolutePath(IEnumerable<PathSegment> segments) : base(segments) { }
        /// <summary>
        /// 将绝对路径输出为规范的字符串表现形式。
        /// </summary>
        public override string ToString()
        {
            if (Segments.Count == 0) return string.Empty;

            var sb = new StringBuilder();
            for (int i = 0; i < Segments.Count; i++)
            {
                var seg = Segments[i];
                sb.Append(seg.Name);
                if (i < Segments.Count - 1)
                {
                    // 盘符（如 "C:"）后面必须追加反斜杠 "C:\"
                    if (seg.Kind == PathSegmentKind.Root && !seg.Name.EndsWith('\\'))
                    {
                        sb.Append(Path.DirectorySeparatorChar);
                    }
                    else if (seg.Kind != PathSegmentKind.Root)
                    {
                        sb.Append(Path.DirectorySeparatorChar);
                    }
                }
            }
            return sb.ToString();
        }
        /// <summary>
        /// 解析绝对路径字符串为路径片段列表。
        /// </summary>
        /// <param name="path">待解析路径。</param>
        /// <returns>首节点为根的路径元列表。</returns>
        private static List<PathSegment> ParseAbsoluteToSegments(string path)
        {
            string absPath = PathValidator.ToAbsolutePath(path);
            var list = new List<PathSegment>();

            string root = Path.GetPathRoot(absPath) ?? string.Empty;
            if (!string.IsNullOrEmpty(root))
            {
                list.Add(new PathSegment(root.TrimEnd('\\', '/'), PathSegment.FolderType, PathSegmentKind.Root));
            }

            string rest = absPath.Substring(root.Length);
            bool trailingSlash = absPath.EndsWith('\\') || absPath.EndsWith('/');
            string[] parts = rest.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < parts.Length; i++)
            {
                bool isLast = (i == parts.Length - 1);
                list.Add(PathSegment.Create(parts[i], isLast, trailingSlash));
            }

            return list;
        }
    }
}
