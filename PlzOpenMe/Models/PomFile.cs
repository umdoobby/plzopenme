using System;
using System.Collections.Generic;

#nullable disable

namespace PlzOpenMe.Models
{
    public partial class PomFile
    {
        public int Id { get; set; }
        public DateTime UploadedOn { get; set; }
        public long UploadedBy { get; set; }
        public long PostedBy { get; set; }
        public int Views { get; set; }
        public string Message { get; set; }
        public string Type { get; set; }
        public string Link { get; set; }
        public string Name { get; set; }
        public string OnDisk { get; set; }
        public string CheckSum { get; set; }
        public DateTime? RemovedOn { get; set; }
        public DateTime? VirusScannedOn { get; set; }
        public string RemovalReason { get; set; }
    }
}
