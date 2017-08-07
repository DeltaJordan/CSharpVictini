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
        public static bool URLExists(string url)
        {
            bool result = false;

            WebRequest webRequest = WebRequest.Create(url);
            webRequest.Timeout = 1200; // miliseconds
            webRequest.Method = "HEAD";

            HttpWebResponse response = null;

            try
            {
                response = (HttpWebResponse)webRequest.GetResponse();
                result = true;
            }
            catch (WebException webException)
            {
                Console.WriteLine(url + " doesn't exist: " + webException.Message);
            }
            finally
            {
                response?.Close();
            }

            return result;
        }
    }
}
