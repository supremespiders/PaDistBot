using PaDistBot.Extensions;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PaDistBot.Models;

namespace PaDistBot.Services
{
    public class Scraper
    {
        private readonly HttpClient _client;
        private int _idx;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly int _threads;
        private bool _userProxies = false;

        public Scraper(int threads)
        {
            _threads = threads;
            _client = new HttpClient(new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                UseCookies = false
            });
            _client.DefaultRequestHeaders.Add("user-agent","Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.114 Safari/537.36");
            _client.DefaultRequestHeaders.Add("cookie","DevicePreferences=DefaultBasketGuid=dc842e6d-8461-4089-83ea-a15cd0e6c79a; __stripe_mid=64b729e8-b40d-43b4-8882-17d64f56e25d4b18ef; __stripe_sid=8e970dbb-f74a-4a9c-9572-7e74d52ac34360281a; WebLogin=Guid=ee588a0e-1586-421c-8a3c-5e23c31a854d&Salt=izG4t4WkQnCe5tGyrgZxj1LlRuBhcozy");
        }

        async Task Login()
        {
            var doc=await _client.PostFormData("https://pa-dist.com/SignIn/Process/Post", new Dictionary<string, string>()
            {
                { "step", "1" },
                { "RedirectUrl", "/" },
                { "step1u", "alan@cameowaterwear.com" },
                { "step1p", "Parker303" },
            }).ToDoc();

           // doc = await _client.GetHtml("https://pa-dist.com/Profile/Edit").ToDoc();
            // doc.Save("html.html");
            // Process.Start("html.html");
        }

        public async Task<Item> GetDetails(string url)
        {
            var doc = await _client.GetHtml(url).ToDoc();
            // doc.Save("x.html");
            // Process.Start("x.html");
            var img = doc.DocumentNode.SelectSingleNode("//img[@src='https://cdn.pa-dist.com/item/v3s/328276-1']").GetAttributeValue("src","");
            return new Item() { Url = url, Img = img };
        }

        public async Task MainWork(CancellationToken ct)
        {
            Notifier.Display("Started working");
            var urls = new List<string>();
            for (int i = 0; i < 1000; i++)
            {
                urls.Add("https://pa-dist.com/Item/328276");
            }

            var items = await urls.Parallel(30, GetDetails);
            //var urls = new List<string> { "a", "b" };
            //var results = await urls.Parallel(_threads, Work);

          //  await Login();
            Notifier.Display("Completed working");
        }
    }
}