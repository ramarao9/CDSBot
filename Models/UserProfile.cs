using System;

namespace Vij.Bots.DynamicsCRMBot.Models
{
    public class UserProfile
    {
        public string Name { get; set; }
    

        public string IssueType;
        public string Description { get; set; }
        public DateTime CallbackTime { get; set; }
        public string PhoneNumber { get; set; }

        public string EmailAddress { get; set; }

        public InvoiceData LastInvoice { get; set; }

    }
}
