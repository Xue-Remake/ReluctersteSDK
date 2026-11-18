using System.Diagnostics;

namespace ReluctersteSDK.Debugger
{
    public static class DebugReporter
    {
        public static void Report(DebugMessage message)
        {
            string type = string.Empty;
            if (message.Type is not null)
            {
                type = TypeStringConverter.Convert((MessageType)message.Type);
            }

            Debug.WriteLine($"[DebugReporter]  [{type}]  {message.Message}");
        }
    }
}
