namespace PlzOpenMe.Models
{
    public class VideoNoteViewModel
    {
        /// <summary>
        /// The link object that leads to this page
        /// </summary>
        public PomLink Link { get; set; }
        
        /// <summary>
        /// The video note file associated with the link
        /// </summary>
        public PomFile File { get; set; }

        /// <summary>
        /// The video note file details
        /// </summary>
        public PomVideoNote VideoNote { get; set; }

        /// <summary>
        /// The thumbnail file for this video note
        /// </summary>
        public PomFile ThumbFile { get; set; }

        /// <summary>
        /// The thumbnail file details for this video note
        /// </summary>
        public PomPhoto ThumbPhoto { get; set; }
    }
}