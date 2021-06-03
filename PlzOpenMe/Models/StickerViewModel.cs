namespace PlzOpenMe.Models
{
    public class StickerViewModel
    {
        /// <summary>
        /// The link object that leads to this page
        /// </summary>
        public PomLink Link { get; set; }
        
        /// <summary>
        /// The sticker file associated with the link
        /// </summary>
        public PomFile File { get; set; }

        /// <summary>
        /// The sticker file details
        /// </summary>
        public PomSticker Sticker { get; set; }

        /// <summary>
        /// The thumbnail file for this sticker
        /// </summary>
        public PomFile ThumbFile { get; set; }

        /// <summary>
        /// The thumbnail file details for this sticker
        /// </summary>
        public PomPhoto ThumbPhoto { get; set; }
    }
}