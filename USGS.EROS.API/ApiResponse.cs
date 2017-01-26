using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USGS.EROS.API
{
    /*******************************************************************************************  
     *  This code file contains the hard-coded classes for the JSON responses that the 
     *  API service generates. Classes are defined based on the API responses at this link: 
     *  https://earthexplorer.usgs.gov/inventory/documentation/json-api#download
     *******************************************************************************************
     */

    /// <summary>
    /// Container Class for EROS API Connection Response
    /// </summary>
    public class ErosApiConnectionResponse
    {
        public string errorCode { get; set; }
        public string data { get; set; }
        public string api_version { get; set; }
        public string executionTime { get; set; }
    }

    /// <summary>
    /// Container Class for EROS API Scene Search Response - TIER 1 - Main
    /// </summary>
    public class ErosApiSceneSearchResponse
    {
        public string errorCode { get; set; }
        public string error { get; set; }
        public ErosApiSearchResponseMetadata data { get; set; }             // NESTED OBJECTS {} ARE DESERIALIZED BY THEIR CLASS
        public string api_version { get; set; }
        public string executionTime { get; set; }
    }

    /// <summary>
    /// Container Class for EROS API Scene Search Response - TIER 2 - Metadata
    /// </summary>
    public class ErosApiSearchResponseMetadata
    {
        public string numberReturned { get; set; }
        public string totalHits { get; set; }
        public string firstRecord { get; set; }
        public string lastRecord { get; set; }
        public string nextRecord { get; set; }
        public List<ErosApiSearchResponseResults> results { get; set; }     // NESTED ARRAYS [] ARE DESERIALIZED BY A LIST OF THEIR CLASS
    }

    /// <summary>
    /// Container Class for EROS API Scene Search Response - TIER 3 - Results
    /// </summary>
    public class ErosApiSearchResponseResults
    {
        public string acquisitionDate { get; set; }
        public string startTime { get; set; }
        public string endTime { get; set; }
        public ErosApiCoordinates lowerLeftCoordinate { get; set; }
        public ErosApiCoordinates upperLeftCoordinate { get; set; }
        public ErosApiCoordinates upperRightCoordinate { get; set; }
        public ErosApiCoordinates lowerRightCoordinate { get; set; }
        public string sceneBounds { get; set; }
        public string browseUrl { get; set; }
        public string dataAccessUrl { get; set; }
        public string downloadUrl { get; set; }
        public string entityId { get; set; }
        public string displayId { get; set; }
        public string metadataUrl { get; set; }
        public string fgdcMetadataUrl { get; set; }
        public string modifiedDate { get; set; }
        public string orderUrl { get; set; }
        public string bulkOrdered { get; set; }
        public string ordered { get; set; }
        public string summary { get; set; }
    }

    /// <summary>
    /// Container Class for EROS API Scene Search Response - TIER 4 - Coordinates
    /// </summary>
    public class ErosApiCoordinates
    {
        public string latitude { get; set; }
        public string longitude { get; set; }
    }

    /// <summary>
    /// Container Class for EROS API Download Options Response - TIER 1 - Main
    /// </summary>
    public class ErosApiDownloadOptionsResponse
    {
        public string errorCode { get; set; }
        public string error { get; set; }
        public List<ErosApiDownloadOptionsResponseMetadata> data { get; set; }
        public string api_version { get; set; }
        public string executionTime { get; set; }
    }

    /// <summary>
    /// Container Class for EROS API Download Options Response - TIER 2 - Metadata
    /// </summary>
    public class ErosApiDownloadOptionsResponseMetadata
    {
        public List<ErosApiDownloadOptionsResponseResults> downloadOptions { get; set; }
        public string entityId { get; set; }
    }

    /// <summary>
    /// Container Class for EROS API Download Options Response - TIER 3 - Results
    /// </summary>
    public class ErosApiDownloadOptionsResponseResults
    {
        public string available { get; set; }
        public string downloadCode { get; set; }
        public string filesize { get; set; }
        public string productName { get; set; }
        public string url { get; set; }
        public string storageLocation { get; set; }
    }

    /// <summary>
    /// Container Class for EROS API Download Response - TIER 1 - Main
    /// </summary>
    public class ErosApiDownloadResponse
    {
        public string errorCode { get; set; }
        public string error { get; set; }
        public List<string> data { get; set; }
        public string api_version { get; set; }
        public string executionTime { get; set; }
    }
}
