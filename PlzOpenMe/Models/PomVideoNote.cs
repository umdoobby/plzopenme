using System;
using System.Collections.Generic;

#nullable disable

namespace PlzOpenMe.Models
{
    public partial class PomVideoNote
    {
        public long Id { get; set; }
        public long FileId { get; set; }
        public int? Length { get; set; }
        public int? Duration { get; set; }
    }
}
