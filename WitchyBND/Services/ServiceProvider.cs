using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using WitchyLib;

namespace WitchyBND.Services;

public static class ServiceProvider
{
    private static IServiceProvider _provider;

    public static T GetService<T>() where T : notnull
    {
        return _provider.GetRequiredService<T>();
    }

    public static IServiceProvider CreateProvider()
    {
        var silent = Configuration.Active.Silent;
        var platform = Configuration.Platform;
        IOutputService output;
            if (silent || (platform == OSPlatform.Windows && WBUtil.GetConsoleWindow() == IntPtr.Zero))
                output = new SilentOutputService();
            else if (platform == OSPlatform.Linux)
                output = new OutputService();
            else
                output = new OutputService();
        var error = new ErrorService(output);
        var game = new GameService(error, output);
        var update = new UpdateService(error, output);

        var collection = new ServiceCollection()
            .AddSingleton<IOutputService>(output)
            .AddSingleton<IErrorService>(error)
            .AddSingleton<IGameService>(game)
            .AddSingleton<IUpdateService>(update);

        return collection.BuildServiceProvider();
    }



    public static void ChangeProvider(IServiceProvider provider)
    {
        _provider = provider;
    }

    public static void InitializeProvider()
    {
        _provider = CreateProvider();
    }
}