using System;
using System.Collections.Generic;

#nullable disable

namespace PlzOpenMe.Models
{
    public partial class PomUser
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public bool IsBot { get; set; }
        public string FirstName { get; set; }
        public string? LastName { get; set; }
        public string Username { get; set; }
        public string LanguageCode { get; set; }
        public DateTime Created { get; set; }
        public bool HasAgreed { get; set; }
        public DateTime? AgreedOn { get; set; }
    }
}
