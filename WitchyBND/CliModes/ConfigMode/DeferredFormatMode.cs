using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using PPlus;
using WitchyLib;

namespace WitchyBND.CliModes;
public static class DeferredFormatMode
{
    private enum DeferredFormatChoices
    {
        RegisterDeferredTool,
        UnregisterDeferredTool,
    }

    public static void Run(CliOptions opt)
    {
        using (PromptPlus.EscapeColorTokens())
        {
            while (true)
            {
                PromptPlus.Clear();
                PromptPlus.DoubleDash("Deferred formats");

                var select = PromptPlus.Select<DeferFormat>("Configure deferred tools")
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
                PromptPlus.WriteLine(
                    @"In the following dialog, please select the executable that will be used to process the given format.
Cancel the dialog to reset the configuration for that format.");
                PromptPlus.KeyPress().Run();
                var openDialog = NativeFileDialogSharp.Dialog.FileOpen("exe");
                if (openDialog.IsError)
                {
                    PromptPlus.Error.WriteLine(
                        $"There was an error while picking the executable: {openDialog.ErrorMessage}");
                    PromptPlus.KeyPress().Run();
                    continue;
                }

                if (openDialog.IsCancelled || string.IsNullOrWhiteSpace(openDialog.Path) ||
                    !File.Exists(openDialog.Path))
                {
                    Configuration.DeferTools.Remove(format);
                    Configuration.UpdateConfiguration();
                    PromptPlus.WriteLine($"{name} files will no longer be processed.");
                    PromptPlus.KeyPress().Run();
                    continue;
                }


                var argsSelect = PromptPlus.Select<string>("Select the arguments provided to the tool")
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
                            PromptPlus.WriteLine(@"Please input your custom arguments for the program being called.
Available placeholders:

$path - Full path of file being processed
$dirname - Directory path of file being processed
$filename - Filename (without extension) of file being processed
$fileext - Extension of file being processed (starts with .)");

                            var customArgsInput = PromptPlus.Input("Enter custom arguments").Run();
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
                PromptPlus.WriteLine($"{name} files will now be processed by program \"{openDialog.Path}\".");
                PromptPlus.KeyPress().Run();
            }
        }
    }
}