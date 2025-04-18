using Microsoft.Extensions.Configuration;

namespace PushoverDownAlert
{
    class Program
    {
        private static string _pushoverUserKey = null!;
        private static string _pushoverAppToken = null!;
        private static string[] _websiteUrls = null!;
        private static int _checkIntervalMs = 5 * 60 * 1000;
        private const string PushoverApiUrl = "https://api.pushover.net/1/messages.json";
        
        private static readonly HttpClient HttpClient = new HttpClient();
        
        static async Task Main(string[] args)
        {
            // Load configuration
            GetConfig();
            
            string websiteUrls = string.Join(", ", _websiteUrls);
            Console.WriteLine($"Starting website monitoring for {websiteUrls} websites.");
            Console.WriteLine($"Checking every {_checkIntervalMs / 1000} seconds");
            Console.WriteLine("Press Ctrl+C to exit");
            
            while (true)
            {
                for (var i = 0; i < _websiteUrls.Length; i++)
                {
                    string url = _websiteUrls[i].Trim();
                    try
                    {
                        
                        bool isWebsiteUp = await CheckWebsiteStatus(url);
                    
                        if (!isWebsiteUp)
                        {
                            Console.WriteLine($"[{DateTime.Now}] ALERT: Website {url} is DOWN!");
                            await SendPushoverAlert($"Website {url} is DOWN", $"The website check failed at {DateTime.Now}");
                        }
                        else
                        {
                            Console.WriteLine($"[{DateTime.Now}] Website {url} is UP");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{DateTime.Now}] Error during website check: {ex.Message}");
                        await SendPushoverAlert("Website Monitor Error", $"Error checking {url}: {ex.Message}");
                    }
                }
                
                Thread.Sleep(_checkIntervalMs);
            }
        }

        static void GetConfig()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddUserSecrets<Program>();
            
            IConfiguration config = builder.Build();
            
            string pushoverUserKey = Environment.GetEnvironmentVariable("PushoverUserKey") 
                ?? config["Pushover:UserKey"]!;;
            string pushoverAppToken = Environment.GetEnvironmentVariable("PushoverAppToken")
                ?? config["Pushover:AppToken"]!;
            string websiteUrls = Environment.GetEnvironmentVariable("WebsiteUrls")
                ?? config["Website:Urls"]!;
            int checkInterval = int.TryParse(Environment.GetEnvironmentVariable("CheckInterval"), out int parsedValue) 
                ? parsedValue 
                : config.GetValue<int>("Website:CheckInterval", _checkIntervalMs);

            
            _pushoverUserKey = pushoverUserKey;
            _pushoverAppToken = pushoverAppToken;
            _websiteUrls = websiteUrls.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            _checkIntervalMs = checkInterval * 60 * 1000; 
        }
        
        static async Task<bool> CheckWebsiteStatus(string url)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                var response = await HttpClient.SendAsync(request);
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                // Any exception means the website is down or unreachable
                return false;
            }
        }
        
        static async Task SendPushoverAlert(string title, string message)
        {
            try
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("token", _pushoverAppToken),
                    new KeyValuePair<string, string>("user", _pushoverUserKey),
                    new KeyValuePair<string, string>("title", title),
                    new KeyValuePair<string, string>("message", message),
                    new KeyValuePair<string, string>("priority", "1")
                });
                
                var response = await HttpClient.PostAsync(PushoverApiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Pushover alert sent successfully");
                }
                else
                {
                    Console.WriteLine($"Failed to send Pushover alert: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending Pushover alert: {ex.Message}");
            }
        }
    }
}