using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Cognitive.CustomVision;
using Microsoft.Cognitive.CustomVision.Models;
using System.IO;
using System.Linq;

namespace CustomVision.Models
{
    public class CustomVisionTraining
    {
        public string Key { get; set; }

        public CustomVisionTraining(string trainingKey)
        {
            Key = trainingKey;
        }

        public void CreateNewProject()
        {
            var credentials = new TrainingApiCredentials(Key);
            var trainingApi = new TrainingApi(credentials);

            // Create the Api, passing in a credentials object that contains the training key
            var project = CreateProject(trainingApi);

            TrainProject(trainingApi, project);
        }
        
        private static ProjectModel CreateProject(ITrainingApi trainingApi)
        {
            Console.Write("Project Name: ");
            var projectName = Console.ReadLine();

            return trainingApi.CreateProject(projectName);
        }

        private static void TrainProject(ITrainingApi trainingApi, ProjectModel project)
        {
            // Before training images, first create tags
            CreateTags(trainingApi, project);

            // Now there are images with tags start training the project
            Console.WriteLine("Training model...");
            var iteration = trainingApi.TrainProject(project.Id);

            // The returned iteration will be in progress, and can be queried periodically to see when it has completed
            while (iteration.Status == "Training")
            {
                Thread.Sleep(1000);

                // Re-query the iteration to get it's updated status
                iteration = trainingApi.GetIteration(project.Id, iteration.Id);
            }

            // The iteration is now trained. Make it the default project endpoint
            iteration.IsDefault = true;
            trainingApi.UpdateIteration(project.Id, iteration.Id, iteration);

            Console.WriteLine("Model trained!\n");
        }

        private static void CreateTags(ITrainingApi trainingApi, ProjectModel project)
        {
            Console.Write("Number of tags: ");

            int numberOfTags;

            while (!int.TryParse(Console.ReadLine(), out numberOfTags))
            {
                Console.Write("Invalid number. Number of tags: ");
            }

            Console.WriteLine();

            for (var i = 1; i <= numberOfTags; i++)
            {
                Console.Write("Name for tag #" + i + ": ");
                var tagName = Console.ReadLine();

                var trainingTag = trainingApi.CreateTag(project.Id, tagName);

                Console.Write("Images folder: ");
                var imagesPath = Console.ReadLine();

                // Upload the images we need for training
                Console.WriteLine("\nLoading images...");

                var images = GetLoadedImagesFromDisk(imagesPath);

                // Upload in a single batch 
                var summary = trainingApi.CreateImagesFromData(project.Id, images, new List<Guid> { trainingTag.Id });

                if (summary.IsBatchSuccessful)
                {
                    Console.WriteLine("Done!\n");
                }
                else
                {
                    Console.WriteLine("Error while uploading images, please check your images path.\n");
                }
            }
        }

        private static List<MemoryStream> GetLoadedImagesFromDisk(string imagesPath)
        {
            var images = String.Format(@"{0}", imagesPath.Replace(@"\\", @"\"));

            // Load the images to be uploaded from disk into memory
            return Directory.GetFiles(images).Select(f => new MemoryStream(File.ReadAllBytes(f))).ToList();
        }
    }
}
