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
using System.Linq;
using ExcelHelperExe;

namespace PaDistBot.Services
{
    public class Scraper
    {
        private  HttpClient _client;
        private int _idx;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly int _threads;
        private bool _userProxies = false;
        private ChromeDriver _driver;
        private string _userName; //= "alan@cameowaterwear.com";
        private string _password; //= "Parker303";
        private string _inputFile;
        private string _outputFile;

        public Scraper(int threads, string userName, string password, string inputFile, string outputFile)
        {
            _threads = threads;
            _userName = userName;
            _password = password;
            _inputFile = inputFile;
            _outputFile = outputFile;
            //_client.DefaultRequestHeaders.Add("cookie","DevicePreferences=DefaultBasketGuid=dc842e6d-8461-4089-83ea-a15cd0e6c79a; __stripe_mid=64b729e8-b40d-43b4-8882-17d64f56e25d4b18ef; __stripe_sid=8e970dbb-f74a-4a9c-9572-7e74d52ac34360281a; WebLogin=Guid=ee588a0e-1586-421c-8a3c-5e23c31a854d&Salt=izG4t4WkQnCe5tGyrgZxj1LlRuBhcozy");
        }

        private void InitClient()
        {
            var cookies = "";
            if (File.Exists("ses.txt"))
                cookies = File.ReadAllText("ses.txt");
            _client = new HttpClient(new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                UseCookies = false
            });
            _client.DefaultRequestHeaders.Add("user-agent","Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.114 Safari/537.36");
            _client.DefaultRequestHeaders.Add("cookie", cookies);
        }

        private void Login()
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
          File.WriteAllText("ses.txt",cookieBiulde.ToString());
        }
        //didn't work out of the box, it needed more work, or we just use selenium for this
        async Task LoginHttpClient()
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
         
            // if (doc.DocumentNode.SelectNodes("//div[@class='PortraitPhotos']//img").Count > 0)
            // {
            //     foreach (var item in doc.DocumentNode.SelectNodes("//div[@class='PortraitPhotos']//img"))
            //     {
            //         urlImgList.Add(item.GetAttributeValue("src", ""));
            //     }
            // }
            // else urlImgList.Add("no image");

            var img = doc.DocumentNode.SelectSingleNode("//div[@class='PortraitPhotos']//img")?.GetAttributeValue("src", "");
            var price = doc.DocumentNode.SelectSingleNode("//item-price-panel//tbody//td[2]")?.InnerText;
            var retailPrice = doc.DocumentNode.SelectSingleNode("//label[contains(text(),'Retail Price')]/following-sibling::div")?.InnerText;
            var upc = doc.DocumentNode.SelectSingleNode("//label[contains(text(),'UPC')]/following-sibling::div")?.InnerText;
            var vendor = doc.DocumentNode.SelectSingleNode("//label[contains(text(),'Vendor Item #')]/following-sibling::div")?.InnerText;
            var pa = doc.DocumentNode.SelectSingleNode("//label[contains(text(),'PA Item #')]/following-sibling::div")?.InnerText;
            var description = doc.DocumentNode.SelectSingleNode("//item-name-panel//item-description")?.InnerText;
            return new Item() { Url = url, Img = img, Price = price, RetailPrice = retailPrice, Upc = upc, Vendor = vendor, Pa = pa, Description = description };
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

        private async Task<List<string>> GetCategoryLinks(string categoryUrl)
        {
            Notifier.Display($"Working on category : {categoryUrl}");
            var doc = await _client.GetHtml(categoryUrl.Replace("&rpp=200","&rpp=1")).ToDoc();
            var itemCountString = doc.DocumentNode.SelectSingleNode("//div[@class='ItemCounts']").InnerText
                .GetStringBetween("of","Total Items").Replace(",","").Trim();
            var itemCount = int.Parse(itemCountString);
            var pages = itemCount / 100;
            if (itemCount % 100 != 0) pages++;
            var pageUrls = new List<string>();
            for (int i = 1; i <= pages; i++) 
                pageUrls.Add(categoryUrl.Replace("&rpp=200", "&rpp=100") + "&pg=" + i);

            var allUrls = await pageUrls.Parallel(_threads, GetUrlItems);
            return allUrls;
        }

        private async Task MakeSureWeAreLoggedIn()
        {
            var doc = await _client.GetHtml("https://pa-dist.com/").ToDoc();
            if (doc.DocumentNode.SelectSingleNode("//menu-item[@class='ProfileMenu IsActive']/menu-title") == null)
            {
                await Task.Run(Login);
                InitClient();
            }
        }
        public async Task MainWork(CancellationToken ct)
        {
            Notifier.Display("Started working");
            InitClient();
            await MakeSureWeAreLoggedIn();
            var inputs = File.ReadAllLines(_inputFile).ToList();
            var allUrls = new List<string>();
            foreach (var input in inputs)
            {
                var urls = await GetCategoryLinks(input);
                allUrls.AddRange(urls);
            }
           // allUrls.Save();
            var items = await allUrls.Parallel(_threads, GetDetails); //this line to start multi tasking
            items.Save(); //this save the result items to json to persist it (not needed , later you just put to excel )
            await items.SaveToExcel(_outputFile);
            //var it = _outputFile.ReadFromExcel<Item>();
            Notifier.Display("Completed working");

        }
    }
}