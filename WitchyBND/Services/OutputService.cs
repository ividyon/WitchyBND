using System;
using System.Globalization;
using PromptPlusLibrary;
using PromptPlusLibrary.PublicLibrary;
using WitchyLib;

namespace WitchyBND.Services;

public interface IOutputService
{
    public object ConsoleWriterLock { get; }
    public (int Left, int Top) WriteLine(
        string? value = null,
        Style? style = null,
        bool clearrestofline = true);
    public (int Left, int Top) WriteLineColor(
        string? value = null,
        Overflow overflow = Overflow.Crop,
        bool clearrestofline = true);
    public (int Left, int Top) WriteError(
        string? value = null,
        Style? style = null,
        bool clearrestofline = true);

    public void DoubleDash(
        string value,
        DashOptions dashOptions = DashOptions.AsciiSingleBorder,
        int extralines = 0,
        Style? style = null);
    public ISelectControl<T> Select<T>(string prompt, string? description = null);
    public void Clear();

    // public IDisposable EscapeColorTokens();

    public IKeyPressControl Confirm(string prompt, Action<IControlOptions> config = null);
    public void SingleDash(
        string value,
        DashOptions dashOptions = DashOptions.AsciiSingleBorder,
        int extralines = 0,
        Style? style = null);

    public IInputControl Input(string prompt, Action<IControlOptions> config = null);
    public IKeyPressControl KeyPress();
    public IKeyPressControl KeyPress(string prompt, Action<IControlOptions> config = null);
    public ConsoleKeyInfo ReadKey(bool intercept = false);
}
public class OutputService : IOutputService
{
    public OutputService()
    {
        PromptPlus.Config.DefaultCulture = new CultureInfo("en-us");
        if (!OperatingSystem.IsWindows())
        {
            PromptPlus.Console.ResetColor();
        }
    }

    public object ConsoleWriterLock => new();
    public (int Left, int Top) WriteLine(string? value = null, Style? style = null, bool clearrestofline = true)
    {
        if (Configuration.Active.Silent) return (0, 0);
        try
        {
            (int Left, int Top) outCode;
            lock (ConsoleWriterLock)
                outCode = PromptPlus.Console.Write(value + Environment.NewLine, style, clearrestofline);
            return outCode;
        }
        catch
        {
            return (-1, -1);
        }
    }
    public (int Left, int Top) WriteLineColor(string? value = null, Overflow overflow = Overflow.Crop, bool clearrestofline = true)
    {
        if (Configuration.Active.Silent) return (0, 0);
        try
        {
            (int Left, int Top) outCode;
            lock (ConsoleWriterLock)
                outCode = PromptPlus.Console.WriteColor(value + Environment.NewLine, Overflow.Crop, clearrestofline);
            return outCode;
        }
        catch
        {
            return (-1, -1);
        }
    }

    public (int Left, int Top) WriteError(string? value = null, Style? style = null, bool clearrestofline = true)
    {
        if (Configuration.Active.Silent) return (0, 0);
        try
        {
            (int Left, int Top) outCode;
            lock (ConsoleWriterLock)
            {
                using (PromptPlus.Console.OutputError())
                {
                    outCode = PromptPlus.Console.Write(value + Environment.NewLine, style, clearrestofline);
                }
            }

            return outCode;
        }
        catch
        {
            return (-1, -1);
        }
    }

    public void DoubleDash(string value, DashOptions dashOptions = DashOptions.AsciiSingleBorder, int extralines = 0,
        Style? style = null)
    {
        if (Configuration.Active.Silent) return;
        lock (ConsoleWriterLock)
            PromptPlus.Widgets.DoubleDash(value, dashOptions, extralines, style);
    }

    public ISelectControl<T> Select<T>(string prompt, string? description = null)
    {
        lock (ConsoleWriterLock)
            return PromptPlus.Controls.Select<T>(prompt, description);
    }

    public void Clear()
    {
        if (Configuration.Active.Silent) return;
        lock (ConsoleWriterLock)
            PromptPlus.Console.Clear();
    }

    // public IDisposable EscapeColorTokens()
    // {
    //     return PromptPlus.Config.EscapeColorTokens();
    // }

    public IKeyPressControl Confirm(string prompt, Action<IControlOptions> opt = null)
    {
        lock (ConsoleWriterLock)
            return PromptPlus.Controls.Confirm(prompt).Options(opt);
    }

    public void SingleDash(string value, DashOptions dashOptions = DashOptions.AsciiSingleBorder, int extralines = 0,
        Style? style = null)
    {
        if (Configuration.Active.Silent) return;
        lock (ConsoleWriterLock)
            PromptPlus.Widgets.SingleDash(value, dashOptions, extralines, style);
    }

    public IInputControl Input(string prompt, Action<IControlOptions> opt = null)
    {
        lock (ConsoleWriterLock)
            return PromptPlus.Controls.Input(prompt).Options(opt);
    }

    public IKeyPressControl KeyPress()
    {
        lock (ConsoleWriterLock)
            return PromptPlus.Controls.KeyPress();
    }

    public IKeyPressControl KeyPress(string prompt, Action<IControlOptions>? opt = null)
    {
        opt ??= _ => { };
        lock (ConsoleWriterLock)
            return PromptPlus.Controls.KeyPress(prompt).Options(opt);
    }

    public ConsoleKeyInfo ReadKey(bool intercept = false)
    {
        lock (ConsoleWriterLock)
            return PromptPlus.Console.ReadKey(intercept);
    }
}