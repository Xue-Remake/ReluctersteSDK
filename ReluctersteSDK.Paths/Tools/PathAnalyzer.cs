namespace ReluctersteSDK.Paths.Tools
{
    /// <summary>
    /// 路径分析与转换工具类，提供路径的规范化解析、绝对路径与相对路径之间的相互转换。
    /// </summary>
    public static class PathAnalyzer
    {
        /// <summary>
        /// 解析任意路径字符串并将其转换为绝对路径对象。
        /// <para>若输入为相对路径，将以程序运行目录（AppContext.BaseDirectory）为基准解析为绝对路径。</para>
        /// </summary>
        /// <param name="str">待解析的路径字符串（支持绝对路径、相对路径或不规范的路径格式）</param>
        /// <returns>解析并格式化后的绝对路径对象 <see cref="AbsolutePath"/>；如果路径格式非法，则返回 <see langword="null"/></returns>
        public static AbsolutePath? Analysis(string str)
        {
            string? result = PathValidator.FormatPath(str);
            if (result == null) return null;
            return new AbsolutePath(PathValidator.ToAbsolutePath(result));
        }
        /// <summary>
        /// 将已有的相对路径对象转换为绝对路径对象。
        /// <para>以程序运行目录（AppContext.BaseDirectory）为基准计算绝对路径。</para>
        /// </summary>
        /// <param name="path">相对路径对象</param>
        /// <returns>转换后的绝对路径对象 <see cref="AbsolutePath"/></returns>
        public static AbsolutePath? Analysis(FdPath path)
        {
            if (path is AbsolutePath abs) return abs;
            return new AbsolutePath(PathValidator.ToAbsolutePath(path.PathStr));
        }

        /// <summary>
        /// 将任意路径字符串转换为相对于程序运行目录（AppContext.BaseDirectory）的相对路径
        /// </summary>
        /// <param name="str">待转换的路径字符串</param>
        /// <returns>相对路径对象；如果格式非法或无法建立相对关系（如跨盘符），则返回 null</returns>
        public static RelativePath? ToRelative(string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return null;

            string? formatted = PathValidator.FormatPath(str);
            if (formatted == null) return null;

            string absPath = PathValidator.ToAbsolutePath(formatted);
            string baseDir = AppContext.BaseDirectory;
            string relative = Path.GetRelativePath(baseDir, absPath);

            string root = Path.GetPathRoot(relative) ?? string.Empty;
            if (!string.IsNullOrEmpty(root))
            {
                return null; // 跨盘符无法建立相对关系
            }

            return new RelativePath(relative);
        }
        /// <summary>
        /// 将绝对路径对象转换为相对于程序运行目录的相对路径
        /// </summary>
        /// <param name="path">绝对路径对象</param>
        /// <returns>相对路径对象；如果无法建立相对关系则返回 null</returns>
        public static RelativePath? ToRelative(AbsolutePath path)
        {
            if (path == null) return null;
            return ToRelative(path.PathStr);
        }
    }
}
