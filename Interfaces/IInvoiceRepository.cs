using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vij.Bots.DynamicsCRMBot.Models;

namespace Vij.Bots.DynamicsCRMBot.Interfaces
{
    public interface IInvoiceRepository
    {

        public  Task<string> CreateInvoice(EntityReference customer, InvoiceData invoiceData);
    }
}
