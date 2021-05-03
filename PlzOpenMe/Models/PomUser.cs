using System;
using System.Collections.Generic;

#nullable disable

namespace PlzOpenMe.Models
{
    public partial class PomUser
    {
        public int Id { get; set; }
        public long UserId { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? AgreedOn { get; set; }
        public bool? Agreed { get; set; }
        public bool Banned { get; set; }
        public DateTime? BannedOn { get; set; }
    }
}
