using Newtonsoft.Json;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ImageModeration.Models
{
    public class ImageModerator
    {
        public string SubscriptionKey { get; set; }
        public string ImagePath { get; set; }

        public ImageModerator(string subscriptionKey, string imagePath)
        {
            SubscriptionKey = subscriptionKey;
            ImagePath = imagePath;
        }

        public async Task<ImageResponse> EvaluateImage()
        {
            var uri = $"https://westus.api.cognitive.microsoft.com/contentmoderator/moderate/v1.0/ProcessImage/Evaluate?CacheImage={false}";
            var client = GetHttpClient();

            var byteData = GetImageAsByteArray();

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = GetMediaTypeHeader();

                var response = await client.PostAsync(uri, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<ImageResponse>(responseContent);
            }
        }

        private HttpClient GetHttpClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", SubscriptionKey);

            return client;
        }

        private MediaTypeHeaderValue GetMediaTypeHeader()
        {
            var mediaType = string.Empty;

            if (ImagePath.EndsWith(".png"))
            {
                mediaType = "image/png";
            }
            else if (ImagePath.EndsWith(".jpeg") || ImagePath.EndsWith(".jpg"))
            {
                mediaType = "image/jpeg";
            }
            else if (ImagePath.EndsWith(".bmp"))
            {
                mediaType = "image/bmp";
            }
            else if (ImagePath.EndsWith(".gif"))
            {
                mediaType = "image/gif";
            }

            return new MediaTypeHeaderValue(mediaType);
        }

        private byte[] GetImageAsByteArray()
        {
            var fileStream = new FileStream(ImagePath, FileMode.Open, FileAccess.Read);
            var binaryReader = new BinaryReader(fileStream);
            return binaryReader.ReadBytes((int)fileStream.Length);
        }
    }
}
