using ReluctersteSDK.Paths.Tools;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ReluctersteSDK.Paths.Test
{
    public class PathAnalyzerTests
    {
        private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        [Fact]
        public void 路径分析转换器_解析转换为绝对路径_01()
        {
            // 合法相对路径应基于 AppContext.BaseDirectory 解析为绝对路径
            AbsolutePath? abs = PathAnalyzer.Analysis("test/folder/file.json");

            Assert.NotNull(abs);
            Assert.True(Path.IsPathRooted(abs!.PathStr));
            Assert.StartsWith(AppContext.BaseDirectory.TrimEnd('\\', '/'), abs.PathStr);
        }

        [Fact]
        public void 路径分析转换器_解析转换为绝对路径_02()
        {
            // 包含非法字符解析失败应返回 null
            AbsolutePath? abs = PathAnalyzer.Analysis("invalid|char/path");
            Assert.Null(abs);
        }

        [Fact]
        public void 路径分析转换器_计算基准目录相对路径_01()
        {
            // 属于基准目录内的绝对路径转相对路径
            string baseDir = AppContext.BaseDirectory;
            string testSubDir = Path.Combine(baseDir, "TestFolder", "test.txt");

            RelativePath? rel = PathAnalyzer.ToRelative(testSubDir);

            Assert.NotNull(rel);
            Assert.Contains("TestFolder", rel!.PathStr);
            Assert.Contains("test.txt", rel.PathStr);
        }

        [Fact]
        public void 路径分析转换器_计算基准目录相对路径_02()
        {
            if (!IsWindows) return;

            // 跨盘符路径无法转换相对于运行目录的相对路径，应返回 null
            string currentRoot = Path.GetPathRoot(AppContext.BaseDirectory) ?? "C:\\";
            char currentLetter = char.ToUpperInvariant(currentRoot[0]);
            char otherLetter = currentLetter == 'C' ? 'Z' : 'C';
            string crossDrivePath = $"{otherLetter}:\\CrossDriveFolder\\file.txt";

            RelativePath? rel = PathAnalyzer.ToRelative(crossDrivePath);
            Assert.Null(rel);
        }
    }
}
