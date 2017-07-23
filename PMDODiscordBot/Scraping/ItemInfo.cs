using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace CSharpDewott.Scraping
{
    class ItemInfo
    {
        public bool hasInitialized
        {
            get;
            private set;
        }

        public bool Buyable
        {
            get;
            private set;
        }

        public int BuyPrice
        {
            get;
            private set;
        }

        public bool Sellable
        {
            get;
            private set;
        }

        public int SellPrice
        {
            get;
            private set;
        }

        public string LatestDescription
        {
            get;
            private set;
        }

        public List<string> ObtainableGameList
        {
            get;
            private set;
        }

        private string webpageSource;

        private string parsedSource;

        private string parsedName;

        public ItemInfo(string itemName)
        {
            for (int index = 0; index < itemName.Split(' ').Length; index++)
            {
                string splitItem = itemName.Split(' ')[index];

                this.parsedName += splitItem[0].ToString().ToUpper() + splitItem.Substring(1);

                if (index + 1 != itemName.Split(' ').Length)
                {
                    this.parsedName += "_";
                }
            }

            using (WebClient client = new WebClient())
            {
                this.webpageSource = client.DownloadString($"https://bulbapedia.bulbagarden.net/w/index.php?title={this.parsedName}&action=edit");
            }

            this.parsedSource = this.webpageSource.Substring(Regex.Match(this.webpageSource, @"textarea readonly").Index).Substring(Regex.Match(this.webpageSource.Substring(Regex.Match(this.webpageSource, @"textarea readonly").Index), @"\>").Index + 1).Substring(0, Regex.Match(this.webpageSource.Substring(Regex.Match(this.webpageSource, @"textarea readonly").Index).Substring(Regex.Match(this.webpageSource.Substring(Regex.Match(this.webpageSource, @"textarea readonly").Index), @">").Index + 1), @"\<").Index - 1);
        }

        private void GetInfoFromWikiSource()
        {
            if (Regex.IsMatch(this.parsedSource, @"buyable=yes"))
            {
                this.Buyable = true;

                this.BuyPrice = int.Parse(Regex.Match(this.parsedSource, @"\|buy=.+").Value.Split('=')[1]);
            }

            if (Regex.IsMatch(this.parsedSource, @"sellable=yes"))
            {
                this.Sellable = true;

                this.SellPrice = int.Parse(Regex.Match(this.parsedSource, @"\|sell=.+").Value.Split('=')[1]);
            }

            if (Regex.IsMatch(this.parsedSource, "descmdrb"))
            {
                this.LatestDescription = Regex.Match(this.parsedSource, @"\|descmdrb=.+").Value.Split('=')[1];

                this.ObtainableGameList.Add("Red and Blue Rescue Team");
            }

            if (Regex.IsMatch(this.parsedSource, "descmdtds"))
            {
                this.LatestDescription = Regex.Match(this.parsedSource, @"\|descmdtds=.+").Value.Split('=')[1];

                this.ObtainableGameList.Add("Explorers of Time/Darkness/Sky");
            }

            //TODO Check syntax of GTI and SMD
        }

        private void ParseStringFormatting()
        {
            
        }

        private void ReparseWebpage()
        {
            this.parsedSource = this.webpageSource.Substring(Regex.Match(this.webpageSource, @"textarea readonly").Index).Substring(Regex.Match(this.webpageSource.Substring(Regex.Match(this.webpageSource, @"textarea readonly").Index), @"\>").Index + 1).Substring(0, Regex.Match(this.webpageSource.Substring(Regex.Match(this.webpageSource, @"textarea readonly").Index).Substring(Regex.Match(this.webpageSource.Substring(Regex.Match(this.webpageSource, @"textarea readonly").Index), @">").Index + 1), @"\<").Index - 1);
        }

    }
}
