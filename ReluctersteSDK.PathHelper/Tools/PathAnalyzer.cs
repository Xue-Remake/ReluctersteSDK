namespace ReluctersteSDK.PathHelper.Tools
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
            else
            {
                return new AbsolutePath(PathValidator.ToAbsolutePath(result));
            }
        }
        /// <summary>
        /// 将已有的相对路径对象转换为绝对路径对象。
        /// <para>以程序运行目录（AppContext.BaseDirectory）为基准计算绝对路径。</para>
        /// </summary>
        /// <param name="rpath">相对路径对象</param>
        /// <returns>转换后的绝对路径对象 <see cref="AbsolutePath"/></returns>
        public static AbsolutePath? Analysis(FdPath path)
        {
            if (PathValidator.IsValidAbsolutePath(path.PathStr))
            {
                if (path is AbsolutePath) return (AbsolutePath)path;
                else return new AbsolutePath(path.PathStr);
            }
            else
            {
                return new AbsolutePath(PathValidator.ToAbsolutePath(path.PathStr));
            }
        }

        /// <summary>
        /// 将任意路径字符串转换为相对于程序运行目录（AppContext.BaseDirectory）的相对路径
        /// </summary>
        /// <param name="str">待转换的路径字符串</param>
        /// <returns>相对路径对象；如果格式非法或无法建立相对关系（如跨盘符），则返回 null</returns>
        public static RelativePath? ToRelative(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return null;
            // 1. 验证并格式化输入的路径
            string? formatted = PathValidator.FormatPath(str);
            if (formatted == null)
                return null;
            // 2. 统一转换为绝对路径（这样可以兼容输入本身就是相对路径、或是未简化的绝对路径的情况）
            string absPath = PathValidator.ToAbsolutePath(formatted);
            // 3. 计算相对于程序集运行目录的相对路径
            string baseDir = AppContext.BaseDirectory;
            string relative = Path.GetRelativePath(baseDir, absPath);
            // 4. 检查转换结果是否真的是相对路径
            // 如果两个路径跨盘符（例如 C:\ 到 D:\），Path.GetRelativePath 会直接返回原绝对路径。
            // 此时通过检查其是否有 Root（根部）来判断是否成功转换为了相对路径。
            string root = Path.GetPathRoot(relative) ?? string.Empty;
            if (!string.IsNullOrEmpty(root))
            {
                return null; // 无法建立相对关系，返回 null
            }
            // 5. 对生成的相对路径运行一次 FormatPath，确保其符合 PathValidator 的规范（如自动补全 ".\" 前缀）
            string? formattedRel = PathValidator.FormatPath(relative);
            if (formattedRel == null)
                return null;
            return new RelativePath(formattedRel);
        }
        /// <summary>
        /// 将绝对路径对象转换为相对于程序运行目录的相对路径
        /// </summary>
        /// <param name="apath">绝对路径对象</param>
        /// <returns>相对路径对象；如果无法建立相对关系则返回 null</returns>
        public static RelativePath? ToRelative(AbsolutePath apath)
        {
            if (apath == null)
                return null;
            // 直接复用 ToRelative(string) 的高可靠性逻辑
            return ToRelative(apath.PathStr);
        }
    }
}
