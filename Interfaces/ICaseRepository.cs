using Microsoft.Xrm.Sdk;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vij.Bots.DynamicsCRMBot.Models;

namespace Vij.Bots.DynamicsCRMBot.Interfaces
{
    public interface ICaseRepository
    {
        public  Task<Entity> CreateCase(Entity entity);
        public  Task<List<Subject>> GetSubjects();
    }
}
