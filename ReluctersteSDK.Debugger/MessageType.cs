using ReluctersteSDK.DataStructures.Structures;

namespace ReluctersteSDK.Debugger
{
    public enum MessageType
    {
        Trace,
        Info,
        Warning,
        SilentError,
        Marker
    }
    internal static class TypeStringConverter
    {
        private static BiDictionary<MessageType, string> _dict = new()
        {
            (MessageType.Trace, "Trace"),
            (MessageType.Info, "Info"),
            (MessageType.Warning, "Warning"),
            (MessageType.SilentError, "SilentError"),
            (MessageType.Marker, "Marker")
        };

        public static string Convert(MessageType type)
        {
            return _dict.GetValue(type);
        }
        public static MessageType Convert(string str)
        {
            if (_dict.TryGetKey(str, out var msg))
            {
                return msg;
            }
            else throw new Exception();
        }
    }
}
