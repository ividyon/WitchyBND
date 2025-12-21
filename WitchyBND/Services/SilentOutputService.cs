using System;
using PromptPlusLibrary;

namespace WitchyBND.Services;

public class SilentOutputService : IOutputService
{
    public object ConsoleWriterLock { get; }

    public (int, int) WriteLine(string? value = null, Style? style = null, bool clearrestofline = true)
    {
        return (-1, -1);
    }

    public (int Left, int Top) WriteLineColor(string? value = null, Overflow overflow = Overflow.Crop,
        bool clearrestofline = true)
    {
        throw new NotImplementedException();
    }

    public (int, int) WriteError(string? value = null, Style? style = null, bool clearrestofline = true)
    {
        return (-1, -1);
    }

    public void DoubleDash(string value, DashOptions dashOptions = DashOptions.AsciiSingleBorder, int extralines = 0,
        Style? style = null)
    {
    }

    public ISelectControl<T> Select<T>(string prompt, string? description = null)
    {
        throw new NotImplementedException();
    }

    public void Clear()
    {
        return;
    }

    public IDisposable EscapeColorTokens()
    {
        throw new NotImplementedException();
    }

    public IKeyPressControl Confirm(string prompt, Action<IControlOptions> opt = null)
    {
        throw new NotImplementedException();
    }

    public void SingleDash(string value, DashOptions dashOptions = DashOptions.AsciiSingleBorder, int extralines = 0,
        Style? style = null)
    {
    }

    public IInputControl Input(string prompt, Action<IControlOptions> opt = null)
    {
        throw new NotImplementedException();
    }

    public IKeyPressControl KeyPress()
    {
        throw new NotImplementedException();
    }

    public IKeyPressControl KeyPress(string prompt, Action<IControlOptions> opt = null)
    {
        throw new NotImplementedException();
    }

    public ConsoleKeyInfo ReadKey(bool intercept = false)
    {
        throw new NotImplementedException();
    }
}