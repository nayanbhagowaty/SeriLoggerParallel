using Crawler;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace serilogger
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Configure Serilog for general logging
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug().Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("logs/general.log", rollingInterval: RollingInterval.Day,outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
                .CreateLogger();

            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSerilog();
            });

            // General logger for the crawler
            var crawlerLogger = loggerFactory.CreateLogger<WebCrawler>();

            // Dictionary to manage domain-specific loggers
            var domainLoggers = new Dictionary<string, ILogger>();

            ILogger GetDomainLogger(string domain)
            {
                if (!domainLoggers.TryGetValue(domain, out var logger))
                {
                    // Create a new logger for the domain
                    var domainLogger = new LoggerConfiguration()
                        .MinimumLevel.Debug().Enrich.FromLogContext()
                        .WriteTo.File($"logs/{domain}.log", rollingInterval: RollingInterval.Day,outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
                        .CreateLogger();

                    logger = new LoggerFactory()
                        .AddSerilog(domainLogger)
                        .CreateLogger($"DomainLogger-{domain}");

                    domainLoggers[domain] = logger;
                }

                return logger;
            }

            // Instantiate the crawler
            var crawler = new WebCrawler(maxConcurrency: 10, crawlerLogger);

            // URLs to crawl
            var urls = new List<string>
            {
                "https://example.com",
                "https://google.com",
                "https://microsoft.com",
                "https://nonexistent.com",
            };

            // Start crawling
            var cancellationTokenSource = new CancellationTokenSource();

            //var results = await crawler.CrawlAsync(urls, GetDomainLogger, cancellationTokenSource.Token);

            foreach (var url in urls)
            {
                using(crawlerLogger.BeginScope($"URL: {url}"))
                await crawler.CrawlAsync(url, cancellationTokenSource.Token);
            }

            // Ensure logs are flushed
            Log.CloseAndFlush();
        }
    }
}
