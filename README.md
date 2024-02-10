# Prune

A simple c# console app that mimics the pruning behavior of [`Proxmox Backup Server's Prune policy`](https://pbs.proxmox.com/docs/prune-simulator/index.html) and utilizes [`Serilog`](https://github.com/serilog/serilog) for logging.

## Requirements
- .NET 8.0 SDK

## Installation
1. Clone `Prune` repository and build it, or download the corresponding release file.
1. Make a copy of the `appsettings-example.json` file and name it `appsettings.json`.
    - The appsettings will need to be filled out otherwise all files in the current execution directory will be remove.
1. Download and install `.NET 8.0 SDK` (See [`.NET 8.0 SDK Download`](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)).

## Configurable appsetting.json Options
These options can be configured by setting the values for the keys in the appsettings.json file.

| Key  | Default | Example | Description |
| ---- | ---- | ---- | ---- |
| `Serilog:MinimumLevel:Default` | `Debug` | `Information` | The minimum log event level written to the log file. See [`Serilog Minimum Level`](https://github.com/serilog/serilog/wiki/Configuration-Basics#minimum-level). |
| `Serilog:WriteTo:Args:path` | - | `Logs/log.txt` | The file name or path to the file name. If the directories to the file names don't exist, it will be created. |
| `Serilog:WriteTo:Args:rollingInterval` | `Infinite` | `Day` | The frequency at which the log file should roll. See [`Serilog Rolling Interval`](https://github.com/serilog/serilog-sinks-file/blob/dev/src/Serilog.Sinks.File/RollingInterval.cs). |
| `Serilog:WriteTo:Args:retainedFileCountLimit` | `31` | `null` | The number of files to retain. |
| `Serilog:WriteTo:Args:shared` | `false` | `true` | By default, only one process may write to a log file at a given time. Setting this allows multi-process shared log files. |
| `Serilog:WriteTo:Args:outputTemplate` | `{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}` | `{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u5}] {Message:lj}{NewLine}{Exception}` | The format for each log entry. See [`Serilog Formatting Output`](https://github.com/serilog/serilog/wiki/Formatting-Output). |
| `DryRun` | `false` | `true` | The application will display the action(s) without actually deleting the file(s). |
| `ForceConfirm` | `false` | `true` | The application will prompt for confirmation for deleting each file. |
| `IgnoreStrings` | `[]` | `[ "example-file.txt" ]` | The application will ignore any file(s) that contain the given string(s).  By default, the required application files (`appsettings.json`, `Bitwarden-Backup.exe`, `Bitwarden-Backup.pdb`, `bw.exe`) are autoamtically ignored. |
| `StartOfWeek`* | `0` | `2` | The day a week begins where `0` indicates `Sunday` and `6` indicates `Friday`. |
| `Default:Path` | `Executing Directory` | `C:\Temp` | The default prune path if the key or invalid value is set in PruneDirectories. |
| `Default:KeepLast` | `0` | `5` | The default number of latest backups to keep if the key or invalid value is set in PruneDirectories. |
| `Default:KeepHourly` | `0` | `4` | The default number of hourly backups to keep if the key or invalid value is set in PruneDirectories. |
| `Default:KeepDaily` | `0` | `3` | The default number of daily backups to keep if the key or invalid value is set in PruneDirectories. |
| `Default:KeepWeekly` | `0` | `2` | The default number of weekly backups to keep if the key or invalid value is set in PruneDirectories. |
| `Default:KeepMonthly` | `0` | `1` | The default number of monthly backups to keep if the key or invalid value is set in PruneDirectories. |
| `Default:KeepYearly` | `0` | `0` | The default number of yearly backups to keep if the key or invalid value is set in PruneDirectories. |
| `PruneDirectories:Path` | `Executing Directory` | `C:\Temp` | The prune path. |
| `PruneDirectories:KeepLast` | `0` | `5` | The number of latest backups to keep. |
| `PruneDirectories:KeepHourly` | `0` | `4` | The number of hourly backups to keep. |
| `PruneDirectories:KeepDaily` | `0` | `3` | The number of daily backups to keep. |
| `PruneDirectories:KeepWeekly` | `0` | `2` | The number of weekly backups to keep. |
| `PruneDirectories:KeepMonthly` | `0` | `1` | The number of monthly backups to keep. |
| `PruneDirectories:KeepYearly` | `0` | `0` | The number of yearly backups to keep. |

\* For this `StartOfWeek` key, it will only apply when the `PruneDirectories:KeepWeekly` or `Default:KeepWeekly` keys that have a valid value.