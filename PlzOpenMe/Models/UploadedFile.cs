namespace PlzOpenMe.Models
{
    public class UploadedFile
    {
        /// <summary>
        /// File name from telegram
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// POM file id associated with this name
        /// </summary>
        public long Id { get; set; }
    }
}