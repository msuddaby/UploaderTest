using System.ComponentModel.DataAnnotations.Schema;

namespace UploaderTest.Models
{
    public class Media
    {
        public int Id { get; set; }
        public string Url { get; set; }
        [Column(TypeName = "jsonb")]
        public string? Insights { get; set; }
        public UploadStatus Status { get; set; }
    }

    public enum UploadStatus
    {
        PreUpload,
        Uploaded,
        Processing,
        Processed
    }
}
