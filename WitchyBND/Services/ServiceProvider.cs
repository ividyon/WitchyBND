using System;
using Microsoft.Extensions.DependencyInjection;

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
        var output = new OutputService();
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