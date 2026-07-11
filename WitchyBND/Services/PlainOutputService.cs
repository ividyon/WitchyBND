using System;
using PromptPlusLibrary;

namespace WitchyBND.Services;

public sealed class PlainOutputService : IOutputService
{
    public object ConsoleWriterLock { get; } = new();

    public void WriteLine(string? value = null, Style? style = null, bool clearrestofline = true)
    {
        lock (ConsoleWriterLock)
            Console.Out.WriteLine(value);
    }

    public void WriteError(string? value = null, Style? style = null, bool clearrestofline = true)
    {
        lock (ConsoleWriterLock)
            Console.Error.WriteLine(value);
    }

    public void DoubleDash(string value, DashOptions dashOptions = DashOptions.AsciiSingleBorder, int extralines = 0,
        Style? style = null)
    {
        WriteLine(value);
    }

    public void SingleDash(string value, DashOptions dashOptions = DashOptions.AsciiSingleBorder, int extralines = 0,
        Style? style = null)
    {
        WriteLine(value);
    }

    public void Clear()
    {
    }

    public ISelectControl<T> Select<T>(string prompt, string? description = null) =>
        throw InteractiveOperationException();

    public IKeyPressControl Confirm(string prompt, Action<IControlOptions>? opt = null) =>
        throw InteractiveOperationException();

    public IInputControl Input(string prompt, Action<IControlOptions>? opt = null) =>
        throw InteractiveOperationException();

    public IKeyPressControl KeyPress() => throw InteractiveOperationException();

    public IKeyPressControl KeyPress(string prompt, Action<IControlOptions>? opt = null) =>
        throw InteractiveOperationException();

    public ConsoleKeyInfo ReadKey(bool intercept = false) => throw InteractiveOperationException();

    private static InvalidOperationException InteractiveOperationException() =>
        new("Interactive prompts are unavailable for this command.");
}
