using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace CustomVision.Models
{
    public class CustomVisionPrediction
    {
        public string Key { get; set; }
        public string Url { get; set; }
        public string Image { get; set; }

        public CustomVisionPrediction(string predictionKey, string predictionUrl, string imagePath)
        {
            Key = predictionKey;
            Url = predictionUrl;
            Image = imagePath;
        }

        public async Task Predict()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Prediction-Key", Key);

            var byteData = GetImageAsByteArray(Image);

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                var response = await client.PostAsync(Url, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                var model = JsonConvert.DeserializeObject<PredictionResponse>(responseContent);

                Console.WriteLine("\nPrediction Results:\n");

                var result = model.Predictions
                    .Aggregate(string.Empty, (current, item) => current +
                    (String.Format("{0:F2}", (item.Probability * 100)) + "%\t\t" + "Tag: " + item.Tag + "\n"));

                Console.WriteLine(result);
            }
        }

        private static byte[] GetImageAsByteArray(string fileName)
        {
            var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            var binaryReader = new BinaryReader(fileStream);
            return binaryReader.ReadBytes((int)fileStream.Length);
        }
    }
}
