namespace ShutdownScheduler;

internal sealed record ShutdownSchedule(int DelaySeconds, DateTimeOffset ScheduledFor, string CommandText);
