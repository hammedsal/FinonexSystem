
using System.Net.Http.Json;
using System;
using System.Threading.Tasks;

namespace FinonexClient
{
    public class Program
    {
        private static readonly string ClientFileName = "client_events.jsonl";
        private static readonly string postEventUri = "http://localhost:8000/Events/liveEvent";

        static async Task Main(string[] args)
        {
            await SendEventsToServer();
        }

        // loop on the file lines and call the server liveEvent uri
        private static async Task SendEventsToServer()
        {
            int totalEvents = 0, failedEvents = 0;
            string[] lines = ReadFileLines(ClientFileName);
            if (lines.Length == 0)
            {
                return;
            }

            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", "secret");
                //httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("secret");

                foreach (var line in lines)
                {
                    var json = new StringContent(line, System.Text.Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await httpClient.PostAsync(postEventUri, json);

                    totalEvents++;
                    // display an error
                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        failedEvents++;
                        Console.WriteLine("FAILED: " + line);
                    }
                } 
            }

            Console.WriteLine($"Failed Events: {failedEvents} of Total Sent Events {totalEvents}.");

            // mark file as "done" after reading its content
            MarkFileAsDone(ClientFileName);
        }

        private static string[] ReadFileLines(string filePath)
        {
            string[] lines = new string[0];

            try
            {
                if (!System.IO.File.Exists(filePath))
                {
                    using (System.IO.File.Create(filePath))
                    {
                    }
                }

                lines = File.ReadAllLines(filePath);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to read the events from file : " + filePath, ex);
            }
            return lines;
        }

        private static void MarkFileAsDone(string filePath)
        {
            try
            {
                File.Move(filePath, filePath + ".Done");
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to rename file : " + filePath, ex);
            }
        }
    }
}
