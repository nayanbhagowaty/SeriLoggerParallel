using Crawler;
using Microsoft.Extensions.Logging;
using Serilog;

namespace serilogger
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                //builder.AddConsole(); // Keep console logging for quick feedback
            });

            var crawler = new WebCrawler(maxConcurrency: 10, loggerFactory);

            var urls = new List<string>
        {
            "https://example.com",
            "https://google.com",
            "https://microsoft.com",
            "https://nonexistent.com",
        };

            var cancellationTokenSource = new CancellationTokenSource();
            var results = await crawler.CrawlAsync(urls, cancellationTokenSource.Token);

            //foreach (var result in results)
            //{
            //    Console.WriteLine($"URL: {result.Key}");
            //    Console.WriteLine($"Content/Message: {result.Value}");
            //    Console.WriteLine();
            //}
        }
    }
}
