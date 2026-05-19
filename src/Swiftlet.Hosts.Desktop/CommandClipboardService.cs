using System.Diagnostics;
using System.Runtime.InteropServices;
using Swiftlet.HostAbstractions;

namespace Swiftlet.Hosts.Desktop;

public sealed class CommandClipboardService : IClipboardService
{
    public async Task<HostActionResult> SetTextAsync(string text, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        cancellationToken.ThrowIfCancellationRequested();

        foreach (ClipboardCommand command in GetCommandsForCurrentPlatform())
        {
            HostActionResult result = await TryRunCommandAsync(command, text, cancellationToken);
            if (result.IsSuccess)
            {
                return result;
            }
        }

        return HostActionResult.Manual(
            "Clipboard copy is not available in this environment.",
            text);
    }

    private static IEnumerable<ClipboardCommand> GetCommandsForCurrentPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            yield return new ClipboardCommand("cmd.exe", "/c clip");
            yield break;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            yield return new ClipboardCommand("pbcopy", string.Empty);
            yield break;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            yield return new ClipboardCommand("xclip", "-selection clipboard");
            yield return new ClipboardCommand("xsel", "--clipboard --input");
        }
    }

    private static async Task<HostActionResult> TryRunCommandAsync(
        ClipboardCommand command,
        string text,
        CancellationToken cancellationToken)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command.FileName,
                    Arguments = command.Arguments,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                },
            };

            if (!process.Start())
            {
                return HostActionResult.Failure($"Failed to start clipboard command '{command.FileName}'.");
            }

            await process.StandardInput.WriteAsync(text.AsMemory(), cancellationToken);
            await process.StandardInput.FlushAsync();
            process.StandardInput.Close();
            await process.WaitForExitAsync(cancellationToken);

            return process.ExitCode == 0
                ? HostActionResult.Success("Text copied to clipboard.")
                : HostActionResult.Failure(await process.StandardError.ReadToEndAsync(cancellationToken));
        }
        catch
        {
            return HostActionResult.Failure($"Clipboard command '{command.FileName}' is unavailable.");
        }
    }

    private sealed record ClipboardCommand(string FileName, string Arguments);
}
