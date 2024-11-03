using NLog;
using NLog.Targets;

[Target("ObservableCollectionTarget")]
public class ObservableCollectionTarget : TargetWithLayout
{
    private readonly LogViewModel _logViewModel;

    public ObservableCollectionTarget(LogViewModel logViewModel)
    {
        _logViewModel = logViewModel;
    }

    protected override void Write(LogEventInfo logEvent)
    {
        string logMessage = Layout.Render(logEvent);
        _logViewModel.AddLog(logMessage);
    }
}
