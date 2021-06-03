namespace PlzOpenMe.Models
{
    public class DocumentViewModel
    {
        /// <summary>
        /// The link object that leads to this page
        /// </summary>
        public PomLink Link { get; set; }
        
        /// <summary>
        /// The document file associated with the link
        /// </summary>
        public PomFile File { get; set; }

        /// <summary>
        /// The thumbnail file for this document
        /// </summary>
        public PomFile ThumbFile { get; set; }

        /// <summary>
        /// The thumbnail file details for this document
        /// </summary>
        public PomPhoto ThumbPhoto { get; set; }
    }
}