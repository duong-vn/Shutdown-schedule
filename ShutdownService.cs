using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

namespace ShutdownScheduler;

internal sealed class ShutdownService
{
    public Task<CommandResult> ScheduleShutdownAsync(int seconds)
    {
        if (seconds <= 0 || seconds > 315_360_000)
        {
            return Task.FromResult(new CommandResult(-1, string.Empty, "Thời lượng tắt máy không hợp lệ."));
        }

        return RunAsync("-s", "-t", seconds.ToString(CultureInfo.InvariantCulture));
    }

    public Task<CommandResult> AbortShutdownAsync() => RunAsync("-a");

    private static async Task<CommandResult> RunAsync(params string[] arguments)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(Environment.SystemDirectory, "shutdown.exe"),
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

        try
        {
            if (!process.Start())
            {
                return new CommandResult(-1, string.Empty, "Không thể khởi chạy shutdown.exe.");
            }

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var completedTask = await Task.WhenAny(
                Task.WhenAll(outputTask, errorTask, process.WaitForExitAsync(timeoutCts.Token)),
                Task.Delay(Timeout.Infinite, timeoutCts.Token)
            );

            return new CommandResult(process.ExitCode, outputTask.Result.Trim(), errorTask.Result.Trim());
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                }
            }
            catch
            {
                // Ignore best-effort cleanup
            }

            return new CommandResult(-1, string.Empty, "Lệnh shutdown.exe phản hồi quá thời gian cho phép.");
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            return new CommandResult(-1, string.Empty, ex.Message);
        }
    }
}

internal sealed record CommandResult(int ExitCode, string StandardOutput, string StandardError)
{
    public bool Succeeded => ExitCode == 0;

    public string ErrorMessage => string.IsNullOrWhiteSpace(StandardError)
        ? $"Lệnh trả về mã lỗi {ExitCode}."
        : StandardError;
}
