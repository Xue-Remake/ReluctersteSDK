namespace ReluctersteSDK.Paths.Test
{
    public class PathSegmentTests
    {
        [Fact]
        public void 路径元分析_盘符与根目录识别_01()
        {
            var seg1 = PathSegment.Create("C:", isLastSegment: false, hasTrailingSlash: true);
            var seg2 = PathSegment.Create(@"D:\", isLastSegment: false, hasTrailingSlash: false);

            Assert.Equal("C:", seg1.Name);
            Assert.Equal(PathSegmentKind.Root, seg1.Kind);
            Assert.Equal(PathSegment.FolderType, seg1.Type);

            Assert.Equal("D:", seg2.Name);
            Assert.Equal(PathSegmentKind.Root, seg2.Kind);
        }
        [Fact]
        public void 路径元分析_当前与父级目录识别_01()
        {
            var dot = PathSegment.Create(".", isLastSegment: false, hasTrailingSlash: false);
            var dotDot = PathSegment.Create("..", isLastSegment: false, hasTrailingSlash: false);

            Assert.Equal(PathSegmentKind.Current, dot.Kind);
            Assert.Equal(PathSegmentKind.Parent, dotDot.Kind);
        }

        [Fact]
        public void 路径元分析_文件与扩展名提取_01()
        {
            // 末尾片段带后缀名应判定为文件类型
            var fileSeg = PathSegment.Create("appsettings.Development.json", isLastSegment: true, hasTrailingSlash: false);

            Assert.Equal("appsettings.Development.json", fileSeg.Name);
            Assert.Equal(".json", fileSeg.Type);
            Assert.Equal(PathSegmentKind.File, fileSeg.Kind);
        }

        [Fact]
        public void 路径元分析_文件与扩展名提取_02()
        {
            // 带后缀名样式的命名但带有显式尾斜杠，仍应作为目录处理
            var folderSeg = PathSegment.Create("my.folder.v1", isLastSegment: true, hasTrailingSlash: true);

            Assert.Equal("my.folder.v1", folderSeg.Name);
            Assert.Equal(PathSegment.FolderType, folderSeg.Type);
            Assert.Equal(PathSegmentKind.Directory, folderSeg.Kind);
        }

        [Fact]
        public void 路径元分析_片段相等性比对_01()
        {
            // 路径元大小写不敏感判断
            var seg1 = new PathSegment("Test.txt", ".txt", PathSegmentKind.File);
            var seg2 = new PathSegment("test.TXT", ".TXT", PathSegmentKind.File);
            var seg3 = new PathSegment("Other.txt", ".txt", PathSegmentKind.File);

            Assert.True(seg1.Equals(seg2));
            Assert.Equal(seg1.GetHashCode(), seg2.GetHashCode());
            Assert.False(seg1.Equals(seg3));
        }
    }
}
