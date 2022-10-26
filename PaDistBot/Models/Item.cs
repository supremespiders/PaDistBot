using System.Collections.Generic;
using System.Diagnostics;

namespace PaDistBot.Models
{
    public class Item
    {
        public string Url { get; set; }
        public  List<string> Img { get; set; }
        public string Upc { get; set; }
        public string Price { get; set; }
        public string RetailPrice { get; set; }
        public string Vendor { get; set; }
        public string Pa { get; set; }
        public string Description { get; set; }
    }
}