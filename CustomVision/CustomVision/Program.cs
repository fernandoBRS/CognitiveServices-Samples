using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Cognitive.CustomVision;
using Microsoft.Cognitive.CustomVision.Models;
using CustomVision.Utils;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using CustomVision.Models;

namespace CustomVision
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Initialize();
        }

        private static void Initialize()
        {
            Console.WriteLine("=== Custom Vision Service ===\n");

            Console.WriteLine("[1] Create a new project");
            Console.WriteLine("[2] Test an image\n");

            StartOptionList().Wait();
        }

        private static async Task StartOptionList()
        {
            var option = 1;

            while (!int.TryParse(Console.ReadLine(), out option))
                Console.Write("Enter a valid option: ");

            switch (option)
            {
                case (int)Option.Create:
                    CreateNewProject();
                    break;
                case (int)Option.Test:
                    await MakePrediction();
                    break;
                default:
                    CreateNewProject();
                    break;
            }

            Console.ReadKey();
        }

        private static void CreateNewProject()
        {
            Console.WriteLine("Minimum requirements: At least 2 tags with 5 images for each tag.\n");

            Console.Write("Training Key: ");
            var trainingKey = GetKey(Console.ReadLine());

            var model = new CustomVisionTraining(trainingKey);

            model.CreateNewProject();
        }
        
        private static async Task MakePrediction()
        {
            Console.Write("Prediction Key: ");
            var predictionKey = GetKey(Console.ReadLine());

            Console.Write("Prediction URL: ");
            var predictionUrl = Console.ReadLine();

            Console.Write("Image path: ");
            var image = Console.ReadLine();

            var model = new CustomVisionPrediction(predictionKey, predictionUrl, image);

            await model.Predict();
        }

        private static string GetKey(string trainingKey)
        {
            while (string.IsNullOrWhiteSpace(trainingKey) || trainingKey.Length != 32)
            {
                Console.Write("Invalid key. Enter your key: ");
                trainingKey = Console.ReadLine();
            }

            return trainingKey;
        }
    }
}
