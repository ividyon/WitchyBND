using System;
using System.Globalization;
using System.IO;
using PPlus;
using PPlus.Controls;

namespace WitchyBND.Services;

public interface IOutputService
{
    public object ConsoleWriterLock { get; }
    public int WriteLine(
        string? value = null,
        Style? style = null,
        bool clearrestofline = true);

    public int DoubleDash(
        string value,
        DashOptions dashOptions = DashOptions.AsciiSingleBorder,
        int extralines = 0,
        Style? style = null);
    public IControlSelect<T> Select<T>(string prompt, string? description = null);
    public void Clear();

    public IDisposable EscapeColorTokens();

    public TextWriter Error { get; }

    public IControlKeyPress Confirm(string prompt, Action<IPromptConfig> config = null);
    public int SingleDash(
        string value,
        DashOptions dashOptions = DashOptions.AsciiSingleBorder,
        int extralines = 0,
        Style? style = null);

    public IControlInput Input(string prompt, Action<IPromptConfig> config = null);
    public IControlKeyPress KeyPress();
    public IControlKeyPress KeyPress(string prompt, Action<IPromptConfig> config = null);
    public ConsoleKeyInfo ReadKey(bool intercept = false);
}
public class OutputService : IOutputService
{
    public OutputService()
    {
        PromptPlus.Config.DefaultCulture = new CultureInfo("en-us");
    }

    public object ConsoleWriterLock => new();
    public int WriteLine(string? value = null, Style? style = null, bool clearrestofline = true)
    {
        if (Configuration.Args.Silent) return 0;
        lock (ConsoleWriterLock)
            return PromptPlus.WriteLine(value, style, clearrestofline);
    }

    public int DoubleDash(string value, DashOptions dashOptions = DashOptions.AsciiSingleBorder, int extralines = 0,
        Style? style = null)
    {
        if (Configuration.Args.Silent) return 0;
        lock (ConsoleWriterLock)
            return PromptPlus.DoubleDash(value, dashOptions, extralines, style);
    }

    public IControlSelect<T> Select<T>(string prompt, string? description = null)
    {
        lock (ConsoleWriterLock)
            return PromptPlus.Select<T>(prompt, description);
    }

    public void Clear()
    {
        if (Configuration.Args.Silent) return;
        lock (ConsoleWriterLock)
            PromptPlus.Clear();
    }

    public IDisposable EscapeColorTokens()
    {
        return PromptPlus.EscapeColorTokens();
    }

    public TextWriter Error => PromptPlus.Error;

    public IControlKeyPress Confirm(string prompt, Action<IPromptConfig> config = null)
    {
        lock (ConsoleWriterLock)
            return PromptPlus.Confirm(prompt, config);
    }

    public int SingleDash(string value, DashOptions dashOptions = DashOptions.AsciiSingleBorder, int extralines = 0,
        Style? style = null)
    {
        if (Configuration.Args.Silent) return 0;
        lock (ConsoleWriterLock)
            return PromptPlus.SingleDash(value, dashOptions, extralines, style);
    }

    public IControlInput Input(string prompt, Action<IPromptConfig> config = null)
    {
        lock (ConsoleWriterLock)
            return PromptPlus.Input(prompt, config);
    }

    public IControlKeyPress KeyPress()
    {
        lock (ConsoleWriterLock)
            return PromptPlus.KeyPress();
    }

    public IControlKeyPress KeyPress(string prompt, Action<IPromptConfig> config = null)
    {
        lock (ConsoleWriterLock)
            return PromptPlus.KeyPress(prompt, config);
    }

    public ConsoleKeyInfo ReadKey(bool intercept = false)
    {
        lock (ConsoleWriterLock)
            return PromptPlus.ReadKey(intercept);
    }
}