using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ListManagement.Utils;
using TextModeration.Models;

namespace TextModeration
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
            Console.WriteLine("=== Content Moderator (Text) ===\n");

            Console.WriteLine("[1] Create new Terms List");
            Console.WriteLine("[2] Show all Terms Lists");
            Console.WriteLine("[3] Update a Terms List\n");

            StartOptionList();
        }

        private static void StartOptionList()
        {
            int option;

            while (!int.TryParse(Console.ReadLine(), out option))
            {
                Console.Write("Please select a valid number: ");
            }

            CreateTermList(option).Wait();
        }

        private static async Task CreateTermList(int option)
        {
            switch (option)
            {
                case (int)Options.Create:
                    await CreateTermsList();
                    break;
                case (int)Options.Show:
                    await ShowTermsLists();
                    break;
                case (int)Options.Update:
                    await UpdateTermsList();
                    break;
            }
        }

        private static async Task CreateTermsList()
        {
            Console.Write("=== Terms List ===\n\n");

            var subscriptionKey = GetInputValue("Subscription Key: ");
            var listName = GetInputValue("Terms List name: ");
            var description = GetInputValue("Description: ");
            var metadata = new Dictionary<string, string>
            {
                { "Category", GetInputValue("Category: ") }
            };
            var filePath = GetInputValue("Terms (csv file): ");

            Console.WriteLine("Loading...");

            var model = new TextModerator(subscriptionKey, listName, description, metadata, filePath);
            await model.CreateList();

            StartOptionList();
        }

        private static async Task ShowTermsLists()
        {
            var subscriptionKey = GetInputValue("Subscription Key: ");

            var model = new TextModerator(subscriptionKey);
            await model.ShowTermLists();

            StartOptionList();
        }

        private static async Task UpdateTermsList()
        {
            var subscriptionKey = GetInputValue("Subscription Key: ");

            int listId;

            while (!int.TryParse(GetInputValue("List ID: "), out listId))
            {
                Console.Write("Invalid ID. ");
            }

            var filePath = GetInputValue("Terms (csv file): ");

            var model = new TextModerator(subscriptionKey, listId, filePath);
            await model.UpdateTermList();

            StartOptionList();
        }
        
        private static string GetInputValue(string description)
        {
            Console.Write(description);
            return Console.ReadLine();
        }
    }
}
