using Microsoft.PowerPlatform.Cds.Client;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vij.Bots.DynamicsCRMBot.Interfaces;
using Vij.Bots.DynamicsCRMBot.Models;

namespace Vij.Bots.DynamicsCRMBot.Repositories
{
    public class InvoiceRepository : IInvoiceRepository
    {
        private CdsServiceClient _cdsServiceClient;
        public InvoiceRepository(CdsServiceClient cdsServiceClient)
        {
            _cdsServiceClient = cdsServiceClient;
        }

        public async Task<string> CreateInvoice(EntityReference customer, InvoiceData invoiceData)
        {

            Entity invoice = new Entity("invoice");
            invoice["customerid"] = customer;
            invoice["name"] = "Test for Vision";

            invoice["pricelevelid"] = new EntityReference("pricelevel", new Guid("d437e569-e7c2-e411-80df-fc15b42886e8"));

            Guid invoiceId = await _cdsServiceClient.CreateAsync(invoice);

            foreach (Entity invoiceLine in invoiceData.InvoiceLines)
            {
                invoiceLine["invoiceid"] = new EntityReference("invoice", invoiceId);
                await _cdsServiceClient.CreateAsync(invoiceLine);
            }


            Entity invoiceAfterCreate = await _cdsServiceClient.RetrieveAsync("invoice", invoiceId, new Microsoft.Xrm.Sdk.Query.ColumnSet("invoicenumber"));

            return invoiceAfterCreate.GetAttributeValue<string>("invoicenumber");

        }

    }
}
