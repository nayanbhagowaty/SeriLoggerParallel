using Crawler;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Log4Net.AspNetCore;
namespace Log4NetLogger
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders(); // Remove default providers
                loggingBuilder.AddLog4Net("log4net.config"); // Add log4net provider
            });
            serviceCollection.AddTransient<IWebCrawler, WebCrawler>();
            var serviceProvider = serviceCollection.BuildServiceProvider();
            // Configure log4net
            log4net.Config.BasicConfigurator.Configure();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            //var crawlerService = new CrawlerService(maxConcurrency: 10);
            var urls = new List<string>
            {
                "https://example.com",
                "https://google.com",
                "https://microsoft.com",
                "https://nonexistent.com",
            };

            //await crawlerService.ProcessUrlsAsync(urls);
            var crawler = serviceProvider.GetRequiredService<IWebCrawler>();
            foreach (var url in urls)
            {
                try
                {
                    var cancellationTokenSource = new CancellationTokenSource();
                    await crawler.CrawlAsync(url, cancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    logger.LogError($"Failed to process {url}", ex);
                }
            }
        }
    }
}