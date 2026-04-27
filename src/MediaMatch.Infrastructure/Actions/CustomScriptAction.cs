using System.Diagnostics;
using MediaMatch.Core.Models;
using MediaMatch.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaMatch.Infrastructure.Actions;

/// <summary>
/// Post-process action that runs an arbitrary PowerShell/bash script with environment variables
/// providing context about the renamed file.
/// </summary>
public sealed class CustomScriptAction : IPostProcessAction
{
    private readonly string _scriptPath;
    private readonly ILogger<CustomScriptAction> _logger;

    public string Name => "custom-script";
    public bool IsAvailable => !string.IsNullOrWhiteSpace(_scriptPath) && File.Exists(_scriptPath);

    public CustomScriptAction(string scriptPath, ILogger<CustomScriptAction>? logger = null)
    {
        _scriptPath = scriptPath;
        _logger = logger ?? NullLogger<CustomScriptAction>.Instance;
    }

    public async Task ExecuteAsync(FileOrganizationResult result, CancellationToken ct = default)
    {
        if (!IsAvailable)
        {
            _logger.LogWarning("Custom script skipped — script not found: {Path}", _scriptPath);
            return;
        }

        var filePath = result.NewPath ?? result.OriginalPath;
        var isWindows = OperatingSystem.IsWindows();

        var psi = new ProcessStartInfo
        {
            FileName = isWindows ? "powershell.exe" : "/bin/bash",
            Arguments = isWindows
                ? $"-NoProfile -ExecutionPolicy Bypass -File \"{_scriptPath}\""
                : $"\"{_scriptPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        // Set environment variables for the script
        psi.EnvironmentVariables["MEDIAMATCH_FILE"] = filePath;
        psi.EnvironmentVariables["MEDIAMATCH_TYPE"] = result.MediaType.ToString();
        psi.EnvironmentVariables["MEDIAMATCH_TITLE"] = Path.GetFileNameWithoutExtension(filePath);
        psi.EnvironmentVariables["MEDIAMATCH_ORIGINAL"] = result.OriginalPath;
        psi.EnvironmentVariables["MEDIAMATCH_SUCCESS"] = result.Success.ToString();

        try
        {
            using var process = new Process { StartInfo = psi };
            process.Start();

            var stdout = await process.StandardOutput.ReadToEndAsync(ct);
            var stderr = await process.StandardError.ReadToEndAsync(ct);

            await process.WaitForExitAsync(ct);

            if (process.ExitCode == 0)
            {
                _logger.LogInformation("Custom script completed for {File}", filePath);
                if (!string.IsNullOrWhiteSpace(stdout))
                    _logger.LogDebug("Script output: {Output}", stdout.Trim());
            }
            else
            {
                _logger.LogWarning("Custom script exited with code {Code}: {Error}", process.ExitCode, stderr.Trim());
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Custom script execution failed for {File}", filePath);
        }
    }
}
