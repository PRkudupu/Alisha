using System;
using System.Net;
using System.Threading.Tasks;

namespace Alisha
{
    public partial class Stock
    {
        //Function to get the stock current stock price and details
        public static async Task<string> GetStockPrice(string stockName)
        {
            //External call to find the stock value
            var url = "http://finance.google.com/finance/info?client=ig&q=NASDAQ:" + stockName;

            string stockOnlyPrice;
            //Get the stock Price
            using (WebClient client = new WebClient())
            {
                try
                {
                    var stockValue = await client.DownloadStringTaskAsync(url);
                    stockOnlyPrice = GetOnlyPrice(stockValue, "NASDAQ", "l_fix");
                }
                catch (Exception)
                {
                    // ignored
                    stockOnlyPrice = string.Empty;
                }
            };
            return stockOnlyPrice;
        }
        /// <summary>
        /// Function to get only the stock price
        /// </summary>
        /// <param name="strSource"></param>
        /// <param name="strStart"></param>
        /// <param name="strEnd"></param>
        /// <returns></returns>
        public static string GetOnlyPrice(string strSource, string strStart, string strEnd)
        {
            if (strSource.Contains(strStart) && strSource.Contains(strEnd))
            {
                var start = strSource.IndexOf(strStart, 0, StringComparison.Ordinal) + strStart.Length + 4;
                var end = strSource.IndexOf(strEnd, start, StringComparison.Ordinal);
                return strSource.Substring(start + 2, end - 4 - start);
            }
            else
            {
                return "";
            }
        }
    }
}