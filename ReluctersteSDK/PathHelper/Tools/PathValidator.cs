using System.Runtime.InteropServices;

namespace ReluctersteSDK.PathHelper.Tools
{
    public static class PathValidator
    {
        // Windows 系统保留的设备名称（不允许作为文件夹或文件名，如 CON.txt 或 NUL）
        private static readonly string[] ReservedWindowsNames =
        {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };
        /// <summary>
        /// 验证输入的字符串是否是标准的合法路径格式（支持绝对路径和相对路径）
        /// </summary>
        /// <param name="path">待验证的路径字符串</param>
        /// <returns>如果是标准的路径格式返回 true，否则返回 false</returns>
        public static bool IsValidPathFormat(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;
            // 1. 检查是否包含平台绝对禁止的路径字符（如 Windows 下的 | < > 等，Linux 下的 \0）
            if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                return false;
            // 2. 利用系统 API 进行初步的完整路径尝试解析（会捕捉超长路径、非法冒号等底层异常）
            try
            {
                _ = Path.GetFullPath(path);
            }
            catch (ArgumentException) { return false; }
            catch (NotSupportedException) { return false; }
            catch (PathTooLongException) { return false; }
            catch (Exception) { return false; }
            // 3. 提取路径的根部（Root）
            string root;
            try
            {
                root = Path.GetPathRoot(path) ?? string.Empty;
            }
            catch
            {
                return false;
            }
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            // 4. 如果是 Windows 系统，对根部（Root）进行特有的格式校验
            if (isWindows && !string.IsNullOrEmpty(root))
            {
                // 4.1. 如果是 UNC 网络共享路径 (以 \\ 或 // 开头)
                if (root.StartsWith("\\\\") || root.StartsWith("//"))
                {
                    var uncSegments = root.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
                    if (uncSegments.Length < 2)
                        return false; // 比如 "\\\\" 或 "\\server" 缺少共享目录，属于非法 UNC 格式
                }
                // 4.2. 如果是盘符路径 (如 C: 或 C:\)
                else if (root.Contains(":"))
                {
                    if (root.Length > 3)
                        return false; // 例如 "C::\" 是非法的
                    char driveLetter = root[0];
                    if (!char.IsLetter(driveLetter))
                        return false; // 盘符首字符必须是英文字母
                    if (root[1] != ':')
                        return false;
                    if (root.Length == 3 && root[2] != '\\' && root[2] != '/')
                        return false;
                }
                // 4.3. 其他 Windows 根目录情况，必须仅为单斜杠 "\" 或 "/"
                else if (root != "\\" && root != "/")
                {
                    return false;
                }
            }
            // 5. 剥离根部，对剩余的每一个路径片段（Segment）进行细粒度验证
            string pathWithoutRoot = path;
            if (!string.IsNullOrEmpty(root))
            {
                if (path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                {
                    pathWithoutRoot = path.Substring(root.Length);
                }
                else
                {
                    // 处理可能存在的斜杠/反斜杠不一致问题（标准化后比较）
                    string normalizedPath = path.Replace('/', '\\');
                    string normalizedRoot = root.Replace('/', '\\');
                    if (normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        pathWithoutRoot = normalizedPath.Substring(normalizedRoot.Length);
                    }
                }
            }
            // 6. 按目录分隔符拆分出各级文件夹和文件名
            char[] separators = { '\\', '/' };
            string[] segments = pathWithoutRoot.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            char[] invalidFileChars = Path.GetInvalidFileNameChars();
            foreach (string segment in segments)
            {
                // 片段不能为空或纯空格
                if (string.IsNullOrWhiteSpace(segment))
                    return false;
                // 单个文件夹/文件名长度不能超过操作系统限制（通常最大为 255 字符）
                if (segment.Length > 255)
                    return false;
                // 检查片段中是否包含非法的Filename字符（如 * ? : 等）
                if (segment.IndexOfAny(invalidFileChars) >= 0)
                    return false;
                // 7. Windows 平台特有的系统保留名称检查（如 CON.txt, NUL, AUX 等）
                if (isWindows)
                {
                    // 提取不含扩展名的主文件名，例如 "CON.tar.gz" -> "CON"
                    int firstDotIndex = segment.IndexOf('.');
                    string baseName = firstDotIndex >= 0 ? segment.Substring(0, firstDotIndex) : segment;

                    if (ReservedWindowsNames.Contains(baseName.ToUpperInvariant()))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 验证并格式化输入的路径（如果合法，则转换为 Windows 标准反斜杠格式，支持绝对/相对路径）。
        /// </summary>
        /// <param name="path">待验证和格式化的路径</param>
        /// <returns>格式化后的标准 Windows 路径；如果验证失败则返回 null</returns>
        public static string? FormatPath(string path)
        {
            return TryFormatPath(path, out string? formattedPath) ? formattedPath : null;
        }

        /// <summary>
        /// 验证输入的字符串是否是一个标准的绝对路径。
        /// 相对路径、非法格式、包含非法字符、Windows保留名称等均返回 false。
        /// </summary>
        /// <param name="path">待验证的路径字符串</param>
        /// <returns>如果是标准的绝对路径返回 true，否则返回 false</returns>
        public static bool IsValidAbsolutePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;
            // 1. 检查是否包含平台绝对禁止的路径字符（如 Windows 下的 | < > 等，Linux 下的 \0）
            if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                return false;
            // 2. 利用系统 API 进行初步的完整路径尝试解析（会捕捉超长路径、绝对非法格式等底层异常）
            try
            {
                _ = Path.GetFullPath(path);
            }
            catch (ArgumentException) { return false; }
            catch (NotSupportedException) { return false; }
            catch (PathTooLongException) { return false; }
            catch (Exception) { return false; }
            // 3. 提取路径的根部（Root）
            string root;
            try
            {
                root = Path.GetPathRoot(path) ?? string.Empty;
            }
            catch
            {
                return false;
            }
            // 绝对路径的根部不能为空
            if (string.IsNullOrEmpty(root))
                return false;
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            // 4. 【核心差异】对根部（Root）进行严格的“绝对路径”格式校验
            if (isWindows)
            {
                // 4.1. 如果是 UNC 网络共享路径 (以 \\ 或 // 开头)
                if (root.StartsWith("\\\\") || root.StartsWith("//"))
                {
                    var uncSegments = root.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
                    if (uncSegments.Length < 2)
                        return false; // 比如 "\\\\" 或 "\\server" 缺少共享目录，属于非法 UNC 格式
                }
                // 4.2. 如果是盘符路径 (绝对路径必须是 C:\ 或 C:/，长度必须为 3)
                else if (root.Contains(":"))
                {
                    // 必须是类似 "C:\" 的 3 字符结构。排除 "C:" (驱动器相对路径) 或 "C::\" 等畸形
                    if (root.Length != 3)
                        return false;
                    char driveLetter = root[0];
                    if (!char.IsLetter(driveLetter))
                        return false; // 盘符首字符必须是英文字母

                    if (root[1] != ':')
                        return false;

                    if (root[2] != '\\' && root[2] != '/')
                        return false;
                }
                // 4.3. 排除单斜杠 "\" 或 "/"
                // 在 Windows 中，"\Temp" 代表当前驱动器下的根目录，属于“相对路径”，在此处必须返回 false
                else
                {
                    return false;
                }
            }
            else
            {
                // Unix/Linux/macOS 平台，绝对路径的根必须是单斜杠 "/"
                if (root != "/")
                    return false;
            }
            // 5. 剥离根部，对剩余的每一个路径片段（Segment）进行细粒度验证
            string pathWithoutRoot = path;
            if (path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                pathWithoutRoot = path.Substring(root.Length);
            }
            else
            {
                // 处理可能存在的斜杠/反斜杠不一致问题（标准化后比较）
                string normalizedPath = path.Replace('/', '\\');
                string normalizedRoot = root.Replace('/', '\\');
                if (normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
                {
                    pathWithoutRoot = normalizedPath.Substring(normalizedRoot.Length);
                }
                else
                {
                    return false; // 无法安全剥离根部，视为格式异常
                }
            }
            // 6. 按目录分隔符拆分出各级文件夹和文件名
            char[] separators = { '\\', '/' };
            string[] segments = pathWithoutRoot.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            char[] invalidFileChars = Path.GetInvalidFileNameChars();
            foreach (string segment in segments)
            {
                // 片段不能为空或纯空格
                if (string.IsNullOrWhiteSpace(segment))
                    return false;
                // 单个文件夹/文件名长度不能超过操作系统限制（通常最大为 255 字符）
                if (segment.Length > 255)
                    return false;
                // 检查片段中是否包含非法的Filename字符（如 * ? : 等）
                if (segment.IndexOfAny(invalidFileChars) >= 0)
                    return false;
                // 7. Windows 平台特有的系统保留名称检查（如 CON.txt, NUL, AUX 等）
                if (isWindows)
                {
                    // 提取不含扩展名的主文件名，例如 "CON.tar.gz" -> "CON"
                    int firstDotIndex = segment.IndexOf('.');
                    string baseName = firstDotIndex >= 0 ? segment.Substring(0, firstDotIndex) : segment;
                    if (ReservedWindowsNames.Contains(baseName.ToUpperInvariant()))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 将相对于当前程序集同级目录的相对路径转换为绝对路径
        /// </summary>
        /// <param name="relativePath">相对路径（假设已完成合法性校验）</param>
        /// <returns>绝对路径</returns>
        public static string ToAbsolutePath(string relativePath)
        {
            string baseDirectory = AppContext.BaseDirectory;
            return Path.GetFullPath(relativePath, baseDirectory);
        }

        /// <summary>
        /// 验证输入的路径，并在验证通过后将其格式化为标准的 Windows 路径格式（反斜杠 \ 分隔）。
        /// </summary>
        /// <param name="path">待验证的路径字符串</param>
        /// <param name="formattedPath">格式化后的路径（验证失败时为 null）</param>
        /// <returns>如果是标准的路径格式返回 true，否则返回 false</returns>
        public static bool TryFormatPath(string path, out string? formattedPath)
        {
            formattedPath = null;
            if (string.IsNullOrWhiteSpace(path))
                return false;
            // 1. 检查是否包含平台绝对禁止的路径字符
            if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                return false;
            // 2. 利用系统 API 进行初步的完整路径尝试解析
            try
            {
                _ = Path.GetFullPath(path);
            }
            catch (ArgumentException) { return false; }
            catch (NotSupportedException) { return false; }
            catch (PathTooLongException) { return false; }
            catch (Exception) { return false; }
            // 3. 提取路径的根部（Root）
            string root;
            try
            {
                root = Path.GetPathRoot(path) ?? string.Empty;
            }
            catch
            {
                return false;
            }
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            // 4. 如果是 Windows 系统，对根部（Root）进行特有的格式校验
            if (isWindows && !string.IsNullOrEmpty(root))
            {
                // 4.1. 如果是 UNC 网络共享路径 (以 \\ 或 // 开头)
                if (root.StartsWith("\\\\") || root.StartsWith("//"))
                {
                    var uncSegments = root.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
                    if (uncSegments.Length < 2)
                        return false;
                }
                // 4.2. 如果是盘符路径 (如 C: 或 C:\)
                else if (root.Contains(":"))
                {
                    if (root.Length > 3)
                        return false;
                    char driveLetter = root[0];
                    if (!char.IsLetter(driveLetter))
                        return false;
                    if (root[1] != ':')
                        return false;
                    if (root.Length == 3 && root[2] != '\\' && root[2] != '/')
                        return false;
                }
                // 4.3. 其他 Windows 根目录情况，必须仅为单斜杠 "\" 或 "/"
                else if (root != "\\" && root != "/")
                {
                    return false;
                }
            }
            // 5. 剥离根部，对剩余的每一个路径片段（Segment）进行细粒度验证
            string pathWithoutRoot = path;
            if (!string.IsNullOrEmpty(root))
            {
                if (path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                {
                    pathWithoutRoot = path.Substring(root.Length);
                }
                else
                {
                    string normalizedPath = path.Replace('/', '\\');
                    string normalizedRoot = root.Replace('/', '\\');
                    if (normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        pathWithoutRoot = normalizedPath.Substring(normalizedRoot.Length);
                    }
                }
            }
            // 6. 按目录分隔符拆分出各级文件夹和文件名
            char[] separators = { '\\', '/' };
            string[] segments = pathWithoutRoot.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            char[] invalidFileChars = Path.GetInvalidFileNameChars();
            foreach (string segment in segments)
            {
                if (string.IsNullOrWhiteSpace(segment))
                    return false;
                if (segment.Length > 255)
                    return false;
                if (segment.IndexOfAny(invalidFileChars) >= 0)
                    return false;
                // 7. Windows 平台特有的系统保留名称检查
                if (isWindows)
                {
                    int firstDotIndex = segment.IndexOf('.');
                    string baseName = firstDotIndex >= 0 ? segment.Substring(0, firstDotIndex) : segment;
                    if (ReservedWindowsNames.Contains(baseName.ToUpperInvariant()))
                    {
                        return false;
                    }
                }
            }
            // ==================== 格式化处理逻辑 ====================
            // 1. 标准化根路径（如果有的话，将 / 替换为 \）
            string formattedRoot = root.Replace('/', '\\');
            // 1.1 如果是 UNC 路径，确保开头是 "\\" 并且各个部分用 "\" 分隔
            if (isWindows && !string.IsNullOrEmpty(formattedRoot) && (root.StartsWith("\\\\") || root.StartsWith("//")))
            {
                var uncSegments = root.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
                formattedRoot = "\\\\" + string.Join("\\", uncSegments);
            }
            // 2. 拼接格式化后的路径主体（利用 segments 自动过滤并重构）
            string result;
            if (segments.Length > 0)
            {
                string segmentsJoined = string.Join("\\", segments);
                if (string.IsNullOrEmpty(formattedRoot))
                {
                    // 【核心修改点】：处理相对路径
                    // 如果第一个片段既不是当前目录 "." 也不是上级目录 ".."，说明是纯相对路径，统一加上 ".\" 前缀
                    if (segments[0] != "." && segments[0] != "..")
                    {
                        result = ".\\" + segmentsJoined;
                    }
                    else
                    {
                        result = segmentsJoined; // 已经是 ".\" 或 "..\" 开头，保持原样
                    }
                }
                else
                {
                    // 绝对路径或带盘符根目录的情况
                    if (formattedRoot.EndsWith("\\"))
                    {
                        result = formattedRoot + segmentsJoined;
                    }
                    else
                    {
                        result = formattedRoot + "\\" + segmentsJoined;
                    }
                }
            }
            else
            {
                result = formattedRoot;
            }
            // 3. 智能处理末尾斜杠
            // 如果输入的原始路径末尾带有斜杠（如 "C:/Users/" 或 "./xxx/"），则格式化后也补上一个 "\"
            bool originalHasTrailingSlash = path.EndsWith("\\") || path.EndsWith("/");
            if (originalHasTrailingSlash && !result.EndsWith("\\"))
            {
                result += "\\";
            }
            formattedPath = result;
            return true;
        }
    }
}
