using ReluctersteSDK.Paths.Tools;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ReluctersteSDK.Paths.Test
{
    public class PathValidatorTests
    {
        private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        #region IsValidPathFormat

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("dir/|illegal/file.txt")]
        [InlineData("dir/<illegal>/file.txt")]
        public void 路径校验器_合法路径格式判定_01(string? path)
        {
            // 空值、纯空白以及包含非法系统字符输入，应判定为无效
            Assert.False(PathValidator.IsValidPathFormat(path!));
        }

        [Fact]
        public void 路径校验器_合法路径格式判定_02()
        {
            if (!IsWindows) return;

            // Windows 保留设备名称（如 CON, NUL, AUX, COM1 等）作为路径片段时应判定为无效
            Assert.False(PathValidator.IsValidPathFormat(@"C:\CON\test.txt"));
            Assert.False(PathValidator.IsValidPathFormat(@"C:\Folder\NUL"));
            Assert.False(PathValidator.IsValidPathFormat(@"C:\Folder\aux.json"));
            Assert.False(PathValidator.IsValidPathFormat(@"COM1.txt"));
        }

        [Fact]
        public void 路径校验器_合法路径格式判定_03()
        {
            if (!IsWindows) return;

            // 单个目录/文件名片段长度超过 255 字符，应判定为无效
            string longSegment = new string('a', 256);
            string path = $@"C:\{longSegment}\file.txt";
            Assert.False(PathValidator.IsValidPathFormat(path));
        }

        [Fact]
        public void 路径校验器_合法路径格式判定_04()
        {
            // 常见的有效相对路径与绝对路径校验
            if (IsWindows)
            {
                Assert.True(PathValidator.IsValidPathFormat(@"C:\Program Files\App\config.json"));
                Assert.True(PathValidator.IsValidPathFormat(@"C:/Program Files/App/config.json"));
                Assert.True(PathValidator.IsValidPathFormat(@"\\Server\Share\file.txt"));
                Assert.True(PathValidator.IsValidPathFormat(@".\local\sub\file.txt"));
                Assert.True(PathValidator.IsValidPathFormat(@"..\parent\file.txt"));
                Assert.True(PathValidator.IsValidPathFormat(@"simple_file.txt"));
            }
            else
            {
                Assert.True(PathValidator.IsValidPathFormat("/usr/local/bin/app"));
                Assert.True(PathValidator.IsValidPathFormat("./local/bin/app"));
            }
        }

        #endregion

        #region IsValidAbsolutePath

        [Theory]
        [InlineData(@"relative\path\file.txt")]
        [InlineData(@".\file.txt")]
        [InlineData(@"..\file.txt")]
        [InlineData(@"\OnlySlashRoot\test.txt")] // Windows 单斜杠视为驱动器相对路径
        public void 路径校验器_绝对路径格式校验_01(string relativePath)
        {
            if (!IsWindows) return;
            // 相对路径或不具备完整盘符根的路径，判定绝对路径应返回 false
            Assert.False(PathValidator.IsValidAbsolutePath(relativePath));
        }

        [Fact]
        public void 路径校验器_绝对路径格式校验_02()
        {
            if (!IsWindows) return;

            // 根部（Root）不符合绝对路径规范（非法盘符、非字母盘符、畸形UNC等）
            Assert.False(PathValidator.IsValidAbsolutePath(@"1:\folder"));     // 盘符首字符非字母
            Assert.False(PathValidator.IsValidAbsolutePath(@"C::\folder"));    // 双冒号畸形
            Assert.False(PathValidator.IsValidAbsolutePath(@"C:folder"));      // 盘符相对路径（长度不为3）
            Assert.False(PathValidator.IsValidAbsolutePath(@"\\server_only")); // UNC 缺少共享目录段
        }

        [Fact]
        public void 路径校验器_绝对路径格式校验_03()
        {
            if (!IsWindows) return;

            // 合法标准绝对路径（盘符、正反斜杠、UNC）
            Assert.True(PathValidator.IsValidAbsolutePath(@"C:\Windows\System32"));
            Assert.True(PathValidator.IsValidAbsolutePath(@"d:/documents/file.pdf"));
            Assert.True(PathValidator.IsValidAbsolutePath(@"\\Server\Share\Path\To\File"));
        }

        #endregion

        #region TryFormatPath & FormatPath

        [Theory]
        [InlineData(@"folder/subfolder/file.txt", @".\folder\subfolder\file.txt")]
        [InlineData(@"./folder/subfolder/", @".\folder\subfolder\")]
        [InlineData(@"../parent/file.txt", @"..\parent\file.txt")]
        public void 路径校验器_格式化路径规范化_01(string input, string expected)
        {
            if (!IsWindows) return;

            // 相对路径标准化：补齐 .\ 前缀并统一反斜杠
            bool success = PathValidator.TryFormatPath(input, out string? formatted);
            Assert.True(success);
            Assert.Equal(expected, formatted);
        }

        [Fact]
        public void 路径校验器_格式化路径规范化_02()
        {
            if (!IsWindows) return;

            // 绝对路径标准化：正斜杠统一替换为反斜杠
            string input = "C:/Program Files/My App/data.txt";
            bool success = PathValidator.TryFormatPath(input, out string? formatted);

            Assert.True(success);
            Assert.Equal(@"C:\Program Files\My App\data.txt", formatted);
        }

        [Fact]
        public void 路径校验器_格式化路径规范化_03()
        {
            if (!IsWindows) return;

            // UNC 路径格式化
            string input = "//my-server/my-share/folder/file.txt";
            bool success = PathValidator.TryFormatPath(input, out string? formatted);

            Assert.True(success);
            Assert.Equal(@"\\my-server\my-share\folder\file.txt", formatted);
        }

        [Theory]
        [InlineData("invalid|char")]
        [InlineData("CON")]
        public void 路径校验器_格式化路径规范化_04(string invalidPath)
        {
            // 非法格式路径格式化失败返回 false 与 null
            bool success = PathValidator.TryFormatPath(invalidPath, out string? formatted);
            Assert.False(success);
            Assert.Null(formatted);
            Assert.Null(PathValidator.FormatPath(invalidPath));
        }

        #endregion
    }
}
