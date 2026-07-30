namespace ReluctersteSDK.PathHelper.Tools
{
    /// <summary>
    /// 物理文件与文件夹的检测与操作工具
    /// </summary>
    public static class PhysicalPathHelper
    {
        /// <summary>
        /// 检测指定的文件路径是否存在该文件
        /// </summary>
        /// <param name="filePath">文件路径（支持相对/绝对路径字符串）</param>
        /// <returns>存在返回 true，否则返回 false</returns>
        public static bool FileExists(string filePath)
        {
            AbsolutePath? absPath = PathAnalyzer.Analysis(filePath);
            if (absPath == null) return false;
            return File.Exists(absPath.PathStr);
        }
        /// <summary>
        /// 检测指定的文件路径是否存在该文件
        /// </summary>
        /// <param name="filePath">文件路径对象</param>
        /// <returns>存在返回 true，否则返回 false</returns>
        public static bool FileExists(FdPath filePath)
        {
            AbsolutePath? absPath = PathAnalyzer.Analysis(filePath);
            if (absPath == null) return false;
            return File.Exists(absPath.PathStr);
        }
        /// <summary>
        /// 检测指定的文件夹路径下是否存在文件
        /// （如果文件夹路径不存在则同样返回 false）
        /// </summary>
        /// <param name="folderPath">文件夹路径（支持相对/绝对路径字符串）</param>
        /// <param name="checkSubDirectories">是否递归检查子文件夹中的文件，默认仅检查当前顶级目录</param>
        /// <returns>如果存在至少一个文件则返回 true，否则返回 false</returns>
        public static bool DirectoryContainsFiles(string folderPath, bool checkSubDirectories = false)
        {
            AbsolutePath? absPath = PathAnalyzer.Analysis(folderPath);
            if (absPath == null) return false;
            return InternalDirectoryContainsFiles(absPath.PathStr, checkSubDirectories);
        }
        /// <summary>
        /// 检测指定的文件夹路径下是否存在文件
        /// （如果文件夹路径不存在则同样返回 false）
        /// </summary>
        /// <param name="folderPath">文件夹路径对象</param>
        /// <param name="checkSubDirectories">是否递归检查子文件夹中的文件，默认仅检查当前顶级目录</param>
        /// <returns>如果存在至少一个文件则返回 true，否则返回 false</returns>
        public static bool DirectoryContainsFiles(FdPath folderPath, bool checkSubDirectories = false)
        {
            AbsolutePath? absPath = PathAnalyzer.Analysis(folderPath);
            if (absPath == null) return false;
            return InternalDirectoryContainsFiles(absPath.PathStr, checkSubDirectories);
        }
        /// <summary>
        /// 内部执行：文件夹内部是否有文件的核心判断
        /// </summary>
        private static bool InternalDirectoryContainsFiles(string absolutePathStr, bool checkSubDirectories)
        {
            // 如果文件夹本身不存在，直接返回 false
            if (!Directory.Exists(absolutePathStr)) return false;
            try
            {
                SearchOption option = checkSubDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                // EnumerateFiles 返回的是按需获取的迭代器。
                // 结合 Any()，它在找到第一个文件时就会立即停止（并返回true），所以在大文件夹中性能极高。
                return Directory.EnumerateFiles(absolutePathStr, "*", option).Any();
            }
            catch
            {
                // 捕捉诸如 权限不足(UnauthorizedAccessException) 等底层异常
                return false;
            }
        }
        /// <summary>
        /// 检测该文件夹路径是否存在，如果不存在则递归新建出这个路径。
        /// </summary>
        /// <param name="folderPath">文件夹路径（支持相对/绝对路径字符串）</param>
        /// <returns>返回创建成功或已存在的绝对路径对象；如果路径非法或创建失败则返回 null</returns>
        public static AbsolutePath? EnsureDirectoryExists(string folderPath)
        {
            AbsolutePath? absPath = PathAnalyzer.Analysis(folderPath);
            if (absPath == null) return null;
            return InternalEnsureDirectoryExists(absPath) ? absPath : null;
        }
        /// <summary>
        /// 检测该文件夹路径是否存在，如果不存在则递归新建出这个路径
        /// </summary>
        /// <param name="folderPath">文件夹路径对象</param>
        /// <returns>返回创建成功或已存在的绝对路径对象；如果路径非法或创建失败则返回 null</returns>
        public static AbsolutePath? EnsureDirectoryExists(FdPath folderPath)
        {
            AbsolutePath? absPath = PathAnalyzer.Analysis(folderPath);
            if (absPath == null) return null;
            return InternalEnsureDirectoryExists(absPath) ? absPath : null;
        }
        /// <summary>
        /// 搜索指定路径下的文件，返回所有符合要求的绝对路径列表
        /// </summary>
        /// <param name="folderPath">文件夹路径字符串（支持绝对/相对路径）</param>
        /// <param name="checkSubDirectories">是否递归检查子文件夹中的文件，默认仅检查当前顶级目录</param>
        /// <param name="predicate">可选的 LINQ 筛选表达式（传入文件绝对路径 string）</param>
        /// <returns>符合条件的文件绝对路径对象 <see cref="AbsolutePath"/> 的 List；若路径无效或目录不存在则返回空列表</returns>
        public static List<AbsolutePath> GetFiles(
            string folderPath,
            bool checkSubDirectories = false,
            Func<string, bool>? predicate = null)
        {
            AbsolutePath? absPath = PathAnalyzer.Analysis(folderPath);
            if (absPath == null) return new List<AbsolutePath>();
            return InternalGetFiles(absPath.PathStr, checkSubDirectories, predicate);
        }

        /// <summary>
        /// 搜索指定路径下的文件，返回所有符合要求的绝对路径列表
        /// </summary>
        /// <param name="folderPath">文件夹路径对象</param>
        /// <param name="checkSubDirectories">是否递归检查子文件夹中的文件，默认仅检查当前顶级目录</param>
        /// <param name="predicate">可选的 LINQ 筛选表达式（传入文件绝对路径 string）</param>
        /// <returns>符合条件的文件绝对路径对象 <see cref="AbsolutePath"/> 的 List；若路径无效或目录不存在则返回空列表</returns>
        public static List<AbsolutePath> GetFiles(
            FdPath folderPath,
            bool checkSubDirectories = false,
            Func<string, bool>? predicate = null)
        {
            AbsolutePath? absPath = PathAnalyzer.Analysis(folderPath);
            if (absPath == null) return new List<AbsolutePath>();
            return InternalGetFiles(absPath.PathStr, checkSubDirectories, predicate);
        }

        /// <summary>
        /// 内部执行：获取文件列表核心逻辑（支持 Linq 委托筛选）
        /// </summary>
        private static List<AbsolutePath> InternalGetFiles(
            string absolutePathStr,
            bool checkSubDirectories,
            Func<string, bool>? predicate)
        {
            var result = new List<AbsolutePath>();
            if (!Directory.Exists(absolutePathStr)) return result;

            try
            {
                SearchOption option = checkSubDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                IEnumerable<string> files = Directory.EnumerateFiles(absolutePathStr, "*", option);

                if (predicate != null)
                {
                    files = files.Where(predicate);
                }

                foreach (string filePath in files)
                {
                    result.Add(new AbsolutePath(filePath));
                }
            }
            catch
            {
                // 捕捉无权限访问等异常，确保安全性
            }

            return result;
        }
        /// <summary>
        /// 内部执行：确保目录存在的核心逻辑
        /// </summary>
        private static bool InternalEnsureDirectoryExists(AbsolutePath absPath)
        {
            try
            {
                if (!Directory.Exists(absPath.PathStr))
                {
                    // .NET原生的 Directory.CreateDirectory 底层自带递归创建机制
                    Directory.CreateDirectory(absPath.PathStr);
                }
                return true;
            }
            catch
            {
                // 创建失败情况：无读写权限、路径被同名无后缀的【文件】占用等
                return false;
            }
        }
    }
}
