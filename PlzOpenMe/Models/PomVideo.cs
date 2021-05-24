using System;
using System.Collections.Generic;

#nullable disable

namespace PlzOpenMe.Models
{
    public partial class PomVideo
    {
        public long Id { get; set; }
        public long FileId { get; set; }
        public int? Height { get; set; }
        public int? Width { get; set; }
        public int? Duration { get; set; }
    }
}
