using System.Diagnostics;

namespace ShutdownScheduler;

internal sealed class ShutdownService
{
    public Task<CommandResult> ScheduleShutdownAsync(int seconds) =>
        RunAsync("-s", "-t", seconds.ToString(System.Globalization.CultureInfo.InvariantCulture));

    public Task<CommandResult> AbortShutdownAsync() => RunAsync("-a");

    private static async Task<CommandResult> RunAsync(params string[] arguments)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "shutdown.exe",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        process.Start();
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await Task.WhenAll(outputTask, errorTask, process.WaitForExitAsync());

        return new CommandResult(process.ExitCode, outputTask.Result.Trim(), errorTask.Result.Trim());
    }
}

internal sealed record CommandResult(int ExitCode, string StandardOutput, string StandardError)
{
    public bool Succeeded => ExitCode == 0;

    public string ErrorMessage => string.IsNullOrWhiteSpace(StandardError)
        ? $"Lệnh trả về mã lỗi {ExitCode}."
        : StandardError;
}
