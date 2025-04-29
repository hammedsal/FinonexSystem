using Npgsql;

using System.Text.Json;

namespace FinonexDataProcessor
{
    public class Program
    {
        private static readonly string connectionString = "Host=localhost;Port=5432;Username=postgres;Password=hs2181;Database=finodb";
        
        private static readonly string postEventUri = "http://localhost:8000/Events/liveEvent";
        private static readonly string DefaultFileName = "server_events.jsonl";
        
        private static readonly string REVENUE_PLUS = "add_revenue";
        private static readonly string REVENUE_MINUS = "subtract_revenue";

        private static readonly SemaphoreSlim semaphoreReadServerFile = new SemaphoreSlim(1, 1);

        static async Task Main(string[] args)
        {
            Console.WriteLine("Enter the file name (or empty for default 'server_events.jsonl'):");
            string serverFileName = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(serverFileName))
            {
                serverFileName = DefaultFileName;
            }

            Console.WriteLine("Processing option ('1' per user events, '2' per single events):");
            string option = Console.ReadLine();

            if (option != "1" && option != "2")
            {
                Console.WriteLine("Invalid option");
                return;
            }

            List<string> lines = new List<string>();
            await foreach (var line in ReadFileLinesAsync(serverFileName))
            {
                if (line != null)
                {
                    lines.Add(line);
                }

                if (lines.Count >= 1000)
                {
                    // convert to dictionary of users
                    List<EventData> events = DeserializeLinesToEvents(lines);

                    // process and ssave to db
                    await ProccessData(option, events);

                    // empty already proccessed lines
                    lines = new List<string>();
                }
            }

            // handle the rest
            if (lines.Count > 0)
            {
                // convert to dictionary of users
                List<EventData> events = DeserializeLinesToEvents(lines);

                // process and ssave to db
                await ProccessData(option, events);
            }

            Console.WriteLine("Done");
        }

        private static List<EventData> ProcessRevenuePerUser(List<EventData> events)
        {
            Dictionary<string, int> userEvents = new Dictionary<string, int>();

            foreach (var evt in events)
            {
                if (!userEvents.ContainsKey(evt.UserId))
                {
                    userEvents.Add(evt.UserId, 0);
                }

                userEvents[evt.UserId] += (evt.Name == REVENUE_PLUS ? evt.Value : -evt.Value);
            }

            // convert dictionary to a list of EventData
            List<EventData> sumEvents = userEvents
                .Select(usrkey => new EventData
                {
                    UserId = usrkey.Key,
                    Name = usrkey.Value >= 0 ? REVENUE_PLUS : REVENUE_MINUS,
                    Value = usrkey.Value
                })
                .ToList();

            return sumEvents;
        }

        private static async Task ProccessData(string option, List<EventData> events)
        {
            if (option == "1")
            {
                // summarize first
                List<EventData> sumEvents = ProcessRevenuePerUser(events);
                await SaveEventsToDatabase(sumEvents);
            }
            else
            {
                await SaveEventsToDatabase(events);
            }
        }

        private static List<EventData> DeserializeLinesToEvents(List<string> lines)
        {
            List<EventData> events = new List<EventData>();

            foreach (var line in lines)
            {
                try
                {
                    var evt = JsonSerializer.Deserialize<EventData>(line);
                    if (evt != null)
                    {
                        events.Add(evt);
                    }                }
                catch (Exception)
                {
                    // handle corrupted events
                    throw;
                }
            }

            return events;
        }

        public static async IAsyncEnumerable<string> ReadFileLinesAsync(string serverFileName)
        {
            await semaphoreReadServerFile.WaitAsync();
            try
            {
                if (string.IsNullOrEmpty(serverFileName))
                {
                    serverFileName = DefaultFileName;
                }

                if (!File.Exists(serverFileName))
                {
                    yield break;
                }

                using var stream = File.OpenRead(serverFileName);
                using var reader = new StreamReader(stream);
                while (!reader.EndOfStream)
                {
                    yield return await reader.ReadLineAsync();
                }
            }
            finally
            {
                semaphoreReadServerFile.Release();
            }
        }

        public static async Task<List<string>> ReadFileLinesAsync1(string serverFileName)
        {
            var eventLines = new List<string>();

            await semaphoreReadServerFile.WaitAsync(); // locking file
            try
            {
                if (string.IsNullOrEmpty(serverFileName))
                {
                    serverFileName = DefaultFileName;
                }

                if (!File.Exists(serverFileName))
                {
                    return new List<string>();
                }

                using (var stream = File.OpenRead(serverFileName))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        while (!reader.EndOfStream)
                        {
                            eventLines.Add(await reader.ReadLineAsync());
                        }
                    }
                }

                return eventLines;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to read file: " + serverFileName);
                return new List<string>();
            }
            finally
            {
                semaphoreReadServerFile.Release(); // unlock after reading
            }
        }

        public static async Task<bool> SaveEventsToDatabase(List<EventData> events)
        {
            if (events == null || events.Count == 0)
            {
                return false;
            }

            Dictionary<string,int> revSign = new Dictionary<string,int>();
            revSign.Add(REVENUE_PLUS, 1);
            revSign.Add(REVENUE_MINUS, -1);

            var sqlText = "INSERT INTO users_revenue (user_id, revenue) VALUES (@user_id, @revenue)";

            try
            {
                await using (var connection = new NpgsqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    foreach (var eventData in events)
                    {
                        await using (var command = new NpgsqlCommand(sqlText, connection))
                        {
                            command.Parameters.AddWithValue("@user_id", eventData.UserId);
                            command.Parameters.AddWithValue("@revenue", eventData.Value * revSign[eventData.Name]);

                            await command.ExecuteNonQueryAsync();
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to save data to database");
                return false;
            }
        }
    }

    public class EventData
    {
        public string UserId { get; set; }
        public string Name { get; set; }
        public int Value { get; set; }
    }
}
