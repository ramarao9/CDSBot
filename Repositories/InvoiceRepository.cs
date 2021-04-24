
using Microsoft.PowerPlatform.Dataverse.Client;
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
        private ServiceClient _dataverseServiceClient;
        public InvoiceRepository(ServiceClient cdsServiceClient)
        {
            _dataverseServiceClient = cdsServiceClient;
        }

        public async Task<string> CreateInvoice(EntityReference customer, InvoiceData invoiceData)
        {

            Entity invoice = new Entity("invoice");
            invoice["customerid"] = customer;
            invoice["name"] = "Test for Form Recognizer";

            invoice["pricelevelid"] = new EntityReference("pricelevel", new Guid("d437e569-e7c2-e411-80df-fc15b42886e8"));

            Guid invoiceId = await _dataverseServiceClient.CreateAsync(invoice);

            foreach (Entity invoiceLine in invoiceData.InvoiceLines)
            {
                invoiceLine["invoiceid"] = new EntityReference("invoice", invoiceId);
                await _dataverseServiceClient.CreateAsync(invoiceLine);
            }


            Entity invoiceAfterCreate = await _dataverseServiceClient.RetrieveAsync("invoice", invoiceId, new Microsoft.Xrm.Sdk.Query.ColumnSet("invoicenumber"));

            return invoiceAfterCreate.GetAttributeValue<string>("invoicenumber");

        }

    }
}
