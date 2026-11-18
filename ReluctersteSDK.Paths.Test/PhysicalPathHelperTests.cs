using ReluctersteSDK.Paths.Tools;
using System;
using System.Collections.Generic;
using System.Text;

namespace ReluctersteSDK.Paths.Test
{
    public class PhysicalPathHelperTests : IDisposable
    {
        private readonly string _testSandboxDirectory;

        public PhysicalPathHelperTests()
        {
            // 动态建立临时隔离测试沙箱
            _testSandboxDirectory = Path.Combine(AppContext.BaseDirectory, $"__UnitTestSandbox_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_testSandboxDirectory);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testSandboxDirectory))
                {
                    Directory.Delete(_testSandboxDirectory, recursive: true);
                }
            }
            catch
            {
                // 清理沙箱异常静默
            }
        }

        [Fact]
        public void 物理磁盘操作_文件是否存在检测_01()
        {
            // 检测存在与不存在的磁盘物理文件
            string filePath = Path.Combine(_testSandboxDirectory, "sample.txt");
            File.WriteAllText(filePath, "Hello World");

            Assert.True(PhysicalPathHelper.FileExists(filePath));
            Assert.False(PhysicalPathHelper.FileExists(Path.Combine(_testSandboxDirectory, "non_existent.txt")));
        }

        [Fact]
        public void 物理磁盘操作_目录是否包含文件检测_01()
        {
            // 验证空文件夹与子目录文件递归检查机制
            string subDir = Path.Combine(_testSandboxDirectory, "Sub");
            Directory.CreateDirectory(subDir);

            // 空目录返回 false
            Assert.False(PhysicalPathHelper.DirectoryContainsFiles(subDir));

            // 子级创建文件
            string childFile = Path.Combine(subDir, "deep.txt");
            File.WriteAllText(childFile, "content");

            // 仅顶层搜索时不应命中
            Assert.False(PhysicalPathHelper.DirectoryContainsFiles(_testSandboxDirectory, checkSubDirectories: false));
            // 开启递归时应能命中
            Assert.True(PhysicalPathHelper.DirectoryContainsFiles(_testSandboxDirectory, checkSubDirectories: true));
        }

        [Fact]
        public void 物理磁盘操作_确保目录存在创建_01()
        {
            // 递归创建不存在的多层物理路径
            string dirToCreate = Path.Combine(_testSandboxDirectory, "Level1", "Level2", "Level3");

            AbsolutePath? result = PhysicalPathHelper.EnsureDirectoryExists(dirToCreate);

            Assert.NotNull(result);
            Assert.True(Directory.Exists(dirToCreate));
        }

        [Fact]
        public void 物理磁盘操作_获取匹配文件列表_01()
        {
            // 支持委托筛选并正确返回 AbsolutePath 列表
            File.WriteAllText(Path.Combine(_testSandboxDirectory, "file1.txt"), "1");
            File.WriteAllText(Path.Combine(_testSandboxDirectory, "file2.log"), "2");
            File.WriteAllText(Path.Combine(_testSandboxDirectory, "file3.txt"), "3");

            List<AbsolutePath> allFiles = PhysicalPathHelper.GetFiles(_testSandboxDirectory);
            Assert.Equal(3, allFiles.Count);

            List<AbsolutePath> txtFiles = PhysicalPathHelper.GetFiles(
                _testSandboxDirectory,
                checkSubDirectories: false,
                predicate: p => p.EndsWith(".txt", StringComparison.OrdinalIgnoreCase));

            Assert.Equal(2, txtFiles.Count);
            Assert.All(txtFiles, f => Assert.EndsWith(".txt", f.PathStr));
        }
    }
}
