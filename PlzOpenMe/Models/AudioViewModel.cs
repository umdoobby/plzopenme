namespace PlzOpenMe.Models
{
    public class AudioViewModel
    {
        /// <summary>
        /// The link object that leads to this page
        /// </summary>
        public PomLink Link { get; set; }
        
        /// <summary>
        /// The audio file associated with the link
        /// </summary>
        public PomFile File { get; set; }

        /// <summary>
        /// The audio file details
        /// </summary>
        public PomAudio Audio { get; set; }

        /// <summary>
        /// The thumbnail file for this audio file
        /// </summary>
        public PomFile ThumbFile { get; set; }

        /// <summary>
        /// The thumbnail file details for this audio file
        /// </summary>
        public PomPhoto ThumbPhoto { get; set; }
    }
}