namespace ImageModeration.Models
{
    public class ImageResponse
    {
        public float AdultClassificationScore { get; set; }
        public bool IsImageAdultClassified { get; set; }
        public float RacyClassificationScore { get; set; }
        public bool IsImageRacyClassified { get; set; }
        public object[] AdvancedInfo { get; set; }
        public bool Result { get; set; }
        public Status Status { get; set; }
        public string TrackingId { get; set; }
    }

    public class Status
    {
        public int Code { get; set; }
        public string Description { get; set; }
        public object Exception { get; set; }
    }
}
