using System;
using System.Text.Json;
using System.Net;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using System.Net.Http;
using System.IO;
using System.Threading.Tasks;

namespace WallpaperSnatcher
{
    class Program
    {
        static string getFileType(string URL)
        {
            return URL.Substring(URL.Length - 4);
        }

        static void downloadFile(string URL, string fileName)
        {
            string folderLocation = @"D:\User Folders\Desktop\TestingFolder\";
            string fileType = getFileType(URL);
            using (WebClient client = new WebClient())
            {
                Console.WriteLine("Saving file as: " + folderLocation + fileName + fileType);
                client.DownloadFile(URL, folderLocation + fileName + fileType);
            }
        }

        static (string URL, string title) parseAPI(dynamic childrenArray, int xthVal)
        {
            string URL = childrenArray[xthVal].data.url;
            string title = childrenArray[xthVal].data.title;
            if (URL.Length == 0)
            {
                throw new System.ArgumentNullException("URL is empty. Invalid target.");
            }
            Console.WriteLine(URL);
            Console.WriteLine(title);
            return (URL, title);
        }

        static void Main(string[] args)
        {
            string redditJson;
            dynamic childrenArray;
            int wallpaperAmount = 10;
            string requestedUrl = "https://www.reddit.com/r/wallpapers/.json";
            using (WebClient wc = new WebClient())
            {
                redditJson = wc.DownloadString(requestedUrl);
            }
            dynamic parsedJson = Newtonsoft.Json.JsonConvert.DeserializeObject(redditJson);


            childrenArray = parsedJson.data.children;
            Console.WriteLine(childrenArray[0].data.url);


            for (int i = 0; i < wallpaperAmount; i++)
            {
                var URLFilenameTuple = parseAPI(childrenArray, i);
                downloadFile(URLFilenameTuple.Item1, URLFilenameTuple.Item2);
            }

            // Immediate todo:
            // ([<>:"|?*\/\\]) block out illegal characters for windows, and maybe illegal names


            /* General structure of code:
             *  1. A section that requests the user of what options they want. Defaults are necessary.
             *  2. The desired subreddits to scan. This can be included in 1.
             *  3. Do this in bulk. Progress bar? Some sort of waiting indicator.
             *  4. For now, save all locally as per the options. 
             *  4a. Otherwise, in the future, save thumbnails and let user tick off what they want.
             *  5. Save and exit.
             */

        }
    } 
}
