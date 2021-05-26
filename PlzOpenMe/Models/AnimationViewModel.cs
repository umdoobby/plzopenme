namespace PlzOpenMe.Models
{
    public class AnimationViewModel
    {
        /// <summary>
        /// The link object that lead to this page
        /// </summary>
        public PomLink Link { get; set; }
        
        /// <summary>
        /// The animation file associated with the link
        /// </summary>
        public PomFile File { get; set; }

        /// <summary>
        /// The animation file details
        /// </summary>
        public PomAnimation Animation { get; set; }

        /// <summary>
        /// The thumbnail file for this animation
        /// </summary>
        public PomFile ThumbFile { get; set; }

        /// <summary>
        /// The thumbnail file details for this animation
        /// </summary>
        public PomPhoto ThumbPhoto { get; set; }
    }
}