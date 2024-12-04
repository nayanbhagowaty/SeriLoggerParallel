using Crawler;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;

namespace SeriLoggerFilter
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Configure Serilog for general logging
            ConfigureLogger("logs/general.log");

            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSerilog();
            });

            // General logger for the crawler
            var crawlerLogger = loggerFactory.CreateLogger<WebCrawler>();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(builder=>builder.AddSerilog());
            serviceCollection.AddTransient<IWebCrawler, WebCrawler>();
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var crawler = serviceProvider.GetRequiredService<IWebCrawler>();
            var urls = new List<string>
            {
                "https://example.com",
                "https://google.com",
                "https://microsoft.com",
                "https://nonexistent.com",
            };
            foreach (var url in urls)
            {
                var cancellationTokenSource = new CancellationTokenSource();
                using (crawlerLogger.BeginScope(new Dictionary<string, object>
                {
                    ["Scope"] = $"URL: {url}",
                    ["Domain"] = new Uri(url).Host
                }))
                {
                    await crawler.CrawlAsync(url, cancellationTokenSource.Token);
                }
            }
            Log.CloseAndFlush();
        }
        static string GetLogFilePath(string url)
        {
            var file = new Uri(url).Host.Replace("www.", "");
            return $"logs/{file}.log";
        }
        static void ConfigureLogger(string logFilePath)
        {
            // Configure Serilog for general logging with conditional file sinks
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                // General log file for all logs
                .WriteTo.File("logs/general.log",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "[{Timestamp:HH:mm} {Level}] {Message} {Properties}{NewLine}{Exception}")
                // Conditional logger for specific domains
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(evt =>
                        evt.Properties.ContainsKey("Scope") &&
                        evt.Properties["Scope"].ToString().Contains("google.com"))
                    .WriteTo.File("logs/google.log",
                        rollingInterval: RollingInterval.Day,
                        outputTemplate: "[{Timestamp:HH:mm} {Level}] {Message} {Properties}{NewLine}{Exception}"))
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(evt =>
                        evt.Properties.ContainsKey("Scope") &&
                        evt.Properties["Scope"].ToString().Contains("microsoft.com"))
                    .WriteTo.File("logs/microsoft.log",
                        rollingInterval: RollingInterval.Day,
                        outputTemplate: "[{Timestamp:HH:mm} {Level}] {Message} {Properties}{NewLine}{Exception}"))
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(evt =>
                        evt.Properties.ContainsKey("Scope") &&
                        evt.Properties["Scope"].ToString().Contains("example.com"))
                    .WriteTo.File("logs/google.log",
                        rollingInterval: RollingInterval.Day,
                        outputTemplate: "[{Timestamp:HH:mm} {Level}] {Message} {Properties}{NewLine}{Exception}"))
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(evt =>
                        evt.Properties.ContainsKey("Scope") &&
                        evt.Properties["Scope"].ToString().Contains("nonexistent.com"))
                    .WriteTo.File("logs/microsoft.log",
                        rollingInterval: RollingInterval.Day,
                        outputTemplate: "[{Timestamp:HH:mm} {Level}] {Message} {Properties}{NewLine}{Exception}"))
                // Error logger for failed crawls
                //.WriteTo.Logger(lc => lc
                //    .Filter.ByIncludingOnly(evt => evt.Level == LogEventLevel.Error)
                //    .WriteTo.File("logs/errors.log",
                //        rollingInterval: RollingInterval.Day,
                //        outputTemplate: "[{Timestamp:HH:mm} {Level}] {Message} {Properties}{NewLine}{Exception}"))
                .CreateLogger();
        }
    }
}
