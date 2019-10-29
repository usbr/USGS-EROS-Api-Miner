using System;
using System.Net;
using System.IO;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using RestSharp;
using Newtonsoft.Json;
using ICSharpCode.SharpZipLib;

namespace USGS.EROS.API
{
    class Program
    {
        // Global variables
        static string erosSrvr = @"https://earthexplorer.usgs.gov/inventory/json/v/stable/";
        static RestClient erosClient = new RestClient(erosSrvr);
        static string erosUser;     //= ConfigurationSettings.AppSettings["UserName"].ToString();
        static string erosPswd;     // = ConfigurationSettings.AppSettings["Password"].ToString();
        static string authToken, erosEntityName;
        static string erosNode = "EE";
        static string erosProduct = "STANDARD";
        static DateTime t1, t2;
        static List<string> erosDatasets;// = new List<string>() { "LANDSAT_8_C1", "LANDSAT_ETM_C1" };
        static List<int> erosPaths; //= new List<int>() { 38, 39 };
        static List<int> erosRows;  // = new List<int>() { 36, 37 };
        static List<double> lowerLeftCoordinates; //= new List<double>() { 33, -115.5 };
        static List<double> upperRightCoordinates; //= new List<double>() { 35, -113.5 };
        static bool download, inventory;

        static bool jrDEBUG = false;
        static string fileName = "";

        public static string ibr3LandsatFolder;// = @"G:\AutoDownloads\";
        static Logger logFile = new Logger();

        /// <summary>
        /// Main Program Entry Point
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] argList)
        {
            // USGS API changed security settings 7/27/2017
            // Error:  Could not create ssl tls secure channel
            // Fix: https://stackoverflow.com/questions/32994464/could-not-create-ssl-tls-secure-channel-despite-setting-servercertificatevalida
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            if (jrDEBUG)
            {
                argList = new string[10];
                // Set search period t1 to t2
                argList[0] = "--t1=" + new DateTime(2017, 5, 1).ToString();
                argList[1] = "--t2=" + DateTime.Now.ToString();
                //argList[2] = "--inventory=MyFile";
                argList[2] = @"--download=C:\Users\jrocha\Desktop\";
                argList[3] = "--user=jrocha@usbr.gov";
                argList[4] = "--pass=******";
                argList[5] = "--paths=38,39";
                argList[6] = "--rows=36,37";
                argList[7] = "--lowerleft=33,-115.5";
                argList[8] = "--upperright=35,-113.5";
                argList[9] = "--datasetnames=LANDSAT_8_C1,LANDSAT_ETM_C1";
            }

            Arguments args = new Arguments(argList);

            if (argList.Length == 0 || !args.Contains("t1") || !args.Contains("t2") || !args.Contains("user") || 
                !args.Contains("pass") || !args.Contains("paths") || !args.Contains("rows") ||
                !args.Contains("lowerleft") || !args.Contains("upperright") || !args.Contains("datasetnames"))
            {
                ShowHelp();
                return;
            }
            else
            {
                SetupDates(args, out t1, out t2);
                erosDatasets = args["datasetnames"].Split(',').ToList();
                erosUser = args["user"].ToString();
                erosPswd = args["pass"].ToString();
                erosPaths = args["paths"].Split(',').Select(Int32.Parse).ToList();
                erosRows = args["rows"].Split(',').Select(Int32.Parse).ToList();
                lowerLeftCoordinates = args["lowerleft"].Split(',').Select(double.Parse).ToList();
                upperRightCoordinates = args["upperright"].Split(',').Select(double.Parse).ToList();
                if (args.Contains("download"))
                {
                    download = true;
                    ibr3LandsatFolder = args["download"].ToString();
                }
                if (args.Contains("inventory"))
                {
                    inventory = true;
                    fileName = System.AppDomain.CurrentDomain.BaseDirectory + args["inventory"].ToString() + ".csv";
                }
            }

            var outputList = new List<string>();
            outputList.Add("Dataset,Acquisition Date,Spatial Boundaries LL1,Spatial Boundaries LL2,Spatial Boundaries UR1,Spatial Boundaries UR2,Browse URL,Metadata URL,Data Access URL,Scene ID,Modified Date,Download Code,Product Name,Is Available,FileSize");

            if (CheckErosStatus())
            {               
                // Connect to EROS service
                Connect();

                // Loop through datasets and scenes
                #region
                foreach (var dataset in erosDatasets)
                {
                    var scenesFound = SceneSearch(dataset);
                    var sceneResults = scenesFound.data.results;
                    // Loop through each Scene
                    foreach (var scene in sceneResults)
                    {
                        erosEntityName = scene.entityId;
                        // Check if the we need this Path-Row 
                        if (CheckEntityPathRow(erosEntityName))
                        {
                            var downloadList = DownloadSearch(dataset, erosEntityName);
                            // Loop through the list of available download options
                            foreach (var optionList in downloadList.data)
                            {
                                // Loop through each download option
                                foreach (var option in optionList.downloadOptions)
                                {
                                    // Get the 'STANDARD' download option - this is the GeoTIFF product
                                    if (option.downloadCode.ToString() == erosProduct)
                                    {
                                        Console.WriteLine("Processing " + erosEntityName);
                                        var outputRow = "";
                                        outputRow += dataset + ",";
                                        outputRow += scene.acquisitionDate + ",";
                                        outputRow += scene.sceneBounds + ",";
                                        outputRow += scene.browseUrl + ",";
                                        outputRow += scene.metadataUrl + ",";
                                        outputRow += scene.dataAccessUrl + ",";
                                        outputRow += scene.entityId + ",";
                                        outputRow += scene.modifiedDate + ",";
                                        outputRow += option.downloadCode + ",";
                                        outputRow += option.productName + ",";
                                        outputRow += option.available + ",";
                                        outputRow += option.filesize + ",";
                                        // Check if download is available
                                        if (Convert.ToBoolean(option.available))
                                        {
                                            if (download && !Logger.isSceneDownloaded(erosEntityName))
                                            {
                                                // Download file
                                                var downloadResult = Download(dataset, erosEntityName);
                                                outputRow += downloadResult.data[0];
                                                var dloadFile = ibr3LandsatFolder + erosEntityName + ".tar.gz";
                                                using (var client = new System.Net.WebClient())
                                                {
                                                    client.DownloadFile(downloadResult.data[0], dloadFile);
                                                    logFile.Log(" OK " + erosEntityName + " downloaded");
                                                }
                                                // Unpack *.tar.gz file
                                                UnpackDownload(erosEntityName, dloadFile);
                                            }
                                        }
                                        else
                                        {
                                            //outputRow += "N/A";
                                        }
                                        outputList.Add(outputRow);
                                        ClearOrders(dataset);
                                    }
                                }
                            }
                        }
                    }
                }
                #endregion

                // Disconnect from EROS service
                Disconnect();

                // Write Output file
                if (inventory)
                {
                    StreamWriter file = new System.IO.StreamWriter(fileName);
                    outputList.ForEach(file.WriteLine);
                    file.Close();
                }               

            }
        }


        /// <summary>
        /// Check EROS service status
        /// </summary>
        private static bool CheckErosStatus()
        {
            // Define API process request
            var request = new RestRequest("status?jsonRequest={}",
                            Method.GET);

            // Execute the request and get service status
            IRestResponse apiResponse = erosClient.Execute(request);
            if (apiResponse.StatusCode == System.Net.HttpStatusCode.OK)
            { return true; }
            else
            { return false; }
        }


        /// <summary>
        /// Connect to EROS service
        /// </summary>
        private static void Connect()
        {
            // Define API process request
            //var request = new RestRequest("login?jsonRequest={" +
            //                @"""catalogId"":""EE""," +
            //                @"""username"":""" + erosUser + @"""," +
            //                @"""password"":""" + erosPswd + @"""}", 
            //                Method.POST);

            var request = new RestRequest("login?", Method.POST);
            string requestString = "{" +
                            @"""catalogId"":""EE""," +
                            @"""username"":""" + erosUser + @"""," +
                            @"""password"":""" + erosPswd + @"""}";
            request.AddParameter("jsonRequest", requestString);

            // Execute the request and get authentication token
            IRestResponse apiResponse = erosClient.Execute(request);
            var response = JsonConvert.DeserializeObject<ErosApiConnectionResponse>(apiResponse.Content);
            authToken = response.data; 
        }


        /// <summary>
        /// Search for available Scenes given datasetName and dates (t1 & t2)
        /// </summary>
        /// <param name="erosDatasetName"></param>
        /// <returns></returns>
        private static ErosApiSceneSearchResponse SceneSearch(string erosDatasetName)
        {
            /*  Working template
             *  https://earthexplorer.usgs.gov/inventory/json/search?jsonRequest={ 
             *              "datasetName":"LANDSAT_8",
             *              "lowerLeft":{ "latitude":"33","longitude":"-115.5"},
             *              "upperRight":{ "latitude":"35","longitude":"-113.5"},
             *              "startDate":"2017-01-01",
             *              "endDate":"2017-01-10",
             *              "includeUnknownCloudCover":true,
             *              "maxResults":"500",
             *              "sortOrder":"ASC",
             *              "apiKey":"XXXXXXXXXX",
             *              "node":"EE"}
             */

            // Define API process request
            var request = new RestRequest("search?jsonRequest={" +
                            @"""datasetName"":""" + erosDatasetName + @"""," +
                            @"""lowerLeft"":{ ""latitude"":""" + lowerLeftCoordinates[0] + @""",""longitude"":""" + lowerLeftCoordinates[1] + @"""}," +
                            @"""upperRight"":{ ""latitude"":""" + upperRightCoordinates[0] + @""",""longitude"":""" + upperRightCoordinates[1] + @"""}," +
                            @"""startDate"":""" + t1.ToString("yyyy-MM-dd") + @"""," +
                            @"""endDate"":""" + t2.ToString("yyyy-MM-dd") + @"""," +
                            @"""includeUnknownCloudCover"":true," +
                            @"""maxResults"":""9999""," +
                            @"""sortOrder"":""ASC""," +
                            @"""apiKey"":""" + authToken + @"""," +
                            @"""node"":""" + erosNode + @"""" +
                            "}",
                            Method.GET);

            // Execute the request and get Scenes
            IRestResponse apiResponse = erosClient.Execute(request);
            return JsonConvert.DeserializeObject<ErosApiSceneSearchResponse>(apiResponse.Content);
        }


        /// <summary>
        /// Search for available download given datasetName and erosEntityName
        /// </summary>
        /// <param name="datasetName"></param>
        /// <param name="entityName"></param>
        /// <returns></returns>
        private static ErosApiDownloadOptionsResponse DownloadSearch(string erosDatasetName, string erosEntityName)
        {
            /* Working template
             * https://earthexplorer.usgs.gov/inventory/json/downloadoptions?jsonRequest={
             *              "datasetName":"LANDSAT_8",
             *              "entityIds":["LT81372072017001LGN00"],
             *              "machineOnly":true,
             *              "apiKey":"XXXXXXXXXX",
             *              "node":"EE"}
             */

            // Define API process request
            var request = new RestRequest("downloadoptions?jsonRequest={" +
                            @"""datasetName"":""" + erosDatasetName + @"""," +
                            @"""entityIds"":[""" + erosEntityName + @"""]," +
                            @"""machineOnly"":true," +
                            @"""apiKey"":""" + authToken + @"""," +
                            @"""node"":""" + erosNode + @"""" +
                            "}",
                            Method.GET);

            // Execute the request and get Scenes
            IRestResponse apiResponse = erosClient.Execute(request);
            return JsonConvert.DeserializeObject<ErosApiDownloadOptionsResponse>(apiResponse.Content);
        }


        /// <summary>
        /// Get Download Information
        /// </summary>
        /// <param name="erosDatasetName"></param>
        /// <param name="erosEntityName"></param>
        /// <returns></returns>
        private static ErosApiDownloadResponse Download(string erosDatasetName, string erosEntityName)
        {
            /* Working template
             * https://earthexplorer.usgs.gov/inventory/json/download?jsonRequest={
             *              "datasetName":"LANDSAT_8",
             *              "products":["STANDARD"],
             *              "entityIds":["LT81372072017001LGN00"],
             *              "apiKey":"XXXXXXXXXX",
             *              "node":"EE"}
             */

            // Define API process request
            var request = new RestRequest("download?jsonRequest={" +
                            @"""datasetName"":""" + erosDatasetName + @"""," +
                            @"""products"":[""" + erosProduct + @"""]," +
                            @"""entityIds"":[""" + erosEntityName + @"""]," +
                            @"""apiKey"":""" + authToken + @"""," +
                            @"""node"":""" + erosNode + @"""" +
                            "}",
                            Method.GET);

            // Execute the request and get Scenes
            IRestResponse apiResponse = erosClient.Execute(request);
            return JsonConvert.DeserializeObject<ErosApiDownloadResponse>(apiResponse.Content);
        }
        

        /// <summary>
        /// Unpacks *.tar.gz downloaded file
        /// </summary>
        /// <param name="erosEntityName"></param>
        private static void UnpackDownload(string erosEntityName, string downloadedFile)
        {
            FileInfo tarFileInfo = new FileInfo(downloadedFile);
            DirectoryInfo targetDirectory = new DirectoryInfo(ibr3LandsatFolder + erosEntityName);
            if (!targetDirectory.Exists)
            { targetDirectory.Create(); }
            using (Stream sourceStream = new ICSharpCode.SharpZipLib.GZip.GZipInputStream(tarFileInfo.OpenRead()))
            {
                using (ICSharpCode.SharpZipLib.Tar.TarArchive tarArchive = ICSharpCode.SharpZipLib.Tar.TarArchive.CreateInputTarArchive(sourceStream, 
                    ICSharpCode.SharpZipLib.Tar.TarBuffer.DefaultBlockFactor))
                {
                    tarArchive.ExtractContents(targetDirectory.FullName);
                }
            }
        }


        /// <summary>
        /// Clear Orders
        /// </summary>
        /// <param name="erosDatasetName"></param>
        private static void ClearOrders(string erosDatasetName)
        {
            /* Working template
             * https://earthexplorer.usgs.gov/inventory/json/clearbulkdownloadorder?jsonRequest={
             *              "datasetName":"LANDSAT_8",
             *              "apiKey":"XXXXXXXXXX",
             *              "node":"EE"}
             * 
             * https://earthexplorer.usgs.gov/inventory/json/clearorder?jsonRequest={
             *              "datasetName":"LANDSAT_8",
             *              "apiKey":"XXXXXXXXXX",
             *              "node":"EE"}                                                       
             */

            // Define API process request
            var requestBulk = new RestRequest("clearbulkdownloadorder?jsonRequest={" +
                            @"""datasetName"":""" + erosDatasetName + @"""," +
                            @"""apiKey"":""" + authToken + @"""," +
                            @"""node"":""" + erosNode + @"""" +
                            "}",
                            Method.GET);
            var requestSingle = new RestRequest("clearorder?jsonRequest={" +
                            @"""datasetName"":""" + erosDatasetName + @"""," +
                            @"""apiKey"":""" + authToken + @"""," +
                            @"""node"":""" + erosNode + @"""" +
                            "}",
                            Method.GET);

            // Execute the request 
            IRestResponse apiResponse = erosClient.Execute(requestBulk);
            apiResponse = erosClient.Execute(requestSingle);

        }


        /// <summary>
        /// Disconnect from EROS service
        /// </summary>
        private static void Disconnect()
        {
            // Define API process request
            var request = new RestRequest("logout?jsonRequest={" +
                            @"""apiKey"":""" + authToken + @"""}",
                            Method.GET);

            // Execute the request and nullify authentication token
            IRestResponse apiResponse = erosClient.Execute(request);
            var response = JsonConvert.DeserializeObject<ErosApiConnectionResponse>(apiResponse.Content);
            authToken = null; // raw content as string
        }
        

        /// <summary>
        /// Check if the entity is in our list of needed Paths and Rows
        /// </summary>
        /// <returns></returns>
        private static bool CheckEntityPathRow(string erosEntityName)
        {
            /* Working template
             *  "entityId": "LE71382072016366EDC00"      // LANDSAT7
             *  "entityId": "LC80380362016009LGN00"      // LANDSAT8
             */

            bool entityIsNeeded = false;
            if (erosEntityName.Length > 9)
            {
                var path = Convert.ToInt16(erosEntityName.Substring(3, 3));
                var row = Convert.ToInt16(erosEntityName.Substring(6, 3));
                if (erosPaths.Contains(path) && erosRows.Contains(row))
                {
                    entityIsNeeded = true;
                }
            }
            return entityIsNeeded;
        }


        /// <summary>
        /// Help for command line program
        /// </summary>
        static void ShowHelp()
        {
            Console.WriteLine("USGS EROS API Miner");
            Console.WriteLine();
            Console.WriteLine("--user=[X]");
            Console.WriteLine("      with [X] as the user's username");
            Console.WriteLine("--pass=[X]");
            Console.WriteLine("      with [X] as the user's password");
            Console.WriteLine("--datasetnames=[X1,X2,...Xn]");
            Console.WriteLine("      Valid EROS API dataset names from ");
            Console.WriteLine(@"      API/json/datasets?jsonRequest={""apiKey"": ""X"",""node"": ""EE""}");
            Console.WriteLine("--download");
            Console.WriteLine("      downloads found files automatically");
            Console.WriteLine("--inventory=[X]");
            Console.WriteLine("      prints found files in a csv file named [X]");
            Console.WriteLine("--lowerleft=[X1,X2]");
            Console.WriteLine("      lower left spatial coordinates of search boundaries in decimal degrees");
            Console.WriteLine("--upperright=[X1,X2]");
            Console.WriteLine("      upper right spatial coordinates of search boundaries in decimal degrees");
            Console.WriteLine("--paths=[X1,X2,...Xn]");
            Console.WriteLine("      Scene Paths to search for");
            Console.WriteLine("--rows=[X1,X2,...Xn]");
            Console.WriteLine("      Scene Rows to search for");
            Console.WriteLine("--t1=[X]");
            Console.WriteLine("      with [X] as a valid date in YYYY-MM-DD format");
            Console.WriteLine("      or today, yesterday, lastweek, or lastmonth");
            Console.WriteLine("--t2=[X]");
            Console.WriteLine("      with [X] as a valid date in YYYY-MM-DD format");
            Console.WriteLine("      or either today or yesterday and t1 < t2");
            Console.WriteLine();
            Console.WriteLine("Sample Usage:");
            Console.WriteLine(@"ErosMiner --user=jrocha@usbr.gov --pass=XXXXX --datasetnames=LANDSAT_8_C1,LANDSAT_ETM_C1 --download=G:\AutoDownloads\ --lowerleft=33,-115.5 --upperright=35-113.5 --paths=38,39 --rows=36,37 --t1=2016-01-01 --t2=yesterday");
            Console.WriteLine("");
            Console.WriteLine("Press any key to continue... ");
            Console.ReadLine();
        }


        /// <summary>
        /// Formats the input dates to the program
        /// </summary>
        /// <param name="args"></param>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        private static void SetupDates(Arguments args, out DateTime t1, out DateTime t2)
        {
            t1 = DateTime.Now.Date.AddDays(-1);
            t2 = DateTime.Now.Date.AddDays(-1);

            if (args.Contains("t1"))
            {
                try
                { t1 = DateTime.Parse(args["t1"]); }
                catch
                {
                    if (args["t1"].ToString().ToLower() == "today")
                    { t1 = DateTime.Now.Date.AddDays(0); }
                    else if (args["t1"].ToString().ToLower() == "yesterday")
                    { t1 = DateTime.Now.Date.AddDays(-1); }
                    else if (args["t1"].ToString().ToLower() == "lastweek")
                    { t1 = DateTime.Now.Date.AddDays(-7); }
                    else if (args["t1"].ToString().ToLower() == "lastmonth")
                    { t1 = DateTime.Now.Date.AddDays(-30); }
                    else
                    { t1 = DateTime.Now.Date.AddDays(-1); }
                }
            }
            if (args.Contains("t2"))
            {
                try
                { t2 = DateTime.Parse(args["t2"]); }
                catch
                {
                    if (args["t2"].ToString().ToLower() == "today")
                    { t2 = DateTime.Now.Date.AddDays(0); }
                    else if (args["t2"].ToString().ToLower() == "yesterday")
                    { t2 = DateTime.Now.Date.AddDays(-1); }
                    else
                    { t2 = DateTime.Now.Date.AddDays(0); }
                }
            }

            if (t1 > t2)
            {
                var tTemp = t2;
                t2 = t1;
                t1 = tTemp;
            }
        }



    }

    
}
