using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CSharpDewott.Extensions
{
    public static class HttpHelper
    {
        public static bool UrlExists(string url)
        {
            bool result = false;

            try
            {
                if (!Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
                {
                    return false;
                }

                WebRequest webRequest = WebRequest.Create(url);
                webRequest.Timeout = 1200; // miliseconds
                webRequest.Method = "HEAD";

                using (HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse())
                {
                    result = true;
                }
            }
            catch
            {
            }

            return result;
        }
    }
}
