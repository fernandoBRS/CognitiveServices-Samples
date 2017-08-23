using ImageModeration.Models;
using System;
using System.Threading.Tasks;

namespace ImageModeration
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            Initialize();
            Console.ReadKey();
        }

        private static void Initialize()
        {
            Console.WriteLine("=== Content Moderation (Image) ===\n");

            var subscriptionKey = GetInputValue("Subscription Key: ");
            var imagePath = GetInputValue("Image: ");

            EvaluateImage(subscriptionKey, imagePath).Wait();
        }

        private static async Task EvaluateImage(string subscriptionKey, string imagePath)
        {
            var moderator = new ImageModerator(subscriptionKey, imagePath);

            var result = await moderator.EvaluateImage();

            Console.WriteLine("\nAdult Classification: " + result.AdultClassificationScore + " (" + result.IsImageAdultClassified + ")");
            Console.WriteLine("Racy Classification: " + result.RacyClassificationScore + " (" + result.IsImageRacyClassified + ")");
        }

        private static string GetInputValue(string description)
        {
            Console.Write(description);
            return Console.ReadLine();
        }
    }
}
