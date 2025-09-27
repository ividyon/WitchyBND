using System;
using PPlus;
using PPlus.Controls;

namespace WitchyBND.Services;

public class LinuxOutputService : IOutputService
{
    public object ConsoleWriterLock { get; }
    public int WriteLine(string? value = null, Style? style = null, bool clearrestofline = true)
    {
        throw new NotImplementedException();
    }

    public int WriteError(string? value = null, Style? style = null, bool clearrestofline = true)
    {
        throw new NotImplementedException();
    }

    public int DoubleDash(string value, DashOptions dashOptions = DashOptions.AsciiSingleBorder, int extralines = 0,
        Style? style = null)
    {
        throw new NotImplementedException();
    }

    public IControlSelect<T> Select<T>(string prompt, string? description = null)
    {
        throw new NotImplementedException();
    }

    public void Clear()
    {
        throw new NotImplementedException();
    }

    public IDisposable EscapeColorTokens()
    {
        throw new NotImplementedException();
    }

    public IControlKeyPress Confirm(string prompt, Action<IPromptConfig> config = null)
    {
        throw new NotImplementedException();
    }

    public int SingleDash(string value, DashOptions dashOptions = DashOptions.AsciiSingleBorder, int extralines = 0,
        Style? style = null)
    {
        throw new NotImplementedException();
    }

    public IControlInput Input(string prompt, Action<IPromptConfig> config = null)
    {
        throw new NotImplementedException();
    }

    public IControlKeyPress KeyPress()
    {
        throw new NotImplementedException();
    }

    public IControlKeyPress KeyPress(string prompt, Action<IPromptConfig> config = null)
    {
        throw new NotImplementedException();
    }

    public ConsoleKeyInfo ReadKey(bool intercept = false)
    {
        throw new NotImplementedException();
    }
}