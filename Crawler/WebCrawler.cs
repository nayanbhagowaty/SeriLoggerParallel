using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace Crawler
{
    public class WebCrawler: IWebCrawler
    {
        public int MaxConcurrency { get; set; } = 10;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
        public string UserAgent { get; set; } = "WebCrawler/1.0";

        private readonly HttpClient _httpClient;
        private readonly SemaphoreSlim _semaphore;
        private readonly ILogger<WebCrawler> _logger;

        public WebCrawler(ILogger<WebCrawler> logger)
        {
            _httpClient = new HttpClient
            {
                Timeout = Timeout
            };
            _httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            _semaphore = new SemaphoreSlim(MaxConcurrency);
            _logger = logger;
            _logger.LogInformation("Started logging.... ");
        }

        //public async Task<ConcurrentDictionary<string, string>> CrawlAsync(IEnumerable<string> urls, Func<string, ILogger> GetLogger, CancellationToken cancellationToken = default)
        //{
        //    var results = new ConcurrentDictionary<string, string>();
        //    var tasks = new List<Task>();

        //    foreach (var url in urls)
        //    {
        //        await _semaphore.WaitAsync(cancellationToken);

        //        tasks.Add(Task.Run(async () =>
        //        {
        //            var domainLogger = GetLogger(new Uri(url).Host.Replace("www.", ""));
        //            try
        //            {
        //                domainLogger.LogInformation("Starting crawl for URL: {Url}", url);
        //                var content = await FetchContentAsync(url, cancellationToken);
        //                results[url] = content;
        //                domainLogger.LogInformation("Successfully fetched content for URL: {Url}", url);
        //                domainLogger.LogInformation(content);
        //            }
        //            catch (Exception ex)
        //            {
        //                domainLogger.LogError(ex, "Error fetching URL: {Url}", url);
        //                results[url] = $"Error: {ex.Message}";
        //            }
        //            finally
        //            {
        //                _semaphore.Release();
        //            }
        //        }, cancellationToken));
        //    }

        //    await Task.WhenAll(tasks);
        //    _logger.LogInformation("Ended logging.... ");
        //    return results;
        //}

        public async Task CrawlAsync(string url, CancellationToken cancellationToken = default)
        {
            var tasks = new List<Task>();
            await _semaphore.WaitAsync(cancellationToken);

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Starting crawl for URL: {Url}", url);
                    var content = await FetchContentAsync(url, cancellationToken);
                    _logger.LogInformation("Successfully fetched content for URL: {Url}", url);
                    _logger.LogInformation($"Title: {InnerText("title",content)}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching URL: {Url}", url);
                }
                finally
                {
                    _semaphore.Release();
                }
            }, cancellationToken));

            await Task.WhenAll(tasks);
            _logger.LogInformation("Ended logging.... ");
        }
        private string InnerText(string tag, string content)
        {
            var document = new HtmlDocument();
            document.LoadHtml(content);
            var node = document.DocumentNode.SelectSingleNode($"//{tag}");
            return node != null ? node.InnerText : "";
        }

        private async Task<string> FetchContentAsync(string url, CancellationToken cancellationToken)
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }

    public interface IWebCrawler
    {
        Task CrawlAsync(string url, CancellationToken cancellationToken = default);
    }
}
