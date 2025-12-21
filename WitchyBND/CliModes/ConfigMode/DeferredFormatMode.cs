using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using NativeFileDialogSharp;
using PromptPlusLibrary;
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
        while (true)
        {
            output.Clear();
            output.DoubleDash("Deferred formats");

            var select = output.Select<DeferFormat>("Configure deferred tools")
                .TextSelector(format => {
                    var checkbox = Configuration.Stored.DeferTools.ContainsKey(format) ? "[x]" : "[ ]";
                    var path = Configuration.Stored.DeferTools.ContainsKey(format)
                        ? Configuration.Stored.DeferTools[format].Path
                        : "Not configured";
                    return $"{checkbox} {format.GetAttribute<DisplayAttribute>().Name}: {path}";
                })
                .Run();

            if (select.IsAborted) return;

            var format = select.Content;
            var name = format.GetAttribute<DisplayAttribute>().Name;
            output.WriteLine(
                @"In the following dialog, please select the executable that will be used to process the given format.
Cancel the dialog to reset the configuration for that format.");
            output.KeyPress().Run();
            var openDialog = Dialog.FileOpen("exe");
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
                Configuration.Stored.DeferTools.Remove(format);
                Configuration.SaveConfiguration();
                output.WriteLine($"{name} files will no longer be processed.");
                output.KeyPress().Run();
                continue;
            }


            ISelectControl<string> argsSelect = output.Select<string>("Select the arguments provided to the tool");

            var hasDefArgs =
                DeferredFormatHandling.DefaultDeferToolArguments.TryGetValue(format, out var defArgs);
            if (hasDefArgs)
            {
                argsSelect.AddItems(defArgs.Select(a => a.Name));
            }

            argsSelect.AddItems(new List<string>
            {
                "Default",
                "Custom..."
            });
            var argsVal = argsSelect.Run();

            if (argsVal.IsAborted) continue;

            bool breakOut = false;
            string unpackArgsDefault = "";
            string repackArgsDefault = "";
            bool hasRepack = true;
            switch (argsVal.Content)
            {
                case "Custom...":
                    break;
                case "Default":
                    Configuration.Stored.DeferTools[format] =
                        new DeferConfig(Path.GetFileNameWithoutExtension(openDialog.Path), openDialog.Path);
                    break;
                default:
                    var myArgs = defArgs!.First(a => a.Name == argsVal.Content);
                    unpackArgsDefault = myArgs.UnpackArgs;
                    if (myArgs.RepackArgs == null)
                        hasRepack = false;
                    else
                        repackArgsDefault = myArgs.RepackArgs;
                    Configuration.Stored.DeferTools[format] = new DeferConfig(myArgs.Name, openDialog.Path,
                        myArgs.UnpackArgs, myArgs.RepackArgs);
                    break;
            }

            output.WriteLine(
                @"Please input the arguments provided to the program when unpacking the format.
Available placeholders:

$path - Full path of file being processed
$dirname - Directory path of file being processed
$filename - Filename (without extension) of file being processed
$fileext - Extension of file being processed (starts with .)");

            var unpackArgsInput = output.Input("Enter custom unpack arguments")
                .Default(unpackArgsDefault)
                .Run();
            if (unpackArgsInput.IsAborted)
            {
                breakOut = true;
                break;
            }

            string? repackArgs = null;
            if (hasRepack)
            {
                output.WriteLine(
                    @"Please input the arguments provided to the program when repacking the format.

Press the Esc key if your program does not support repacking.

Available placeholders:

$path - Full path of file being processed
$dirname - Directory path of file being processed
$filename - Filename (without extension) of file being processed
$fileext - Extension of file being processed (starts with .)");
                var repackArgsInput = output.Input("Enter custom repack arguments")
                    .Default(repackArgsDefault)
                    .Run();

                repackArgs = !repackArgsInput.IsAborted ? repackArgsInput.Content : null;
            }

            Configuration.Stored.DeferTools[format] = new DeferConfig(
                Path.GetFileNameWithoutExtension(openDialog.Path), openDialog.Path, unpackArgsInput.Content,
                repackArgs);

            if (breakOut) continue;
            Configuration.SaveConfiguration();
            output.WriteLine($"{name} files will now be processed by program \"{openDialog.Path}\".");
            output.KeyPress().Run();
        }
    }
}