using Microsoft.PowerPlatform.Cds.Client;
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
    public class SubjectRepository : ISubjectRepository
    {

        private CdsServiceClient _cdsServiceClient;
        public SubjectRepository(CdsServiceClient cdsServiceClient)
        {
            _cdsServiceClient = cdsServiceClient;
        }

        public async Task<List<Subject>> GetSubjects()
        {
            List<Subject> subjects = new List<Subject>();
            QueryExpression subjectQuery = new QueryExpression("subject");
            subjectQuery.ColumnSet = new ColumnSet("subjectid", "title");
            subjectQuery.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);

            EntityCollection results = _cdsServiceClient.RetrieveMultiple(subjectQuery);

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
