using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Huppy
{
    public static class Extensions
    {
        public static IServiceCollection AddSingletons<T>(this IServiceCollection serviceCollection)
        {
            var services = typeof(Program).Assembly.GetTypes()
                                                   .Where(x => !x.IsInterface && typeof(T).IsAssignableFrom(x));

            foreach (var service in services)
            {
                serviceCollection.AddSingleton(service);
            }

            return serviceCollection;
        }
    }
}