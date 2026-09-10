using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ReluctersteSDK.PathHelper
{
    /// <summary>表示路径片段（语义节点）的种类。</summary>
    public enum PathSegmentKind
    {
        /// <summary>目录/文件夹。</summary>
        Directory,
        /// <summary>文件。</summary>
        File,
        /// <summary>根目录（如 Windows 盘符 "C:" 或 UNC 根 "\\server\share"）。</summary>
        Root,
        /// <summary>当前目录（"."）。</summary>
        Current,
        /// <summary>父级目录（".."）。</summary>
        Parent
    }

    /// <summary>
    /// 路径元数据类，代表路径中的一个片段/节点
    /// </summary>
    public sealed class PathSegment : IEquatable<PathSegment>
    {
        /// <summary>表示目录类型的统一常量标识符（"folder"）。</summary>
        public const string FolderType = "folder";

        /// <summary>片段原始名称（例如 "A"、"doc.txt"、"C:"）</summary>
        public string Name { get; }

        /// <summary>节点类型：如果是文件夹或目录则为 "folder"；如果是文件则为扩展名（如 ".txt"）；无扩展名文件为 ""</summary>
        public string Type { get; }

        /// <summary>片段在语义上的种类</summary>
        public PathSegmentKind Kind { get; }

        /// <summary>初始化 <see cref="PathSegment"/> 类的新实例。</summary>
        /// <param name="name">片段名称。</param>
        /// <param name="type">片段类型（文件夹或文件扩展名）。若为 null 则视为空字符串。</param>
        /// <param name="kind">片段种类语义枚举。</param>
        /// <exception cref="ArgumentNullException">当 <paramref name="name"/> 为 null 时抛出。</exception>
        public PathSegment(string name, string type, PathSegmentKind kind)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type ?? string.Empty;
            Kind = kind;
        }

        /// <summary>从原始字符串片段解析并创建路径元对象。</summary>
        /// <param name="rawSegment">待解析的单段原始字符串。</param>
        /// <param name="isLastSegment">该片段是否为整条路径中的最后一个片段。</param>
        /// <param name="hasTrailingSlash">原始路径末尾是否存在显式路径分隔符（'/' 或 '\'）。</param>
        /// <returns>解析得到的 <see cref="PathSegment"/> 实例。</returns>
        /// <exception cref="ArgumentException">当 <paramref name="rawSegment"/> 为空或空白字符串时抛出。</exception>
        public static PathSegment Create(string rawSegment, bool isLastSegment, bool hasTrailingSlash)
        {
            if (string.IsNullOrEmpty(rawSegment))
                throw new ArgumentException("Segment cannot be empty.", nameof(rawSegment));

            // 1. 处理驱动器盘符（如 "C:" 或 "C:\"）
            if (Regex.IsMatch(rawSegment, @"^[a-zA-Z]:\\?$"))
            {
                string drive = rawSegment.TrimEnd('\\', '/');
                return new PathSegment(drive, FolderType, PathSegmentKind.Root);
            }

            // 2. 处理 UNC 根（例如 "\\server\share"）
            if (rawSegment.StartsWith(@"\\") || rawSegment.StartsWith("//"))
            {
                return new PathSegment(rawSegment, FolderType, PathSegmentKind.Root);
            }

            // 3. 处理当前目录和上级目录
            if (rawSegment == ".")
            {
                return new PathSegment(".", FolderType, PathSegmentKind.Current);
            }
            if (rawSegment == "..")
            {
                return new PathSegment("..", FolderType, PathSegmentKind.Parent);
            }

            // 4. 不是最后一个片段，或者末尾带有显式斜杠，一定是文件夹
            if (!isLastSegment || hasTrailingSlash)
            {
                return new PathSegment(rawSegment, FolderType, PathSegmentKind.Directory);
            }

            // 5. 最后一个片段：根据是否有文件后缀判断
            string ext = Path.GetExtension(rawSegment);
            if (!string.IsNullOrEmpty(ext))
            {
                return new PathSegment(rawSegment, ext, PathSegmentKind.File);
            }

            // 无后缀默认按文件夹处理（符合通用路径习惯，若为无后缀文件可按需求指定）
            return new PathSegment(rawSegment, FolderType, PathSegmentKind.Directory);
        }
        /// <summary>
        /// 返回片段的原始名称。
        /// </summary>
        public override string ToString() => Name;
        /// <summary>
        /// 判断当前片段是否与另一个片段相等（忽略名称与类型的大小写）。
        /// </summary>
        /// <param name="other">用于比较的另一个片段。</param>
        /// <returns>如果二者相等则为 true；否则为 false。</returns>
        public bool Equals(PathSegment? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(Type, other.Type, StringComparison.OrdinalIgnoreCase) &&
                   Kind == other.Kind;
        }
        /// <summary>
        /// 确定指定对象是否等于当前片段。
        /// </summary>
        public override bool Equals(object? obj) => Equals(obj as PathSegment);
        /// <summary>
        /// 获取当前路径片段的哈希代码。
        /// </summary>
        public override int GetHashCode() =>
            HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(Name), Kind);
    }
}
