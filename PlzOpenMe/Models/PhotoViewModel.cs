namespace PlzOpenMe.Models
{
    public class PhotoViewModel
    {
        /// <summary>
        /// The link object that lead to this page
        /// </summary>
        public PomLink Link { get; set; }
        
        /// <summary>
        /// The photo file associated with the link
        /// </summary>
        public PomFile File { get; set; }

        /// <summary>
        /// The animation file details
        /// </summary>
        public PomPhoto Photo { get; set; }

        /// <summary>
        /// The thumbnail file for this photo
        /// </summary>
        public PomFile ThumbFile { get; set; }

        /// <summary>
        /// The thumbnail file details for this photo
        /// </summary>
        public PomPhoto ThumbPhoto { get; set; }
    }
}