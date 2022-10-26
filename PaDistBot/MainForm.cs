using MetroFramework.Controls;
using MetroFramework.Forms;
//using Microsoft.Playwright;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using PaDistBot.Extensions;
using PaDistBot.Models;
using PaDistBot.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PaDistBot
{
    public partial class MainForm : MetroForm
    {
        private bool _logToUi = true;
        private bool _logToFile = true;

        private readonly string _path = Application.StartupPath;
        private int _maxConcurrency;
        private Dictionary<string, string> _config;
        private CancellationTokenSource _cancellationTokenSource;
        //private HttpClient _httpClient;


        public MainForm()
        {
            InitializeComponent();
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            ServicePointManager.DefaultConnectionLimit = 65000;
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            inputI.Text = _path + @"\input.txt";
            outputI.Text = _path + @"\output.xlsx";
            Directory.CreateDirectory("data");
            LoadConfig();
            Notifier.OnDisplay += OnDisplay;
            Notifier.OnLog += OnLog;
            Notifier.OnError += OnError;
            Notifier.OnProgress += OnProgress;        
        }

        //private async Task MainWork() 
        //{
        //    Console.WriteLine("work Start");
        //    var cookies = await GetCookies();
        //    var allUrlList = new List<List<string>>();
        //    for (int i = 1; i < 126; i++)
        //    {
        //        Console.WriteLine("page number: " + i);
        //        var urlList = await GetUrl(cookies, "https://pa-dist.com/sr?f=1:13530&pg=" + i + "&rpp=100&sort=8&sid=365907");
        //        allUrlList.Add(urlList);
                
        //    }


        //    foreach (var item in allUrlList)
        //    {
        //        foreach (var url in item)
        //        {
        //            Console.WriteLine(url);
        //            using (StreamWriter writer = new StreamWriter(@"D:\urlCategorie1.txt"))
        //            {
        //                writer.WriteLine(url);
        //            }
        //        }
        //    }


        //    Console.WriteLine("End Work");
        //}

        //public async Task<string> GetCookies() 
        //{
            
        //    _userName = userNameLabel.Text;
        //    _password = passwordLabel.Text;

        //    var chromeDriverService = ChromeDriverService.CreateDefaultService();
        //    chromeDriverService.HideCommandPromptWindow = true;
        //    _driver = new ChromeDriver(chromeDriverService);
        //    _driver.Navigate().GoToUrl("https://pa-dist.com/SignIn");
        //    var inputEmail = _driver.FindElement(By.Id("step1u"));
        //    var inputPassword = _driver.FindElement(By.Id("step1p"));

        //    inputEmail.SendKeys(_userName);
        //    inputPassword.SendKeys(_password);
        //    //Thread.Sleep(2000);
        //    var submitButton = _driver.FindElement(By.XPath("//button[@type='submit']"));
        //    submitButton.Click();

        //    //Thread.Sleep(5000);

        //    var cookieBiulde = new StringBuilder();
        //    foreach (var cookie in _driver.Manage().Cookies.AllCookies)
        //    {
        //        cookieBiulde.Append(cookie.Name + "=" + cookie.Value + ";");
        //    }
        //    _driver.Quit();
        //    cookieBiulde.Length--;
        //    var cookies = cookieBiulde.ToString();
        //    //Console.WriteLine("-->  " +cookies);
        //    return cookies;
        //}

        //public async Task<List<string>> GetUrl(string cookies, string urlCategorie)
        //{
        //    var _urlList = new List<string>();
        //    _httpClient = new HttpClient(new HttpClientHandler
        //    {
        //        UseCookies = false,
        //        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        //    })
        //    {
        //        DefaultRequestHeaders =
        //        {
        //            { "user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.114 Safari/537.36" },
        //            { "Cookie", cookies }
        //        }
        //    };

        //    //var doc = await _httpClient.GetHtml("https://pa-dist.com/").ToDoc();
        //    //var html = doc.DocumentNode.SelectNodes("//div[@class='Categories']/a").Count;
        //    //Console.WriteLine("number of categories: " +html);

        //    var doc = await _httpClient.GetHtml(urlCategorie).ToDoc();
        //    var htmlNodes = doc.DocumentNode.SelectNodes("//a[@class='ItemImgLink ']");
        //    foreach (var item in htmlNodes)
        //    {
        //        var href = item.GetAttributeValue("href", "");
        //        var itemId = Utility.BetweenStrings(href, "/Item/", "?");
        //        //Console.WriteLine(itemId);
        //        _urlList.Add("https://pa-dist.com/Item/" + itemId);
        //    }

        //    //Console.WriteLine("number of categories: " + htmlNodes.Count);

        //    return _urlList;
        //}

        private void OnProgress(object sender, (int x, int total) e)
        {
            SetProgress(e.x * 100 / e.total);
        }

        private void OnError(object sender, string e)
        {
            ErrorLog(e);
        }

        private void OnLog(object sender, string e)
        {
            NormalLog(e);
        }

        private void OnDisplay(object sender, string e)
        {
            Display(e);
        }

        void InitControls(Control parent)
        {
            try
            {
                foreach (Control x in parent.Controls)
                {
                    try
                    {
                        if (x.Name.EndsWith("I"))
                        {
                            switch (x)
                            {
                                case MetroCheckBox _:
                                case CheckBox _:
                                    ((CheckBox)x).Checked = bool.Parse(_config[((CheckBox)x).Name]);
                                    break;
                                case RadioButton radioButton:
                                    radioButton.Checked = bool.Parse(_config[radioButton.Name]);
                                    break;
                                case TextBox _:
                                case RichTextBox _:
                                case MetroTextBox _:
                                    x.Text = _config[x.Name];
                                    break;
                                case NumericUpDown numericUpDown:
                                    numericUpDown.Value = int.Parse(_config[numericUpDown.Name]);
                                    break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }

                    InitControls(x);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public void SaveControls(Control parent)
        {
            try
            {
                foreach (Control x in parent.Controls)
                {
                    #region Add key value to disctionarry

                    if (x.Name.EndsWith("I"))
                    {
                        switch (x)
                        {
                            case MetroCheckBox _:
                            case RadioButton _:
                            case CheckBox _:
                                _config.Add(x.Name, ((CheckBox)x).Checked + "");
                                break;
                            case TextBox _:
                            case RichTextBox _:
                            case MetroTextBox _:
                                _config.Add(x.Name, x.Text);
                                break;
                            case NumericUpDown _:
                                _config.Add(x.Name, ((NumericUpDown)x).Value + "");
                                break;
                            default:
                                Console.WriteLine(@"could not find a type for " + x.Name);
                                break;
                        }
                    }

                    #endregion

                    SaveControls(x);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void SaveConfig()
        {
            _config = new Dictionary<string, string>();
            SaveControls(this);
            try
            {
                File.WriteAllText("config.txt", JsonConvert.SerializeObject(_config, Formatting.Indented));
            }
            catch (Exception e)
            {
                ErrorLog(e.ToString());
            }
        }

        private void LoadConfig()
        {
            try
            {
                _config = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText("config.txt"));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return;
            }

            InitControls(this);
        }

        static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.ToString(), @"Unhandled Thread Exception");
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show((e.ExceptionObject as Exception)?.ToString(), @"Unhandled UI Exception");
        }

        #region UIFunctions

        public delegate void WriteToLogD(string s, Color c);

        public void WriteToLog(string s, Color c)
        {
            try
            {
                if (InvokeRequired)
                {
                    Invoke(new WriteToLogD(WriteToLog), s, c);
                    return;
                }

                if (_logToUi)
                {
                    if (DebugT.Lines.Length > 5000)
                    {
                        DebugT.Text = "";
                    }

                    DebugT.SelectionStart = DebugT.Text.Length;
                    DebugT.SelectionColor = c;
                    DebugT.AppendText(DateTime.Now.ToString(Utility.SimpleDateFormat) + " : " + s + Environment.NewLine);
                }

                Console.WriteLine(DateTime.Now.ToString(Utility.SimpleDateFormat) + @" : " + s);
                if (_logToFile)
                {
                    File.AppendAllText(_path + "/data/log.txt", DateTime.Now.ToString(Utility.SimpleDateFormat) + @" : " + s + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public void NormalLog(string s)
        {
            WriteToLog(s, Color.Black);
        }

        public void ErrorLog(string s)
        {
            WriteToLog(s, Color.Red);
        }

        public void SuccessLog(string s)
        {
            WriteToLog(s, Color.Green);
        }

        public void CommandLog(string s)
        {
            WriteToLog(s, Color.Blue);
        }

        public delegate void SetProgressD(int x);

        public void SetProgress(int x)
        {
            if (InvokeRequired)
            {
                Invoke(new SetProgressD(SetProgress), x);
                return;
            }

            if ((x <= 100))
            {
                ProgressB.Value = x;
            }
        }

        public delegate void DisplayD(string s);

        public void Display(string s)
        {
            if (InvokeRequired)
            {
                Invoke(new DisplayD(Display), s);
                return;
            }

            displayT.Text = s;
        }

        #endregion

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveConfig();
        }

        private void loadInputB_Click_1(object sender, EventArgs e)
        {
            OpenFileDialog o = new OpenFileDialog { Filter = @"TXT|*.txt", InitialDirectory = _path };
            if (o.ShowDialog() == DialogResult.OK)
            {
                inputI.Text = o.FileName;
            }
        }

        private void openInputB_Click_1(object sender, EventArgs e)
        {
            try
            {
                Process.Start(inputI.Text);
            }
            catch (Exception ex)
            {
                ErrorLog(ex.ToString());
            }
        }

        private void openOutputB_Click_1(object sender, EventArgs e)
        {
            try
            {
                Process.Start(outputI.Text);
            }
            catch (Exception ex)
            {
                ErrorLog(ex.ToString());
            }
        }

        private void loadOutputB_Click_1(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog
            {
                Filter = @"xlsx file|*.xlsx",
                Title = @"Select the output location"
            };
            saveFileDialog1.ShowDialog();
            if (saveFileDialog1.FileName != "")
            {
                outputI.Text = saveFileDialog1.FileName;
            }
        }

        private async void startB_Click_1(object sender, EventArgs e)
        {
            SaveConfig();
            _logToUi = logToUII.Checked;
            _logToFile = logToFileI.Checked;
            _maxConcurrency = (int)threadsI.Value;
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                var scraper = new Scraper((int)threadsI.Value,userNameLabelI.Text,passwordLabelI.Text,inputI.Text,outputI.Text);
                await scraper.MainWork(_cancellationTokenSource.Token);
            }
            catch (TaskCanceledException)
            {
                CommandLog("Canceled by the user");
            }
            catch (KnownException ex)
            {
                ErrorLog(ex.Message);
            }
            catch (Exception ex)
            {
                ErrorLog(ex.ToString());
            }
            //await MainWork();
        }

        private void stopB_Click(object sender, EventArgs e)
        {
            //_cancellationTokenSource?.Cancel();
        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        public void metroTextBox1_Click(object sender, EventArgs e)
        {

        }

        public void passwordLabel_Click(object sender, EventArgs e)
        {

        }
    }
}