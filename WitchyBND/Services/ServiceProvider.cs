﻿using System;
using Microsoft.Extensions.DependencyInjection;

namespace WitchyBND.Services;

public static class ServiceProvider
{
    private static IServiceProvider _provider;

    public static T GetService<T>() where T : notnull
    {
        return _provider.GetRequiredService<T>();
    }

    private static IServiceProvider CreateProvider()
    {
        var error = new ErrorService();
        var game = new GameService(error);
        var update = new UpdateService(error);

        var collection = new ServiceCollection()
            .AddSingleton<IErrorService>(error)
            .AddSingleton<IGameService>(game)
            .AddSingleton<IUpdateService>(update);

        return collection.BuildServiceProvider();
    }

    public static void ReplaceProvider(IServiceProvider provider)
    {
        _provider = provider;
    }

    static ServiceProvider()
    {
        _provider = CreateProvider();
    }
}