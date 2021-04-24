
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vij.Bots.DynamicsCRMBot.Interfaces;
using Vij.Bots.DynamicsCRMBot.Models;

namespace Vij.Bots.DynamicsCRMBot.Repositories
{
    public class ContactRepository : IContactRepository
    {
        private ServiceClient _dataverseServiceClient;
        public ContactRepository(ServiceClient cdsServiceClient)
        {
            _dataverseServiceClient = cdsServiceClient;
        }

        public async Task<EntityReference> CreateContact(UserProfile userProfile)
        {
            Entity contact = new Entity("contact");
            contact["emailaddress1"] = userProfile.EmailAddress;

            string[] contactName = userProfile.Name.Split(' ').ToArray();
            if (contactName.Length == 2)
            {
                contact["lastname"] = contactName[1];
            }
            contact["firstname"] = contactName[0];

            Guid contactId = await _dataverseServiceClient.CreateAsync(contact);
            return new EntityReference("contact", contactId);
        }

        public async Task<EntityReference> FindContact(UserProfile userProfile)
        {
            QueryExpression query = new QueryExpression("contact");
            query.ColumnSet = new ColumnSet("contactid");
            query.Criteria.AddCondition("emailaddress1", ConditionOperator.Equal, userProfile.EmailAddress);

            EntityCollection results = await _dataverseServiceClient.RetrieveMultipleAsync(query);

            if (results != null && results.Entities != null && results.Entities.Count == 1)
            {
                return results.Entities[0].ToEntityReference();
            }
            else
            {
                return null;
            }
        }


        public async Task<EntityReference> FindContact(string fullName)
        {
            QueryExpression query = new QueryExpression("contact");
            query.ColumnSet = new ColumnSet("contactid");
            query.Criteria.AddCondition("fullname", ConditionOperator.Equal, fullName);

            EntityCollection results = await _dataverseServiceClient.RetrieveMultipleAsync(query);

            if (results != null && results.Entities != null && results.Entities.Count == 1)
            {
                return results.Entities[0].ToEntityReference();
            }
            else
            {
                return null;
            }
        }


    }
}
