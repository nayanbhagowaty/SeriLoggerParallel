using Crawler;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Collections.Concurrent;

namespace SeriLogger
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            // Create a dictionary to store loggers for each domain
            var domainLoggers = new ConcurrentDictionary<string, ILogger<WebCrawler>>();

            // Configure the default logger
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("logs/general.log",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "[{Timestamp:HH:mm} {Level}] {Message} {Properties}{NewLine}{Exception}")
                .CreateLogger();

            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSerilog();
            });
            serviceCollection.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders(); // Remove default providers
                loggingBuilder.AddSerilog();
            });
            serviceCollection.AddTransient<IWebCrawler, WebCrawler>();
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var _logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            _logger.LogInformation("Program started...");
            // Function to get or create a domain-specific logger
            ILogger<WebCrawler> GetDomainLogger(string url)
            {
                var domain = new Uri(url).Host;
                return domainLoggers.GetOrAdd(domain, (domainKey) =>
                {
                    // Create a new LoggerFactory for this domain
                    var domainLoggerFactory = LoggerFactory.Create(builder =>
                    {
                        var domainLogger = new LoggerConfiguration()
                            .MinimumLevel.Debug()
                            .Enrich.FromLogContext()
                            .WriteTo.File($"logs/{domainKey}.log",
                                rollingInterval: RollingInterval.Day,
                                outputTemplate: "[{Timestamp:HH:mm} {Level}] {Message} {Properties}{NewLine}{Exception}")
                            .CreateLogger();

                        builder.AddSerilog(domainLogger, dispose: true);
                    });

                    // Return a typed ILogger instance for this domain
                    return domainLoggerFactory.CreateLogger<WebCrawler>();
                });
            }

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
                var domainLogger = GetDomainLogger(url);

                // Create a custom crawler instance for each domain
                var crawler = new WebCrawler(domainLogger);
                try
                {
                    using (domainLogger.BeginScope($"URL: {url}"))
                    {
                        await crawler.CrawlAsync(url, cancellationTokenSource.Token);
                    }
                }
                catch (Exception ex)
                {
                    // Log errors to both domain-specific log and general error log
                    domainLogger.LogError(ex, "Error crawling {Url}", url);
                    Log.Error(ex, "Error crawling {Url}", url);
                    _logger.LogError($"Error crawling {url}");
                }
            }
            // Cleanup
            foreach (var logger in domainLoggers.Values)
            {
                (logger as IDisposable)?.Dispose();
            }
            _logger.LogInformation("Program ended...");
            Log.CloseAndFlush();
        }

    }
}
