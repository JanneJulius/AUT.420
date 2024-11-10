using NLog;
using NLog.Targets;
using System;

[Target("ObservableCollectionTarget")]
public class ObservableCollectionTarget : TargetWithLayout
{
    public LogViewModel LogViewModel { get; set; } = null;

    protected override void Write(LogEventInfo logEvent)
    {
        if (LogViewModel == null)
            throw new InvalidOperationException("LogViewModel must be set before logging.");

        string logMessage = Layout.Render(logEvent);
        LogViewModel.AddLog(logMessage);
    }
}