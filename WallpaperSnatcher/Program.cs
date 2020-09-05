using System;
using System.Text.Json;
using System.Net;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using System.Net.Http;
using System.IO;
using System.Threading.Tasks;
using System.Configuration;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;

namespace WallpaperSnatcher
{
    class Program
    {
        static string getFileType(string url)
        {
            url = url.Split('?')[0];
            url = url.Split('/').Last();
            if (url.Contains('.'))
            {
                return url.Substring(url.LastIndexOf('.'));
            }
            else
            {
                return "";
            }
        }

        static void downloadFile(string url, string fileName)
        {
            string folderLocation = ConfigurationManager.AppSettings["filePath"];
            string fileType = getFileType(url);
            if (fileType.Equals(""))
            {
                Console.WriteLine("File type not found for URL: " + url + " , Skipping post...");
            }
            else
            {
                using (WebClient client = new WebClient())
                {
                    Console.WriteLine("Saving file as: " + folderLocation + fileName + fileType + "\n");
                    client.DownloadFile(url, folderLocation + fileName + fileType);
                }
            }
        }
        static string removeInvalidChars(string fileName)
        {
            return string.Join("", fileName.Split(Path.GetInvalidFileNameChars()));
        }

        static (string url, string title) parseAPI(dynamic childrenArray, int xthVal)
        {
            if (!((bool) childrenArray[xthVal].data.is_reddit_media_domain &&
                childrenArray[xthVal].data.post_hint == "image" ))
            {
                return (null, null); // Not a supported medium or fails score check, ignore.
            } else if (childrenArray[xthVal].data.score < ConfigurationManager.AppSettings["userScore"]) {
                return (null, "lowScore");
            }
            string url = childrenArray[xthVal].data.url;
            string title = childrenArray[xthVal].data.title;
            if (url.Length == 0)
            {
                throw new System.ArgumentNullException("url is empty. Invalid target.");
            }
            title = removeInvalidChars(title);
            // Console.WriteLine(url);
            Console.WriteLine("Name:" + title);
            return (url, title);
        }

        static void setOptions(string setting) // Actually changes the configs
        {
            string tempVal = "";

            while (string.IsNullOrEmpty(tempVal))
            {
                Console.WriteLine("Please enter a value for " + setting + ": ");
                tempVal = Console.ReadLine();
                if (setting.Equals("imageCount")) // Make sure the one entry that is an int is valid
                {
                    if (!int.TryParse(tempVal, out _))
                    {
                        Console.WriteLine("Enter a valid integer for this option.");
                        tempVal = "";
                    }
                }
                try
                {
                    var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    var settings = configFile.AppSettings.Settings;
                    if (settings[setting] == null)
                    {
                        settings.Add(setting, tempVal);
                    }
                    else
                    {
                        settings[setting].Value = tempVal;
                    }
                    configFile.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
                }
                catch (ConfigurationErrorsException)
                {
                    Console.WriteLine("Error saving configuration.");
                }
            }
            // TODO: Does this actually save it to the local file??
        }

        static void checkOptions()
        {
            String[] listOfSettings = { "filePath", "webLocation", "imageCount" };
            foreach (string setting in listOfSettings) {
                if (ConfigurationManager.AppSettings[setting] == null || ConfigurationManager.AppSettings[setting].Length == 0)
                {
                    setOptions(setting);
                }
            }
        }

        static void modifyOptions() // Lets user decide what config to change if any
        {
            Console.WriteLine("\n-CHANGING OPTIONS-\n");
            foreach (string key in ConfigurationManager.AppSettings)
            {
                Console.WriteLine(key + " : " + ConfigurationManager.AppSettings[key]);
                Console.WriteLine("----------");
            }
            Console.WriteLine("\nPlease write the name of the option you would like to change: ");
            string userInput = Console.ReadLine();
            while (ConfigurationManager.AppSettings[userInput] == null)
            {
                Console.WriteLine("\"" + userInput + "\" is not present in the current list of options. Please enter a valid option: ");
                userInput = Console.ReadLine();
            }
            setOptions(userInput);
        }

        static void Main(string[] args)
        {
            string redditJson;
            dynamic childrenArray;

            checkOptions();

            Console.WriteLine("Press 1 to download images according to current settings.");
            Console.WriteLine("Press 9 to change the current options.");
            string choice = Console.ReadLine();
            if (choice.Equals("9"))
            {
                modifyOptions();
            }
            int wallpaperAmount = int.Parse(ConfigurationManager.AppSettings["imageCount"]);
            string requestedUrl = ConfigurationManager.AppSettings["webLocation"];

            using (WebClient wc = new WebClient())
            {
                redditJson = wc.DownloadString(requestedUrl);
            }
            dynamic parsedJson = Newtonsoft.Json.JsonConvert.DeserializeObject(redditJson);
            childrenArray = parsedJson.data.children; // Ditch everything else, get the array of posts info

            for (int i = 0; i < wallpaperAmount; i++) // Loop through the top x posts
            {
                var urlFilenameTuple = parseAPI(childrenArray, i);
                if (urlFilenameTuple.Item2 == "lowScore") // TODO, seems messy
                {
                    Console.WriteLine("Post has insufficient user score, skipping post...");
                }
                else if (!(urlFilenameTuple.Item1 == null || urlFilenameTuple.Item2 == null))
                {
                    downloadFile(urlFilenameTuple.Item1, urlFilenameTuple.Item2);
                }
                else
                {
                    Console.WriteLine("Unsupported media type entered, skipping post...");
                }
            }
        }
    } 
}
