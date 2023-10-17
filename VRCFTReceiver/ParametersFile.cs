using System;
using System.Net.Http;
using System.IO;
using System.Threading.Tasks;

namespace VRCFTReceiver
{
    internal class ParametersFile
    {
        public static async Task Create()
        {
            string url = "https://raw.githubusercontent.com/hazre/VRCFTReceiver/main/static/vrc_parameters.json";

            string savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow", @"VRChat\VRChat\OSC\vrcft\Avatars\vrc_parameters.json");


            if (File.Exists(savePath)) {
                Loader.Msg("JSON file already exists.");
                return;
            };

            // Check if the directory exists, if not, create it
            string directory = Path.GetDirectoryName(savePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Download and save the JSON file
            using (HttpClient httpClient = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await httpClient.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        string jsonContent = await response.Content.ReadAsStringAsync();
                        File.WriteAllText(savePath, jsonContent);
                        Loader.Msg("JSON file downloaded and saved successfully.");
                    }
                    else
                    {
                        Loader.Warn("Failed to download JSON file. HTTP status code: " + response.StatusCode);
                    }
                }
                catch (Exception e)
                {
                    Loader.Error("An error occurred: " + e.Message);
                }
            }
        }
    }
}
