using System;
using System.Collections.Generic;

#nullable disable

namespace PlzOpenMe.Models
{
    public partial class PomLink
    {
        public long Id { get; set; }
        public long File { get; set; }
        public string Link { get; set; }
        public long? Thumbnail { get; set; }
        public long Views { get; set; }
        public long UserId { get; set; }
        public string Name { get; set; }
        public DateTime AddedOn { get; set; }
        public DateTime? RemovedOn { get; set; }
    }
}
