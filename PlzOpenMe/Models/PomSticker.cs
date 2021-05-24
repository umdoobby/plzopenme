using System;
using System.Collections.Generic;

#nullable disable

namespace PlzOpenMe.Models
{
    public partial class PomSticker
    {
        public long Id { get; set; }
        public long FileId { get; set; }
        public int? Height { get; set; }
        public int? Width { get; set; }
        public bool? IsAnimated { get; set; }
        public string Emoji { get; set; }
        public string SetName { get; set; }
        public string MaskPoint { get; set; }
        public float? MaskShiftX { get; set; }
        public float? MaskShiftY { get; set; }
        public float? MaskScale { get; set; }
    }
}
