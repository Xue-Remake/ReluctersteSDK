using ReluctersteSDK.Registry;

namespace ReluctersteSDK.Registry.Test
{
    /// <summary>
    /// <see cref="CompositeKey"/> 的相等性、哈希与只读视图测试。
    /// </summary>
    public class CompositeKeyTests
    {
        [Fact]
        public void Keys_不能通过强制转换修改()
        {
            var key = new CompositeKey(1, "a");

            Assert.Throws<NotSupportedException>(() => ((IList<object?>)key.Keys)[0] = 2);
            Assert.Equal(1, key[0]);
        }

        [Fact]
        public void 相同内容相等且哈希一致()
        {
            var a = new CompositeKey(1, "x");
            var b = new CompositeKey(1, "x");
            var c = new CompositeKey(1, "y");

            Assert.Equal(a, b);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
            Assert.NotEqual(a, c);
        }

        [Fact]
        public void 默认值不抛异常且等同于空键()
        {
            CompositeKey key = default;
            var empty = new CompositeKey(Array.Empty<object?>());

            Assert.Equal(0, key.Length);
            Assert.Empty(key.Keys);
            Assert.Equal("[]", key.ToString());
            Assert.True(key.Equals(default(CompositeKey)));
            Assert.Equal(key, empty);
            Assert.Equal(key.GetHashCode(), empty.GetHashCode());
        }

        [Fact]
        public void 索引越界抛异常()
        {
            CompositeKey key = default;
            Assert.Throws<IndexOutOfRangeException>(() => _ = key[0]);
        }
    }
}
