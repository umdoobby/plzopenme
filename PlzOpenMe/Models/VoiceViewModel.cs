namespace PlzOpenMe.Models
{
    public class VoiceViewModel
    {
        /// <summary>
        /// The link object that leads to this page
        /// </summary>
        public PomLink Link { get; set; }
        
        /// <summary>
        /// The voice file associated with the link
        /// </summary>
        public PomFile File { get; set; }

        /// <summary>
        /// The voice file details
        /// </summary>
        public PomVoice Voice { get; set; }
    }
}