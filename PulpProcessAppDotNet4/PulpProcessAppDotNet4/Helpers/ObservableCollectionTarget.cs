using NLog;
using NLog.Targets;
using System;

/// <summary>
/// A custom NLog target that logs messages via a <see cref="LogViewModel"/>.
/// </summary>
/// <remarks>
/// This target allow logs to be displayed in real-time in a UI.
/// </remarks>
[Target("ObservableCollectionTarget")]
public class ObservableCollectionTarget : TargetWithLayout
{
    /// <summary>
    /// Gets or sets the <see cref="LogViewModel"/> instance used to manage log messages.
    /// </summary>
    /// <remarks>
    /// The <see cref="LogViewModel"/> must be set before logging messages; otherwise, an <see cref="InvalidOperationException"/> will be thrown.
    /// </remarks>
    public LogViewModel LogViewModel { get; set; } = null;

    /// <summary>
    /// Writes a log event to the <see cref="LogViewModel"/>.
    /// </summary>
    /// <param name="logEvent">The log event containing the log message and related metadata.</param>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="LogViewModel"/> property is not set.</exception>
    protected override void Write(LogEventInfo logEvent)
    {
        if (LogViewModel == null)
            throw new InvalidOperationException("LogViewModel must be set before logging.");

        // Render the log message using the layout and add it to the LogViewModel
        string logMessage = Layout.Render(logEvent);
        LogViewModel.AddLog(logMessage);
    }
}
