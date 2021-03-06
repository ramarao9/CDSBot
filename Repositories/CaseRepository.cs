﻿
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
    public class CaseRepository : ICaseRepository
    {
        private ServiceClient _dataverseServiceClient;
        public CaseRepository(ServiceClient cdsServiceClient)
        {
            _dataverseServiceClient = cdsServiceClient;
        }

        public async Task<Entity> CreateCase(Entity entity)
        {
            Guid caseId = await _dataverseServiceClient.CreateAsync(entity);

            Entity caseRecord = await _dataverseServiceClient.RetrieveAsync("incident", caseId, new ColumnSet("ticketnumber"));
            return caseRecord;
        }

        public async Task<List<Subject>> GetSubjects()
        {
            List<Subject> subjects = new List<Subject>();
            QueryExpression subjectQuery = new QueryExpression("subject");
            subjectQuery.ColumnSet = new ColumnSet("subjectid", "title");

            EntityCollection results = await _dataverseServiceClient.RetrieveMultipleAsync(subjectQuery);

            if (results != null && results.Entities != null && results.Entities.Count > 0)
            {
                subjects = results.Entities.Select(x =>
                new Subject { Id = x.Id, Name = x["title"].ToString() })
                .ToList();
            }

            return subjects;
        }
    }
}
