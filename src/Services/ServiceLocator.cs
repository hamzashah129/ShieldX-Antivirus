using System;
using Microsoft.Extensions.DependencyInjection;
using ShieldX.Services;

namespace ShieldX.Services
{
    public static class ServiceLocator
    {
        private static IServiceProvider _serviceProvider;

        public static void Initialize()
        {
            var services = new ServiceCollection();

            // Register services
            services.AddSingleton<DatabaseService>();
            services.AddSingleton<ScanEngine>();
            services.AddSingleton<SecurityScoreEngine>();
            services.AddSingleton<ModuleManager>();

            _serviceProvider = services.BuildServiceProvider();
        }

        public static T GetService<T>() => _serviceProvider.GetService<T>();
    }
}