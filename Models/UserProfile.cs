using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Vij.Bots.DynamicsCRMBot.Models
{
    public class UserProfile
    {
        public string Name { get; set; }
        public bool GreetingComplete;

        public string IssueType;
        public string Description { get; set; }
        public DateTime CallbackTime { get; set; }
        public string PhoneNumber { get; set; }

    }
}
