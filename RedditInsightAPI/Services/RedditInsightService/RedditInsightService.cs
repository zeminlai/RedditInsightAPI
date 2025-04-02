using RedditInsightAPI.Dtos.RedditInsight;
using Microsoft.AspNetCore.Mvc;
using PuppeteerSharp;
using Reddit.Controllers;
using Reddit;
using System.Text.RegularExpressions;
using RedditInsightAPI.Models;
using System.Web;
using Newtonsoft.Json;
using RestSharp;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HtmlAgilityPack;
using System.Security.Claims;

namespace RedditInsightAPI.Services.RedditInsightService
{
    public class RedditInsightService : IRedditInsightService
    {
        public async Task<GetRedditInsightResultsDto> GetRedditInsight(string searchTerm)
        {

            Console.WriteLine("searchTerm: " + searchTerm);

            //List<string> urlList = await GetUrlListFromWebScrape(searchTerm);
            List<string> urlList = await GetUrlListFromGoogleApi(searchTerm);

            GetRedditInsightResultsDto redditInsightResults = new GetRedditInsightResultsDto
            {
                RedditPosts = new List<RedditPost>(),
            };

            int urlPosition = 0;

            Parallel.ForEach(urlList, new ParallelOptions { MaxDegreeOfParallelism = 10 }, url =>
            {
                urlPosition++;
                RedditPost redditPost = GetSingleRedditPost(url, urlPosition);
                redditInsightResults.RedditPosts.Add(redditPost);
            });

            redditInsightResults.RedditPosts = redditInsightResults.RedditPosts
                .OrderBy(redditPost => redditPost.Id)
                .ToList();

            Console.WriteLine("Done");

            return redditInsightResults;
        }

        private async Task<List<string>> GetUrlListFromHiddenGoogleApi(string searchTerm)
        {
            int maxUrls = 10;
            var client = new RestClient("https://www.google.com/search");
            var request = new RestRequest(Method.GET);
            request.AddParameter("q", "site: reddit.com " + searchTerm);
            request.AddParameter("oq", "site:");
            //request.AddCookie("1P_JAR", "2024-03-05-05");
            request.AddCookie("SID", "g.a000ugjZC3GgtAZ3XsgWOkNJO2gbJhrD7vQK_UjkYTGCiff3Jk2DQ3QVAXg7_hzgAPNaEfL3DAACgYKASwSARUSFQHGX2MiGcnbOmlJReuRqQ_gbqmkehoVAUF8yKp-Ppg0sRQPT4zhlKBgNGHD0076");
            request.AddCookie("__Secure-1PSID", "g.a000ugjZC3GgtAZ3XsgWOkNJO2gbJhrD7vQK_UjkYTGCiff3Jk2DEaEesNGCthXbDEqUvDWGnQACgYKAQwSARUSFQHGX2MigDyDZe_U4sMqAqPSSs9JUBoVAUF8yKqW0GGSyncs5-5IZvXN6J2J0076");
            request.AddCookie("HSID", "A4yn-LtG4__r3cEtK");
            request.AddCookie("SSID", "AF8njFyRUf46PXB2-");
            request.AddCookie("APISID", "V3sa5vtCacUoKzdX/AJFkniZeZMWLVSL4X");
            request.AddCookie("SAPISID", "9JajJzHSmDpqHL94/AwZFVm3t-KFFRW5H2");
            request.AddCookie("NID", "NID=522=X7ILZPYkPIRGy_bY7mmRZiBVofhQxBw1C9XnbawLdNvMaYMjbI4BUDFVu9q6pQIpUmJn_itNyJID-QeEd9PpTNo4GSRHCCYlR72Rgdb9AqezxvEq3pH5FLAOuFn6TrV_RS3orm3h5-kGCTr3NxvOyMis6-h8IVsYDPDOdx07TH-cDJJIh0yTpkvEakKsZkzScjiGU5nyyFn_YzPvPiYBUYinKJJcfTzOmVn46QBeAsm1T48aQlsbmLQuVBl-M1x5utRU9nAVYxTi0yqk3krhyxyCf2N7ZPeOqNjfRqJNlZzgp5npMil5Uriydg5Cgs6lwfpZZHgJubcEGBVvARJqdWS_SANh-PkH6xkiTguhBG0bvAYv7Q5HVn1fYm6b1_5gtjd-VkBcbYhA4sBM-cpCuz_5Z9k4CZfULsm4iiCDTH2s-pelNu5RfdVpXZGh6cavD3eCYaQ3Y_erbqjnDaO5XALFBuYRjzl7KNZWxuegDKVBJVUNpyig1V7wvFYLmIEoP5ERm-V4vH-csHbJz9bi0WbjpSlroVvWFPKkz77py3Uq3UCy6fL-ISaZeGe1vxoAYkJwL_2aFn7S-KWHxGrZYbl37JY2LoHVndU9qpNsbvZrg6KLSUn-H8mzvx85qE8AYxVAoMFTt1Zv4H0YCcHrc35-w1QauCkni3zA9HSt-hbc8IACKT14m9kQcwm25RJKe0_K_gGLPTRAlKVHuPRgnioTmEcP0_56LtSmcyzldt1aSGwfdidgZyNUxVcT5U9WmLL2FZaBuI2nkuHfVVWeINpP4qBZZh_r2_7syYXKTG3E5dB5bZjTjOSLGfFW_3DTME8ILWJiLxP5Lzg46ZFmuxRl7EWd-NpkzOkizLyNC4QEYPUtLj9BsqxCLss");
            request.AddCookie("AEC", "AVcja2eNVhTYqR8TleVeA5xkA-EGWQRn4w8iL-nSUqPsBlLYjJWJ_77RFQ");
            IRestResponse response = client.Execute(request);

            var doc = new HtmlDocument();
            doc.LoadHtml(response.Content);
            var htmlBody = doc.DocumentNode.SelectNodes("//body/div[@id = 'main\']/div/div[@class='Gx5Zad fP1Qef xpd EtOod pkphOe\']");
            List<string> googleApiUrlList = new List<string>();

            foreach (var htmlItem in htmlBody)
            {
                string url = htmlItem.SelectSingleNode("div[@class='egMi0 kCrYT\']/a").Attributes["href"].Value;
                url = url.Substring(url.IndexOf("=") + 1);
                url = url.Substring(0, url.LastIndexOf('/'));
                Console.WriteLine(url);
                googleApiUrlList.Add(url);
            }

            googleApiUrlList = googleApiUrlList
                    .Where(url => url.Contains("https://www.reddit.com") && url.Contains("/comments/"))
                    .Where(url => (url.Count(c => c == '/') > 5))
                    .Take(maxUrls).ToList();

            return googleApiUrlList;
        }
        private async Task<List<string>> GetUrlListFromGoogleApi(string searchTerm)
        {
            int maxUrls = 10;
            var client = new HttpClient();

            var builder = new UriBuilder("https://www.googleapis.com/customsearch/v1");
            builder.Port = -1;
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["key"] = "AIzaSyDaU1kU_RSH1RWgkLXvYmU_umVUfIEU8bU";
            query["cx"] = "57bf992fba6c74a45";
            query["q"] = "site:reddit.com " + searchTerm;
            builder.Query = query.ToString();
            string url = builder.ToString();

            try
            {
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var jsonResponse = await response.Content.ReadAsStringAsync();

                var searchResult = JsonConvert.DeserializeObject<dynamic>(jsonResponse);
                List<string> urlList = new List<string>();

                if (searchResult.items != null)
                {
                    foreach (var item in searchResult.items)
                    {
                        string link = item.link.ToString();
                        Console.WriteLine($"Found URL: {link}");
                        urlList.Add(link);
                    }
                }

                // Filter and limit results
                urlList = urlList
                    .Where(url => url.Contains("https://www.reddit.com") && url.Contains("/comments/"))
                    .Where(url => (url.Count(c => c == '/') > 5))
                    .Take(maxUrls)
                    .ToList();

                return urlList;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching URLs: {ex.Message}");
                return new List<string>();
            }
        }

        private RedditPost GetSingleRedditPost(string url, int urlPosition)
        {
            Console.WriteLine("Getting single reddit post:" + urlPosition);
            int maxComments = 5;
            var reddit = new RedditClient(appId: "IoYkiBExqL67zvpHax8cXA", appSecret: "qkKymYZi0eb5I-PdSHqydNQxmhIHCg", refreshToken: "273912227236-WXvOz9T-L-JQ7JPGfAL5gDVpFkuZLg");

            // Get the ID from the permalink, then preface it with "t3_" to convert it to a Reddit fullname.
            Match match = Regex.Match(url, @"\/comments\/([a-z0-9]+)\/");

            string postFullname = "t3_" + (match != null && match.Groups != null && match.Groups.Count >= 2
                ? match.Groups[1].Value
                : "");
            if (postFullname.Equals("t3_"))
            {
                throw new Exception("Unable to extract ID from permalink.");
            }

            Console.WriteLine("Fetching single reddit post: " + urlPosition);
            // Retrieve the post and return the result. 
            var post = reddit.Post(postFullname).About();
            Console.WriteLine("Fetching done: " + urlPosition);

            RedditPost redditPost = new RedditPost
            {
                Id = urlPosition,
                Title = post.Title,
                Upvotes = post.UpVotes,
                Subreddit = post.Subreddit,
                Author = post.Author,
                CreatedAt = post.Created,
                Body = post.Listing.IsSelf
                ? ((SelfPost)post).SelfText
                : ((LinkPost)post).URL,
                Comments = new List<RedditComment>(),
                Link = url,
            };

            var comments = post.Comments.GetConfidence().Take(maxComments);
            foreach (Comment comment in comments)
            {
                var replies = comment.Replies;
                RedditComment redditComment = new RedditComment
                {
                    Author = comment.Author,
                    CreatedAt = comment.Created,
                    Upvotes = comment.UpVotes,
                    Body = comment.Body,
                };
                redditPost.Comments.Add(redditComment);
            }
            Console.WriteLine("Single reddit post done");
            return redditPost;
        }

        private async Task<List<string>> GetUrlListFromWebScrape(string searchTerm)
        {
            Console.WriteLine("Getting list of urls....");
            var urlList = new List<string>();

            // download the browser executable
            await new BrowserFetcher().DownloadAsync();

            // browser execution configs
            var launchOptions = new LaunchOptions
            {
                Headless = true,
            };

            // open a new page in the controlled browser
            using (var browser = await Puppeteer.LaunchAsync(launchOptions))
            using (var page = await browser.NewPageAsync())
            {
                await page.SetRequestInterceptionAsync(true);

                page.Request += (sender, e) =>

                {
                    if (e.Request.ResourceType == ResourceType.Image)
                        e.Request.AbortAsync();
                    else
                        e.Request.ContinueAsync();
                };

                await page.GoToAsync("https://www.google.com", waitUntil: WaitUntilNavigation.DOMContentLoaded);
                var googleSearchBox = await page.QuerySelectorAsync(".gLFyf");
                await googleSearchBox.TypeAsync("site: reddit.com " + searchTerm);
                await page.Keyboard.PressAsync("Enter");

                await page.WaitForNavigationAsync();

                var jsSelectAllAnchors = @"Array.from(document.querySelectorAll('.yuRUbf a')).map(a => a.href);";
                var urls = await page.EvaluateExpressionAsync<string[]>(jsSelectAllAnchors);
                //urlList = urls.Take(5).ToList();

                urlList = urls
                    .Where(url => url.Contains("https://www.reddit.com") && url.Contains("/comments/"))
                    .Where(url => (url.Count(c => c == '/') > 5))
                    .Take(5).ToList();

                var h3titles = await page.QuerySelectorAllAsync(".DKV0Md");
                Console.WriteLine("Url list scraped");

                foreach (string url in urlList)
                {
                    Console.WriteLine(url);
                }

                return urlList;
            }
        }
        //private async Task<List<string>> GetUrlListFromGoogleApi(string searchTerm)
        //{
        //    HttpClient client = new HttpClient();

        //    var builder = new UriBuilder("https://customsearch.googleapis.com/customsearch/v1");
        //    builder.Port = -1;
        //    var query = HttpUtility.ParseQueryString(builder.Query);
        //    query["cx"] = "b1a638f038c914afc";
        //    query["q"] = "site: reddit.com " + searchTerm;
        //    query["key"] = "AIzaSyDMBMHZ-dGJymEqm8lX83QTePAjatjMmv8";
        //    builder.Query = query.ToString();
        //    string url = builder.ToString();

        //    var responseString = await client.GetStringAsync(url);
        //    var myDeserializedClass = JsonConvert.DeserializeObject<dynamic>(responseString);

        //    List<string> googleApiUrlList = new List<string>();

        //    foreach (var items in myDeserializedClass.items)
        //    {
        //        googleApiUrlList.Add(items.link.ToString());
        //        Console.WriteLine(items.link);
        //    }

        //    googleApiUrlList = googleApiUrlList
        //            .Where(url => url.Contains("https://www.reddit.com") && url.Contains("/comments/"))
        //            .Where(url => (url.Count(c => c == '/') > 5))
        //            .Take(5).ToList();

        //    return googleApiUrlList;
        //}

    }
}
