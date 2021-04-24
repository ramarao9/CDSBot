using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vij.Bots.DynamicsCRMBot.Models;

namespace Vij.Bots.DynamicsCRMBot.Interfaces
{
    public interface IContactRepository
    {
        public Task<EntityReference> FindContact(UserProfile userProfile);

        public Task<EntityReference> FindContact(string fullName);

        public Task<EntityReference> CreateContact(UserProfile userProfile);

    }
}
