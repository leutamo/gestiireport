using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CsvHelper;
using Newtonsoft.Json.Linq;
using Polly;

namespace Gestii
{
    internal class GestiiClient
    {
        public static int DownloadReports(int lastId)
        {
            Program.logger.Info("Descargando reportes {0}", lastId);

            if (Properties.Settings.Default.client == "" || Properties.Settings.Default.apikey == "")
            {
                Program.logger.Error("client and apikey are required");
                return lastId;
            } 

            string date = DateTime.Now.AddDays(1).ToString("yyMMdd000000");

            string baseUrl = "https://" + Properties.Settings.Default.client + ".gestii.com/api/v1/";            
            string tasksUrl = baseUrl+"tasks/?apikey=" + Properties.Settings.Default.apikey + "&limit=500&created_at=" + date + "-5d";

            DirectoryInfo baseDir = Directory.CreateDirectory(Properties.Settings.Default.directory);
            string downloadedDir = baseDir.CreateSubdirectory("downloaded").FullName;
            string correctDir = baseDir.CreateSubdirectory("processed").FullName;


            JToken json = GetJsonAsync(tasksUrl).Result;
          

            WebClient myWebClient = new WebClient();
            foreach (JToken report in json)
            {

                int current = report["id"].Value<int>();
                string caption = report["caption"].Value<string>();
                string Extension = Path.GetExtension(caption);
                string type = report["type"].Value<string>();

                if (current > lastId && type == "report" && caption.StartsWith("auto_"))
                {
                    string reportUrl = baseUrl + "cdn/reports/" + current + "?apikey=" + Properties.Settings.Default.apikey;
                    
                    string download_file = downloadedDir + "\\" + caption;
                    string process_file = correctDir + "\\" + caption;

                    if (!File.Exists(download_file) && !File.Exists(process_file) && DownloadFile(myWebClient, reportUrl, download_file))
                    {
                        lastId = current;
                    }                   
                }
            }

            return lastId;
        }

        private static async Task<JToken> GetJsonAsync(string url)
        {
            using (var client = new HttpClient())
            {
                var policy = Policy.Handle<HttpRequestException>().WaitAndRetryAsync(10, retryAttempt => TimeSpan.FromSeconds(300));
                var response = await policy.ExecuteAsync(() => client.GetAsync(url));

                JToken json;

                try
                {
                    string content = await response.Content.ReadAsStringAsync();

                    json = JToken.Parse(content);
                }
                catch (WebException ex)
                {
                    int statusCode = (int)((HttpWebResponse)ex.Response).StatusCode;
                    string res= ((HttpWebResponse)ex.Response).ToString();
                    json = JToken.Parse("{}");
                    json["content"] = res;
                    json["status_code"] = statusCode;
                    Program.logger.Error(res);
                }

                return json;

            }
        }

        private static Boolean DownloadFile(WebClient wc, string url, string filename)
        {                       
            try
            {
                Program.logger.Info("Descargando {0}", filename);
                wc.DownloadFile(url, filename);
            } 
            catch (WebException ex)
            {
                Program.logger.Error("Error al descargar {0}: {1}", filename, ex.Message);
                return false;
            }

            Program.logger.Info("{0} descargado correctamente", filename);                
            
            return true;
        }
    }
}