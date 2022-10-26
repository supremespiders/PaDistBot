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
using MetroFramework.Controls;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using System.Security.Policy;
using Newtonsoft.Json;
using System;

namespace PaDistBot.Services
{
    public class Scraper
    {
        private readonly HttpClient _client;
        private int _idx;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly int _threads;
        private bool _userProxies = false;
        private ChromeDriver _driver;
        private string _userName = "alan@cameowaterwear.com";
        private string _password = "Parker303";

        public Scraper(int threads)
        {
            
            var chromeDriverService = ChromeDriverService.CreateDefaultService();
            var chromeOptions = new ChromeOptions();
            chromeDriverService.HideCommandPromptWindow = true;
            chromeOptions.AddArgument("headless");
            _driver = new ChromeDriver(chromeDriverService, chromeOptions);
            _driver.Navigate().GoToUrl("https://pa-dist.com/SignIn");
            var inputEmail = _driver.FindElement(By.Id("step1u"));
            var inputPassword = _driver.FindElement(By.Id("step1p"));
            inputEmail.SendKeys(_userName);
            inputPassword.SendKeys(_password);
            var submitButton = _driver.FindElement(By.XPath("//button[@type='submit']"));
            submitButton.Click();
            var cookieBiulde = new StringBuilder();
            foreach (var cookie in _driver.Manage().Cookies.AllCookies)
            {
                cookieBiulde.Append(cookie.Name + "=" + cookie.Value + ";");
            }
            _driver.Quit();
            cookieBiulde.Length--;
            var cookies = cookieBiulde.ToString();
            Console.WriteLine("-->  " + cookies);

            _threads = threads;
            _client = new HttpClient(new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                UseCookies = false
            });
            _client.DefaultRequestHeaders.Add("user-agent","Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.114 Safari/537.36");
            _client.DefaultRequestHeaders.Add("cookie", cookies);
            //_client.DefaultRequestHeaders.Add("cookie","DevicePreferences=DefaultBasketGuid=dc842e6d-8461-4089-83ea-a15cd0e6c79a; __stripe_mid=64b729e8-b40d-43b4-8882-17d64f56e25d4b18ef; __stripe_sid=8e970dbb-f74a-4a9c-9572-7e74d52ac34360281a; WebLogin=Guid=ee588a0e-1586-421c-8a3c-5e23c31a854d&Salt=izG4t4WkQnCe5tGyrgZxj1LlRuBhcozy");
        }

        //didn't work out of the box, it needed more work, or we just use selenium for this
        async Task Login()
        {
            
            var doc =await _client.PostFormData("https://pa-dist.com/SignIn/Process/Post", new Dictionary<string, string>()
            {
                { "step", "1" },
                { "RedirectUrl", "/" },
                { "step1u", "alan@cameowaterwear.com" },
                { "step1p", "Parker303" },
            }).ToDoc();

            //doc = await _client.GetHtml("https://pa-dist.com/Profile/Edit").ToDoc();
            doc.Save("login.html");
            Process.Start("login.html");
        }

        /// <summary>
        /// This is the main function to put the xpaths and pull details of item then return the item
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<Item> GetDetails(string url)
        {
            var doc = await _client.GetHtml(url).ToDoc();
            var urlImgList = new List<string>();
            //var img = doc.DocumentNode.SelectSingleNode("//img[@src='https://cdn.pa-dist.com/item/v3s/328276-1']")?.GetAttributeValue("src","");
         
            if (doc.DocumentNode.SelectNodes("//div[@class='PortraitPhotos']//img").Count > 0)
            {
                foreach (var item in doc.DocumentNode.SelectNodes("//div[@class='PortraitPhotos']//img"))
                {
                    urlImgList.Add(item.GetAttributeValue("src", ""));
                }
            }
            else urlImgList.Add("no image");

            var price = doc.DocumentNode.SelectSingleNode("//item-price-panel//tbody//td[2]")?.InnerText;
            var retailPrice = doc.DocumentNode.SelectSingleNode("//label[contains(text(),'Retail Price')]/following-sibling::div").InnerText;
            var upc = doc.DocumentNode.SelectSingleNode("//label[contains(text(),'UPC')]/following-sibling::div").InnerText;
            var vendor = doc.DocumentNode.SelectSingleNode("//label[contains(text(),'Vendor Item #')]/following-sibling::div").InnerText;
            var pa = doc.DocumentNode.SelectSingleNode("//label[contains(text(),'PA Item #')]/following-sibling::div").InnerText;
            var description = doc.DocumentNode.SelectSingleNode("//item-name-panel//item-description").InnerText;
            return new Item() { Url = url, Img = urlImgList, Price = price, RetailPrice = retailPrice, Upc = upc, Vendor = vendor, Pa = pa, Description = description };
        }

        public async Task<List<string>> GetUrlItems(string urlCategorie)
        {
            var urlItemsList = new List<string>();
            var doc = await _client.GetHtml(urlCategorie).ToDoc();
            var htmlNodes = doc.DocumentNode?.SelectNodes("//a[@class='ItemImgLink ']");
            foreach (var item in htmlNodes)
            {
                var href = item.GetAttributeValue("href", "");
                var itemId = Utility.BetweenStrings(href, "/Item/", "?");
                //Console.WriteLine(itemId);
                urlItemsList.Add("https://pa-dist.com/Item/" + itemId);
            }
            return urlItemsList;
        }

        public async Task MainWork(CancellationToken ct)
        {
            Notifier.Display("Started working");
            var allUrlList = new List<string>();
           
            for (int i = 1; i < 6; i++)
            {
                Console.WriteLine("Page number " + i);
                try
                {
                    
                    var urls = await GetUrlItems("https://pa-dist.com/sr?f=1:13530&pg=" + i + "&rpp=200&sort=8&sid=365907");
                    allUrlList.AddRange(urls);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception: " +ex);
                    //allUrlList.Add("Exception");
                    allUrlList.Save();
                }

            }
            //foreach (var item in urls)
            //{

            //}
            //var urls = new List<string>();
            //for (int i = 0; i < 100; i++)
            //{
            //    urls.Add("https://pa-dist.com/Item/328276");
            //} 
            var items = await allUrlList.Parallel(30, GetDetails); //this line to start multi tasking
            items.Save(); //this save the result items to json to persist it (not needed , later you just put to excel )
            Notifier.Display("Completed working");

        }
    }
}