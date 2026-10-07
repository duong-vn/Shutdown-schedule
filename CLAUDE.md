# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

Requires .NET 8 SDK on Windows. This is a Windows Forms desktop app targeting `net8.0-windows` and `win-x64`.

```powershell
dotnet run                         # Build and launch app
dotnet build -c Release           # Build Release
dotnet publish -c Release         # Publish self-contained, single-file Windows x64 app
dotnet test                       # No test project currently exists
```

No lint or formatter command is configured. No test project exists, so there is no single-test command.

## Architecture

- `Program.cs` is the STA entry point; it initializes WinForms and opens `MainForm`.
- `MainForm.cs` owns both scheduling and active-countdown views, custom-painted controls, dialogs, selection state, and UI event flow. Preview timer updates the displayed target and command; countdown timer refreshes remaining time and progress. Keep UI state transitions here.
- `ShutdownService.cs` is the OS boundary. It validates delay and invokes the absolute Windows `shutdown.exe` path with argument-list parameters, redirected output, and a timeout. `ScheduleShutdownAsync` issues `-s -t`; `AbortShutdownAsync` issues `-a`.
- `ShutdownSchedule.cs` is the immutable record for an active schedule: delay, target time, and displayed command.
- Scheduling requires explicit confirmation. Closing while a schedule is active asks whether to abort it, keep it while exiting, or return to the app. Windows owns the scheduled shutdown after the command succeeds; app exit does not cancel it.

The project has no external NuGet dependencies or separate test project. `ShutdownScheduler.csproj` configures the Windows x64 self-contained, single-file publish and application icon. Release publish output is under `bin/Release/net8.0-windows/win-x64/publish/`.