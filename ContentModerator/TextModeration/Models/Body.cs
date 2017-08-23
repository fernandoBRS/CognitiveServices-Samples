using System.Collections.Generic;

namespace TextModeration.Models
{
    public class Body
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
    }
}
