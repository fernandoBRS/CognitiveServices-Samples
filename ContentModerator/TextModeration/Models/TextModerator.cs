using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TextModeration.Models
{ 
    public class TextModerator
    {
        public string SubscriptionKey { get; set; }
        public string TermsListName { get; set; }
        public string ListDescription { get; set; }
        public Dictionary<string, string> ListMetadata { get; set; }
        public string FilePath { get; set; }
        public int ListId { get; set; }

        public string DefaultLanguage { get; } = "eng"; // en-US

        public TextModerator(string subscriptionKey)
        {
            SubscriptionKey = subscriptionKey;
        }

        public TextModerator(string subscriptionKey, int listId, string filePath)
        {
            SubscriptionKey = subscriptionKey;
            ListId = listId;
            FilePath = filePath;
        }

        public TextModerator(string subscriptionKey, 
                            string listName, 
                            string description, 
                            Dictionary<string, string> metadata,
                            string filePath)
        {
            SubscriptionKey = subscriptionKey;
            TermsListName = listName;
            ListDescription = description;
            ListMetadata = metadata;
            FilePath = filePath;
        }

        public async Task CreateList()
        {
            const string uri = "https://westus.api.cognitive.microsoft.com/contentmoderator/lists/v1.0/termlists";
            var client = GetHttpClient();
            var body = GetBody();

            using (var content = new ByteArrayContent(body))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                var response = await client.PostAsync(uri, content);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Console.WriteLine("Terms list created successfully!");

                    var result = response.Content.ReadAsStringAsync().Result;
                    var output = JsonConvert.DeserializeObject<TermList>(result);

                    await AddTerms(output.Id);
                }
            }
        }

        public async Task ShowTermLists()
        {
            const string uri = "https://westus.api.cognitive.microsoft.com/contentmoderator/lists/v1.0/termlists";
            var client = GetHttpClient();

            var response = await client.GetAsync(uri);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var result = response.Content.ReadAsStringAsync().Result;
                var lists = JsonConvert.DeserializeObject<List<TermList>>(result);

                Console.WriteLine();

                foreach (var termList in lists)
                {
                    Console.WriteLine("Id: " + termList.Id);
                    Console.WriteLine("Name: " + termList.Name);
                    Console.WriteLine("Description: " + termList.Description + "\n");
                }
            }
        }

        public async Task UpdateTermList()
        {
            await AddTerms(ListId);
        }

        private HttpClient GetHttpClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", SubscriptionKey);

            return client;
        }

        private byte[] GetBody()
        {
            var model = new Body
            {
                Name = TermsListName,
                Description = ListDescription,
                Metadata = ListMetadata
            };

            var body = JsonConvert.SerializeObject(model, Formatting.None);

            return Encoding.UTF8.GetBytes(body);
        }

        private async Task AddTerms(int listId)
        {
            try
            {
                using (var reader = new StreamReader($@"{FilePath}"))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();

                        if (line == null) continue;

                        var term = line.Split(',')[0];
                        var uri = $"https://westus.api.cognitive.microsoft.com/contentmoderator/lists/v1.0/termlists/{listId}/terms/{term}?language={DefaultLanguage}";
                        var client = GetHttpClient();

                        await client.PostAsync(uri, null);

                        Thread.Sleep(1000);
                    }
                }

                await RefreshIndex(listId);
            }
            catch (IOException ioex)
            {
                throw new IOException("Error: ", ioex);
            }
            catch (Exception ex)
            {
                throw new Exception("Error: ", ex);
            }
        }

        private async Task RefreshIndex(int listId)
        {
            var uri = $"https://westus.api.cognitive.microsoft.com/contentmoderator/lists/v1.0/termlists/{listId}/RefreshIndex?language={DefaultLanguage}";
            var client = GetHttpClient();

            var response = await client.PostAsync(uri, null);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Console.WriteLine("Terms added sucessfully!");
            }
        }
    }
}
