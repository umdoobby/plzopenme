namespace PlzOpenMe.Models
{
    public class VideoViewModel
    {
        /// <summary>
        /// The link object that leads to this page
        /// </summary>
        public PomLink Link { get; set; }
        
        /// <summary>
        /// The video file associated with the link
        /// </summary>
        public PomFile File { get; set; }

        /// <summary>
        /// The video file details
        /// </summary>
        public PomVideo Video { get; set; }

        /// <summary>
        /// The thumbnail file for this video
        /// </summary>
        public PomFile ThumbFile { get; set; }

        /// <summary>
        /// The thumbnail file details for this video
        /// </summary>
        public PomPhoto ThumbPhoto { get; set; }
    }
}