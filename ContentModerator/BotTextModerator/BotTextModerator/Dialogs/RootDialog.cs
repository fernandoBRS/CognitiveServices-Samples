using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Net.Http;
using System.Text;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using BotTextModerator.Models;
using System.Linq;
using System.Configuration;
using System.Collections.Generic;

namespace BotTextModerator.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        private static async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;

            if (activity != null)
            {
                var moderatedText = await ModerateText(activity.Text);

                await context.PostAsync(moderatedText);
            }

            context.Wait(MessageReceivedAsync);
        }

        static async Task<string> ModerateText(string text)
        {
            var client = new HttpClient();

            var uri = ConfigurationManager.AppSettings["ContentModeratorUri"];
            var key = ConfigurationManager.AppSettings["ContentModeratorKey"];
            

            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);
            
            // Request body 
            byte[] byteData = Encoding.UTF8.GetBytes(text);

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
                
                var response = await client.PostAsync(uri, content);
                var result = response.Content.ReadAsStringAsync().Result;
                var model = JsonConvert.DeserializeObject<ModeratorModel>(result);
                
                if (model.Terms == null || !model.Terms.Any())
                {
                    return text;
                }

                return GetModeratedText(text, model.Terms);
            }
        }

        private static string GetModeratedText(string text, List<TermItem> terms)
        {
            var moderatedText = text;

            foreach (var item in terms)
            {
                var substring = item.Term.Substring(1, item.Term.Length - 1);

                var maskedSubstring = string.Concat(
                    substring.Select(x => '*')
                );

                var moderatedWord = item.Term[0] + maskedSubstring;

                moderatedText = moderatedText.Replace(item.Term, moderatedWord);
            }

            return moderatedText;
        }
    }
}