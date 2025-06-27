using Microsoft.Extensions.DependencyInjection;
using Wadapter.src;
using Wadapter.src.Converter;

namespace Wadapter.src.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddWadapter(this IServiceCollection services)
        {
            services.AddSingleton<IConverter, WhatsappConverter>();
            services.AddSingleton<IWhatsappAdapter, WhatsappAdapter>();

            return services;
        }
    }
}
