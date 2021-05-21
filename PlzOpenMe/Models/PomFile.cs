using System;
using System.Collections.Generic;

#nullable disable

namespace PlzOpenMe.Models
{
    public partial class PomFile
    {
        public long Id { get; set; }
        public string FileId { get; set; }
        public string FileUniqueId { get; set; }
        public int Size { get; set; }
        public string Mime { get; set; }
        public string Type { get; set; }
        public string Location { get; set; }
        public DateTime UploadedOn { get; set; }
        public DateTime? DeletedOn { get; set; }
    }
}
