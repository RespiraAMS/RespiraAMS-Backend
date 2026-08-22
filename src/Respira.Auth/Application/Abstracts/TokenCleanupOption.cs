namespace Application.Abstracts;

/// <summary>
/// Configuration for the expired-token cleanup background service.
/// </summary>
public class TokenCleanupOption
{
    /// <summary>How often the cleanup runs, in minutes. Defaults to 60</summary>
    public int IntervalMinutes { get; set; } = 60;
}
