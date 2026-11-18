namespace ReluctersteSDK.Debugger
{
    public static class Debugger
    {
        public static ReportException Signal(DebugMessage message)
        {
            DebugReporter.Report(message);
            return new ReportException(message.Message);
        }
    }
}
