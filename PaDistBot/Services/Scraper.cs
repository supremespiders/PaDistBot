using PaDistBot.Extensions;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PaDistBot.Services
{
    public class Scraper
    {
        private readonly HttpClient _client;
        private List<HttpClient> _clients;
        private int _idx;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly int _threads;
        private bool _userProxies = false;

        public Scraper(int threads)
        {
            _threads = threads;
            _client = new HttpClient(new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            });
            if (File.Exists("proxies.txt"))
            {
                InitClientProxies();
                _clients.Add(_client); //add your ip to proxies
            }
        }

        private void InitClientProxies()
        {
            _clients = new List<HttpClient>();
            var proxies = File.ReadAllLines("proxies.txt");
            foreach (var p in proxies)
            {
                var pp = p.Split(':');
                var proxy = new WebProxy($"{pp[0]}:{pp[1]}", true)
                {
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(pp[2], pp[3]),
                };
                _clients.Add(new HttpClient(new HttpClientHandler()
                {
                    Proxy = proxy,
                    UseCookies = false,
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                }));
            }
        }

        async Task<HttpClient> GetNextClient()
        {
            if (!_userProxies) return _client;
            await _semaphore.WaitAsync();
            var client = _clients[_idx];
            _idx++;
            if (_idx == _clients.Count)
                _idx = 0;
            _semaphore.Release();
            return client;
        }

        async Task<string> Work(string url)
        {
            var client = await GetNextClient();
            var doc = await client.GetHtml(url).ToDoc();
            return url;
        }

        private

        public async Task MainWork(CancellationToken ct)
        {
            Notifier.Display("Started working");
            //var urls = new List<string> { "a", "b" };
            //var results = await urls.Parallel(_threads, Work);
            var request = new HttpRequestMessage(HttpMethod.Post, "https://pa-dist.com/SignIn/Process/Post");
            request.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.114 Safari/537.36");
            var response = await _client.SendAsync(request);
            var bytes = await response.Content.ReadAsByteArrayAsync();
            var html = Encoding.UTF8.GetString(bytes);
            File.WriteAllText("html.html",html);
            Process.Start("html.html");
            Notifier.Display("Completed working");
        }
    }
}