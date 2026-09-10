using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ReluctersteSDK.PathHelper.Test
{
    public class FdPathTests
    {
        private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        [Fact]
        public void 文件路径抽象_相对路径解析构建_01()
        {
            // 纯相对路径初始片段应自动补齐当前目录 "."
            var rel = new RelativePath(@"SubFolder/file.txt");
            Assert.Equal(3, rel.Segments.Count);
            Assert.Equal(".", rel.Segments[0].Name);
            Assert.Equal("SubFolder", rel.Segments[1].Name);
            Assert.Equal("file.txt", rel.Segments[2].Name);
        }

        [Fact]
        public void 文件路径抽象_乘号路径组合运算符_01()
        {
            // 两个相对路径的常规拼接
            FdPath baseRel = new RelativePath("folderA");
            FdPath targetRel = new RelativePath("folderB/file.txt");

            FdPath combined = baseRel * targetRel;

            Assert.IsType<RelativePath>(combined);
            Assert.Contains("folderA", combined.PathStr);
            Assert.Contains("folderB", combined.PathStr);
        }

        [Fact]
        public void 文件路径抽象_乘号路径组合运算符_02()
        {
            // 拼接中包含 ".." 上级目录时应能回退上一级
            FdPath path1 = new RelativePath("A/B");
            FdPath path2 = new RelativePath("../C");

            FdPath combined = path1 * path2;

            Assert.IsType<RelativePath>(combined);
            Assert.DoesNotContain("B", combined.PathStr);
            Assert.Contains("A", combined.PathStr);
            Assert.Contains("C", combined.PathStr);
        }

        [Fact]
        public void 文件路径抽象_乘号路径组合运算符_03()
        {
            if (!IsWindows) return;

            // 右操作数为绝对路径时，组合结果直接为右操作数
            FdPath left = new RelativePath("local");
            FdPath right = new AbsolutePath(@"C:\Data\file.bin");

            FdPath result = left * right;

            Assert.IsType<AbsolutePath>(result);
            Assert.Equal(right.PathStr, result.PathStr);
        }

        [Fact]
        public void 文件路径抽象_除号相对路径计算运算符_01()
        {
            if (!IsWindows) return;

            // 计算同盘符两个路径的相对差
            FdPath target = new AbsolutePath(@"C:\Root\App\Bin\Release");
            FdPath @base = new AbsolutePath(@"C:\Root\App\Docs");

            RelativePath relative = target / @base;

            string relStr = relative.PathStr;
            Assert.StartsWith("..", relStr);
            Assert.Contains("Bin", relStr);
            Assert.Contains("Release", relStr);
            Assert.DoesNotContain("Docs", relStr);
        }

        [Fact]
        public void 文件路径抽象_除号相对路径计算运算符_02()
        {
            if (!IsWindows) return;

            // 跨盘符尝试计算相对路径，应抛出 InvalidOperationException
            FdPath target = new AbsolutePath(@"C:\Folder\File.txt");
            FdPath @base = new AbsolutePath(@"D:\Folder\Other.txt");

            Assert.Throws<InvalidOperationException>(() =>
            {
                _ = target / @base;
            });
        }

        [Fact]
        public void 文件路径抽象_对象相等性比对_01()
        {
            // 路径大小写及斜杠规范不同时应判定为相等
            var p1 = new RelativePath("A/b/C.txt");
            var p2 = new RelativePath("a\\B\\c.txt");

            Assert.True(p1.Equals(p2));
            Assert.Equal(p1.GetHashCode(), p2.GetHashCode());
        }
    }
}
