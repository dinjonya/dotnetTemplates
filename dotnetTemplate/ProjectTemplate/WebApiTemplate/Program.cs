using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AspectCore.Extensions.DependencyInjection;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Odin.Plugs.OdinString;
using Odin.Plugs.V5.OdinWebHost;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using Unicorn.AspNetCore.Middleware.RealIp;

namespace WebApiTemplate
{
    public class Program
    {
        public static IEnumerable<ApiCommentConfig> ApiComments { get; set; }
        public static void Main(string[] args)
        {
            #region Log设置
            Log.Logger = new LoggerConfiguration()
                // 最小的日志输出级别
                .MinimumLevel.Information()
                //.MinimumLevel.Information ()
                // 日志调用类命名空间如果以 System 开头，覆盖日志输出最小级别为 Information
                .MinimumLevel.Override("System", LogEventLevel.Information)
                // 日志调用类命名空间如果以 Microsoft 开头，覆盖日志输出最小级别为 Information
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .WriteTo.Logger(fileLogger =>
                   fileLogger.Filter
                   .ByIncludingOnly(p => p.Level.Equals(LogEventLevel.Debug))
                   .WriteTo.File(
                       $"logs/{DateTime.Now.ToString("yyyyMMdd")}/log-{DateTime.Now.ToString("yyyyMMdd")}-Debug.txt",
                       fileSizeLimitBytes: 1000000,
                       rollOnFileSizeLimit: true,
                       shared: true,
                       flushToDiskInterval: TimeSpan.FromSeconds(1)
                   )
                   .WriteTo.Console(
                       outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}",
                       theme: SystemConsoleTheme.Colored
                   )
                )
                .WriteTo.Logger(fileLogger =>
                   fileLogger.Filter
                   .ByIncludingOnly(p => p.Level.Equals(LogEventLevel.Warning))
                   .WriteTo.File(
                       $"logs/{DateTime.Now.ToString("yyyyMMdd")}/log-{DateTime.Now.ToString("yyyyMMdd")}-Waring.txt",
                       fileSizeLimitBytes: 1000000,
                       rollOnFileSizeLimit: true,
                       shared: true,
                       flushToDiskInterval: TimeSpan.FromSeconds(1)
                   )
                   .WriteTo.Console(
                       outputTemplate:
                       "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}",
                       theme: SystemConsoleTheme.Colored
                   )
                )
                .WriteTo.Logger(fileLogger =>
                   fileLogger.Filter
                   .ByIncludingOnly(p => p.Level.Equals(LogEventLevel.Information))
                   .WriteTo.File(
                       $"logs/{DateTime.Now.ToString("yyyyMMdd")}/log-{DateTime.Now.ToString("yyyyMMdd")}-Info.txt",
                       fileSizeLimitBytes: 1000000,
                       rollOnFileSizeLimit: true,
                       shared: true,
                       flushToDiskInterval: TimeSpan.FromSeconds(1)
                   )
                   .WriteTo.Console(
                       outputTemplate:
                       "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}",
                       theme: SystemConsoleTheme.Colored
                   )
                )
                .WriteTo.Logger(fileLogger =>
                   fileLogger.Filter
                   .ByIncludingOnly(p => p.Level.Equals(LogEventLevel.Error))
                   .WriteTo.File(
                       $"logs/{DateTime.Now.ToString("yyyyMMdd")}/log-{DateTime.Now.ToString("yyyyMMdd")}-Error.txt",
                       fileSizeLimitBytes: 1000000,
                       rollOnFileSizeLimit: true,
                       shared: true,
                       flushToDiskInterval: TimeSpan.FromSeconds(1)
                   )
                   .WriteTo.Console(
                       outputTemplate:
                       "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}",
                       theme: SystemConsoleTheme.Colored
                   )
                )
                .CreateLogger();
            #endregion

            try
            {
                var odinWebHostManager = OdinWebHostManager.Load();
                do
                {
                    odinWebHostManager.Start<Startup>(CreateHostBuilder(args));
                } while (odinWebHostManager.Restarting);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("服务器启动失败");
                System.Console.WriteLine(JsonConvert.SerializeObject(ex).ToJsonFormatString());
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var builderRoot = new ConfigurationBuilder()
                .AddEnvironmentVariables(prefix: "ASPNETCORE_")
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddEnvironmentVariables();
            if (File.Exists("serverConfig/cnf.json"))
            {
                builderRoot = builderRoot.Add(new JsonConfigurationSource { Path = "serverConfig/cnf.json", Optional = false, ReloadOnChange = true });
            }
            var builder = builderRoot.Build();
            var iHostBuilder = Host.CreateDefaultBuilder(args);
            if (builder.GetValue<bool>("ProjectConfigOptions:FrameworkConfig:Autofac:Enable"))
            {
                iHostBuilder = iHostBuilder.UseServiceProviderFactory(new AutofacServiceProviderFactory());
            }
            if (builder.GetValue<bool>("ProjectConfigOptions:FrameworkConfig:AspectCore:Enable"))
            {
                iHostBuilder = iHostBuilder.UseServiceProviderFactory(new DynamicProxyServiceProviderFactory());
            }

            return iHostBuilder.ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseConfiguration(builder)
                    .UseKestrel(
                        (context, options) =>
                        {
                            options.AllowSynchronousIO = true;
                            //设置应用服务器Kestrel请求体最大为200MB
                            options.Limits.MaxRequestBodySize = 209715200;
                        }
                    )
                    .UseIISIntegration()
                    .UseUrls(builder.GetValue<string>("ProjectConfigOptions:Url").Split(','))
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseRealIp("X-Forwarded-For")
                    .UseStartup<Startup>()
                    .UseSerilog();
                });
        }
    }
}
