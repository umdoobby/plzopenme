using System;
using System.Collections.Generic;

#nullable disable

namespace PlzOpenMe.Models
{
    public partial class PomAudio
    {
        public long Id { get; set; }
        public long FileId { get; set; }
        public int? Duration { get; set; }
        public string Title { get; set; }
        public string Performer { get; set; }
    }
}
