using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vij.Bots.DynamicsCRMBot.Models;

namespace Vij.Bots.DynamicsCRMBot.Interfaces
{
    public interface ISubjectRepository
    {


        public Task<List<Subject>> GetSubjects();

    }
}
