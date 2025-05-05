using HtmlAgilityPack;

namespace Baguettefy.DPLNLink
{
    public static class DLPNLinkFinder
    {
        public static async Task<string> GetFirstSearchResultLinkAsync(string searchTerm)
        {
            try
            {
                string formattedSearch = searchTerm.Replace(" ", "+");
                string url = $"https://www.dofuspourlesnoobs.com/apps/search?q={formattedSearch}";

                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
                client.DefaultRequestHeaders.Add("Accept", "text/html");

                string html = await client.GetStringAsync(url);

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var firstLi = doc.DocumentNode.SelectSingleNode("//ol[@id='wsite-search-list']/li[1]/a[@href]");
                if (firstLi != null)
                {
                    string href = firstLi.GetAttributeValue("href", "");
                    if (!href.StartsWith("http"))
                        href = "https://www.dofuspourlesnoobs.com" + href;

                    return href;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return null;
            }
        }
    }
}
