namespace ReluctersteSDK.PathHelper
{
    /// <summary>
    /// 路径的抽象基类
    /// </summary>
    public abstract class FdPath
    {
        public string PathStr { get; protected set; }
    }
    /// <summary>
    /// 相对路径
    /// </summary>
    public class RelativePath : FdPath
    {
        public RelativePath(string path)
        {
            PathStr = path;
        }
    }

    /// <summary>
    /// 绝对路径
    /// </summary>
    public class AbsolutePath : FdPath
    {
        public AbsolutePath(string path)
        {
            PathStr = path;
        }
    }
}
