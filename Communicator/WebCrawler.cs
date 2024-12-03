namespace Crawler
{
    using Microsoft.Extensions.Logging;
    using Serilog;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using ILogger = Microsoft.Extensions.Logging.ILogger;

    public class WebCrawler
    {
        // Configuration properties
        public int MaxConcurrency { get; set; } = 5;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
        public string UserAgent { get; set; } = "WebCrawler/1.0";
        //private readonly ILogger<WebCrawler> _logger;

        // Internal fields
        private readonly HttpClient _httpClient;
        private readonly SemaphoreSlim _semaphore;
        private readonly ILoggerFactory _loggerFactory;
        public WebCrawler(int maxConcurrency, ILoggerFactory loggerFactory)
        {
            MaxConcurrency = maxConcurrency;
            _httpClient = new HttpClient
            {
                Timeout = Timeout
            };
            _httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            _semaphore = new SemaphoreSlim(MaxConcurrency);
            _loggerFactory = loggerFactory;
        }

        public async Task<ConcurrentDictionary<string, string>> CrawlAsync(IEnumerable<string> urls, CancellationToken cancellationToken = default)
        {
            var results = new ConcurrentDictionary<string, string>();
            var tasks = new List<Task>();

            foreach (var url in urls)
            {
                await _semaphore.WaitAsync(cancellationToken);

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var domain = new Uri(url).Host.Replace("www.", "");
                        var logger = CreateDomainLogger(domain);

                        logger.LogInformation("Starting crawl for URL: {Url}", url);

                        var content = await FetchContentAsync(url, cancellationToken);
                        results[url] = content;

                        logger.LogInformation("Successfully fetched content for URL: {Url}", url);
                        logger.LogInformation(content);
                    }
                    catch (Exception ex)
                    {
                        var domain = new Uri(url).Host.Replace("www.", "");
                        var logger = CreateDomainLogger(domain);

                        logger.LogError(ex, "Error fetching URL: {Url}", url);
                        results[url] = $"Error: {ex.Message}";
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }, cancellationToken));
            }

            await Task.WhenAll(tasks);
            return results;
        }

        private async Task<string> FetchContentAsync(string url, CancellationToken cancellationToken)
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        private ILogger CreateDomainLogger(string domain)
        {
            // Create a dynamic logger for each domain
            var logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File($"logs/{domain}.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            return new LoggerFactory().AddSerilog(logger).CreateLogger<WebCrawler>();
        }
    }
}
