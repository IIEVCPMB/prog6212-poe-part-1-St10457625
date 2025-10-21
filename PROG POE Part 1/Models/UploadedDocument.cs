namespace PROG_POE_Part_1.Models
{
    public class UploadedDocument
    {
        public int ID { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public DateTime UploadedDate { get; set; }
        public bool IsEncrypted { get; set; } = true;
    }
}
