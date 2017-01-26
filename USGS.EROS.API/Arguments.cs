using System;
using System.IO;
using System.Collections;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace USGS.EROS.API
{

    /// <summary>
    ///  command line Argument manager
    ///  modified from:
    ///  http://www.codeproject.com/Articles/3111/C-NET-Command-Line-Arguments-Parser
    ///  
    /// Use like this:
    /// 
    /// var args = new Argumnets(argList);
    /// if( args.Contains("myArg"))  //  --myArg=12
    ///      {
    ///        var argVal = args["myArg"]; // 12
    ///      }
    /// 
    /// </summary>
    public class Arguments
    {
        // Variables
        private StringDictionary prefixedParams = new StringDictionary();
        //private ArrayList anonParams = new ArrayList();
        List<string> anonParams = new List<string>();

        // Constructors
        public Arguments(string[] args)
        {
            Regex spliter = new Regex(@"^-*(?<name>[\w\-]+)([:=])?(?<value>.+)?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            //char[] trimChars = { '"', '\'' };
            char[] trimChars = { '"' }; //, '\'' };
            Match part;

            foreach (string arg in args)
            {
                part = spliter.Match(arg);
                if (part.Success)
                {
                    string sval = part.Groups["value"].Value;
                    sval = sval.Trim(trimChars);

                    prefixedParams[part.Groups["name"].Value] = sval;
                }
                else
                    anonParams.Add(arg);
            }
        }

        public int Count
        {
            get { return prefixedParams.Count; }
        }

        public bool Contains(string name)
        {
            return (prefixedParams[name] != null);
        }


        // Retrieve a parameter value if it exists
        public string this[string param]
        {
            get
            {
                if (!Contains(param))
                    return "";

                return (prefixedParams[param]);
            }
        }
        //public int Count {

        public string this[int i]
        {
            get
            {
                return anonParams.Count > i ? (string)anonParams[i] : null;
            }
        }

        public string[] AnonParams
        {
            get
            {
                return anonParams.ToArray();
            }
        }
    }


    /// <summary>
    /// C# Class for logging capabilities
    /// </summary>
    public class Logger
    {
        public void Log(string message)
        {
            using (StreamWriter w = File.AppendText(Program.ibr3LandsatFolder + @"downloadLog.txt"))
            {
                w.WriteLine("[{0}] {1} ", String.Format("{0:yyyyMMdd HH:mm:ss}", DateTime.Now), message);
                w.Flush();
            }
        }

        public static bool isSceneDownloaded(string sceneName)
        {
            var logFile = Program.ibr3LandsatFolder + @"downloadLog.txt";
            if (File.Exists(logFile))
            {
                if (File.ReadAllText(logFile).Contains(sceneName))
                { return true; }
                else
                { return false; }
            }
            else
            {
                return false;
            }
        }
    }
}
