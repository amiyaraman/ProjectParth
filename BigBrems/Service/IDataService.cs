using System.Collections.Generic;
using BigBrems.Models;

namespace BigBrems.Services
{
    public interface IDataService
    {
        // 1. Returns a list of all test runs (e.g., "DS_101", "DS_102")
        List<Dataset> GetDatasets();

        // 2. Returns the sensors available for a specific test run
        //    (Now includes the 'Unit' property inside the Channel object)
        List<Channel> GetChannels(string datasetId);

        // 3. Returns the actual rows of data for the dashboard charts/grids
        List<MeasurementData> GetData(string datasetId, string channelId);
    }
}