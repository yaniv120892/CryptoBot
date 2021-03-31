using System.Collections.Generic;
using System.Threading.Tasks;
using Common;

namespace Storage.Abstractions.Repository
{
    public interface IBotResultDetailsRepository
    {
        public Task AddAsync(BotResultDetails botResultDetails);
        public Task<List<BotResultDetails>> GetAllAsync();
    }
}