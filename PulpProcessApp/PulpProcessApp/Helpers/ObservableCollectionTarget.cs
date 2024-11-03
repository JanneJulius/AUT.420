using NLog;
using NLog.Targets;

[Target("ObservableCollectionTarget")]
public class ObservableCollectionTarget : TargetWithLayout
{
    public required LogViewModel LogViewModel { get; set; }

    protected override void Write(LogEventInfo logEvent)
    {
        if (LogViewModel != null)
        {
            string logMessage = Layout.Render(logEvent);
            LogViewModel.AddLog(logMessage);
        }
    }
}