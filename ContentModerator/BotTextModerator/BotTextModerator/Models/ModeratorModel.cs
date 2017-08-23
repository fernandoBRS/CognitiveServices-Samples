using System.Collections.Generic;

namespace BotTextModerator.Models
{
    public class ModeratorModel
    {
        public string OriginalText { get; set; }
        public string NormalizedText { get; set; }
        public object Misrepresentation { get; set; }
        public string Language { get; set; }
        public List<TermItem> Terms { get; set; }
        public Status Status { get; set; }
        public string TrackingId { get; set; }
    }

    public class Status
    {
        public int Code { get; set; }
        public string Description { get; set; }
        public object Exception { get; set; }
    }

    public class TermItem
    {
        public int Index { get; set; }
        public int OriginalIndex { get; set; }
        public int ListId { get; set; }
        public string Term { get; set; }
    }
}

