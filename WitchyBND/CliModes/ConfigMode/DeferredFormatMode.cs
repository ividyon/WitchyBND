using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using PPlus;
using WitchyBND.Services;
using WitchyLib;

namespace WitchyBND.CliModes;
public static class DeferredFormatMode
{
    private static readonly IOutputService output;
    static DeferredFormatMode()
    {
        output = ServiceProvider.GetService<IOutputService>();
    }
    public static void Run(CliOptions opt)
    {
        using (output.EscapeColorTokens())
        {
            while (true)
            {
                output.Clear();
                output.DoubleDash("Deferred formats");

                var select = output.Select<DeferFormat>("Configure deferred tools")
                    .TextSelector(format => {
                        var checkbox = Configuration.DeferTools.ContainsKey(format) ? $"[x]" : "[ ]";
                        var path = Configuration.DeferTools.ContainsKey(format)
                            ? Configuration.DeferTools[format].Path
                            : "Not configured";
                        return $"{checkbox} {format.GetAttribute<DisplayAttribute>().Name}: {path}";
                    })
                    .Run();

                if (select.IsAborted) return;

                var format = select.Value;
                var name = format.GetAttribute<DisplayAttribute>().Name;
                output.WriteLine(
                    @"In the following dialog, please select the executable that will be used to process the given format.
Cancel the dialog to reset the configuration for that format.");
                output.KeyPress().Run();
                var openDialog = NativeFileDialogSharp.Dialog.FileOpen("exe");
                if (openDialog.IsError)
                {
                    output.WriteError(
                        $"There was an error while picking the executable: {openDialog.ErrorMessage}");
                    output.KeyPress().Run();
                    continue;
                }

                if (openDialog.IsCancelled || string.IsNullOrWhiteSpace(openDialog.Path) ||
                    !File.Exists(openDialog.Path))
                {
                    Configuration.DeferTools.Remove(format);
                    Configuration.UpdateConfiguration();
                    output.WriteLine($"{name} files will no longer be processed.");
                    output.KeyPress().Run();
                    continue;
                }


                var argsSelect = output.Select<string>("Select the arguments provided to the tool")
                    .AddItems(DeferredFormatHandling.DefaultDeferToolArguments.SelectMany(a => a.Value)
                        .Select(a => a.Item1).Union(new List<string>()
                        {
                            "Default",
                            "Custom..."
                        }))
                    .Run();

                if (argsSelect.IsAborted) continue;

                bool breakOut = false;
                switch (argsSelect.Value)
                {
                    case "Custom...":
                            output.WriteLine(@"Please input your custom arguments for the program being called.
Available placeholders:

$path - Full path of file being processed
$dirname - Directory path of file being processed
$filename - Filename (without extension) of file being processed
$fileext - Extension of file being processed (starts with .)");

                            var customArgsInput = output.Input("Enter custom arguments").Run();
                            if (customArgsInput.IsAborted)
                            {
                                breakOut = true;
                                break;
                            }

                            Configuration.DeferTools[format] = new DeferFormatConfiguration(openDialog.Path, customArgsInput.Value);
                            break;
                        break;
                    case "Default":
                        Configuration.DeferTools[format] = new DeferFormatConfiguration(openDialog.Path);
                        break;
                    default:
                        foreach ((string selName, string args) in DeferredFormatHandling.DefaultDeferToolArguments[format])
                        {
                            if (argsSelect.Value == selName)
                            {
                                Configuration.DeferTools[format] = new DeferFormatConfiguration(openDialog.Path, args);
                                break;
                            }
                        }
                        break;
                }

                if (breakOut) continue;
                Configuration.UpdateConfiguration();
                output.WriteLine($"{name} files will now be processed by program \"{openDialog.Path}\".");
                output.KeyPress().Run();
            }
        }
    }
}