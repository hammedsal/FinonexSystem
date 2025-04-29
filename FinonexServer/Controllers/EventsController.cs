using Microsoft.AspNetCore.Mvc;

using Npgsql;

namespace FinonexServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly string connectionString = "Host=localhost;Port=5432;Username=postgres;Password=hs2181;Database=finodb";
        private readonly string ServerFileName = "server_events.jsonl";
        private static readonly string REVENUE_PLUS = "add_revenue";
        private static readonly string REVENUE_MINUS = "subtract_revenue";

        private static readonly SemaphoreSlim semaphoreWriteToFile = new SemaphoreSlim(1, 1);

        public EventsController()
        {
        }

        [HttpPost("liveEvent")]
        public async Task<IActionResult> SaveLiveEventToFile([FromBody] EventData liveEventData)
        {
            if (Request.Headers["Authorization"] != "secret")
            {
                return Unauthorized();
            }

            if (liveEventData == null || string.IsNullOrEmpty(liveEventData.UserId) || string.IsNullOrEmpty(liveEventData.Name))
            {
                return BadRequest("Event is invalid.");
            }

            try
            {
                var jsonEventData = System.Text.Json.JsonSerializer.Serialize(liveEventData);
                await WriteToFileAsync(ServerFileName, jsonEventData);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "failed to save the event." });
            }

            return Ok();
        }

        private async Task WriteToFileAsync(string filePath, string jsonEventData)
        {
            int retryCount = 3;
            while (retryCount > 0)
            {
                try
                {
                    await semaphoreWriteToFile.WaitAsync();// lock the file
                    
                    // append to file
                    using (StreamWriter sw = new StreamWriter(filePath, true))
                    {
                        await sw.WriteLineAsync(jsonEventData);
                    }
                    return;
                }
                catch (IOException ex)
                {
                    // since the file can be locked, try after a delay
                    retryCount--;
                    await Task.Delay(100);
                }
                finally
                {
                    semaphoreWriteToFile.Release();// release the file
                }
            }
            throw new Exception("Failed to write event after 3 attempts.");
        }


        [HttpGet("userEvents/{userId}")]
        public async Task<ActionResult<List<EventData>>> GetUserEvents(string userId)
        {
            // I assume there is no huge amount of events per user
            // otherwise, I need pagination

            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("userId is invalid.");
            }

            List<EventData> eventDataList = await GetDatabaseEvents(userId);

            return Ok(eventDataList);
        }

        // query the revenue history from db
        // and convert them to events again
        private async Task<List<EventData>> GetDatabaseEvents(string userId)
        {
            var eventDataList = new List<EventData>();

            string sqlText = "SELECT revenue FROM users_revenue WHERE user_id = @userId";

            await using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                await using (var sqlCmd = new NpgsqlCommand(sqlText, connection))
                {
                    sqlCmd.Parameters.AddWithValue("@userId", userId);

                    await using (var reader = await sqlCmd.ExecuteReaderAsync())
                    {
                        int previousRevenue = 0;
                        while (await reader.ReadAsync())
                        {
                            int currentRevenue = reader.GetInt32(reader.GetOrdinal("revenue")) - previousRevenue;
                            previousRevenue = reader.GetInt32(reader.GetOrdinal("revenue"));

                            var eventData = new EventData
                            {
                                UserId = userId,
                                Value = currentRevenue,
                                Name = currentRevenue >= 0 ? REVENUE_PLUS : REVENUE_MINUS
                            };

                            eventDataList.Add(eventData);
                        }
                    }
                }
            }

            return eventDataList;
        }

        [HttpGet]
        public string Get()
        {
            return "FinonexServer is running";
        }
    }

    public class EventData
    {
        public string UserId { get; set; }
        public string Name { get; set; }
        public int Value { get; set; }
    }
}
