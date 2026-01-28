using System.Collections.Generic;
using BigBrems.Models;
using System.Collections.Generic;
using System.Threading.Tasks; // Needed for async/await

namespace BigBrems.Services
{
    public interface IDataService
    {
        // 1. Silent Login (Returns true if successful)
        Task<bool> AuthenticateAsync();

        // 2. New Top Level: Data Pools
        Task<List<DataPool>> GetDataPoolsAsync();

        // 3. Get Datasets (Now depends on which Pool is selected)
        Task<List<Dataset>> GetDatasetsAsync(string poolId);

        // 4. Get Channels (Depends on Dataset)
        Task<List<Channel>> GetChannelsAsync(string datasetId);

        // 5. Get Data
        Task<List<MeasurementData>> GetDataAsync(string datasetId, string channelId);
    }
}