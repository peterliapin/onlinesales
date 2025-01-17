// <copyright file="SmsPlugin.cs" company="WavePoint Co. Ltd.">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OnlineSales.Data;
using OnlineSales.Plugin.Sms.Configuration;
using OnlineSales.Plugin.Sms.Data;
using OnlineSales.Plugin.Sms.Interfaces;
using OnlineSales.Plugin.Sms.Services;
using OnlineSales.Plugin.Sms.Tasks;

namespace OnlineSales.Plugin.Sms;

public class SmsPlugin : IPlugin
{
    public static PluginConfig Configuration { get; private set; } = new PluginConfig();

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var pluginConfig = configuration.Get<PluginConfig>();

        if (pluginConfig != null)
        {
            Configuration = pluginConfig;
        }

        services.AddScoped<PluginDbContextBase, SmsDbContext>();
        services.AddScoped<SmsDbContext, SmsDbContext>();

        services.AddSingleton<ISmsService, SmsService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<ITelegramService, TelegramService>();
        services.AddScoped<IWhatsAppService, WhatsAppService>();

        services.AddScoped<ITask, SyncSmsLogTask>();
    }
}