using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ShieldX.Models;

namespace ShieldX.Services
{
    /// <summary>
    /// Service for checking email addresses against HaveIBeenPwned database
    /// </summary>
    public class DarkWebService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static DateTime _lastApiCall = DateTime.MinValue;
        private const double ApiRateLimitMs = 1500; // HaveIBeenPwned recommends 1.5+ seconds between requests
        private const string HibpApiBaseUrl = "https://haveibeenpwned.com/api/v3";
        private const string UserAgentString = "ShieldX-Antivirus/3.1.1"; // Required by HIBP API

        static DarkWebService()
        {
            // Configure HTTP client with proper headers
            _httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgentString);
        }

        /// <summary>
        /// Checks if an email address has been involved in any known breaches
        /// </summary>
        /// <param name="email">Email address to check</param>
        /// <param name="includeUnverified">Include unverified breaches in results</param>
        /// <returns>List of breaches found, or empty list if clean</returns>
        public static async Task<(bool HasBreaches, List<BreachData> Breaches, string ErrorMessage)> CheckEmailBreachesAsync(
            string email, 
            bool includeUnverified = false)
        {
            try
            {
                // Validate email format
                if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
                {
                    return (false, new List<BreachData>(), "Invalid email address format");
                }

                // Enforce rate limiting to respect HIBP API courtesy
                await EnforceRateLimitAsync();

                // URL encode the email for the API call
                string encodedEmail = System.Web.HttpUtility.UrlEncode(email);
                string apiUrl = $"{HibpApiBaseUrl}/breachedaccount/{encodedEmail}?truncateResponse=false&includeUnverified={includeUnverified}";

                using (var request = new HttpRequestMessage(HttpMethod.Get, apiUrl))
                {
                    request.Headers.Add("User-Agent", UserAgentString);

                    var response = await _httpClient.SendAsync(request);

                    // 404 means email not found in any breaches (clean)
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        _lastApiCall = DateTime.UtcNow;
                        return (false, new List<BreachData>(), null);
                    }

                    // 200 means breaches found
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        _lastApiCall = DateTime.UtcNow;
                        var content = await response.Content.ReadAsStringAsync();
                        var breaches = ParseBreachResponse(content);
                        return (breaches.Count > 0, breaches, null);
                    }

                    // Handle rate limiting (429)
                    if ((int)response.StatusCode == 429)
                    {
                        _lastApiCall = DateTime.UtcNow;
                        return (false, new List<BreachData>(), 
                            "API rate limit exceeded. Please try again in a few moments.");
                    }

                    // Other errors
                    _lastApiCall = DateTime.UtcNow;
                    return (false, new List<BreachData>(), 
                        $"API error: {response.StatusCode}");
                }
            }
            catch (HttpRequestException ex)
            {
                return (false, new List<BreachData>(), 
                    $"Connection error: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                return (false, new List<BreachData>(), 
                    "Request timeout. Please check your connection.");
            }
            catch (Exception ex)
            {
                return (false, new List<BreachData>(), 
                    $"Error checking breaches: {ex.Message}");
            }
        }

        /// <summary>
        /// Parses the JSON response from HaveIBeenPwned API
        /// </summary>
        private static List<BreachData> ParseBreachResponse(string jsonResponse)
        {
            var breaches = new List<BreachData>();

            try
            {
                using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                {
                    JsonElement root = doc.RootElement;

                    // Handle if root is an array
                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var breachElement in root.EnumerateArray())
                        {
                            var breach = ParseBreachElement(breachElement);
                            if (breach != null)
                            {
                                breaches.Add(breach);
                            }
                        }
                    }
                }

                // Sort breaches by date, most recent first
                return breaches.OrderByDescending(b => b.BreachDate).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing breach response: {ex.Message}");
                return breaches;
            }
        }

        /// <summary>
        /// Parses a single breach element from the JSON response
        /// </summary>
        private static BreachData ParseBreachElement(JsonElement element)
        {
            try
            {
                var breach = new BreachData();

                // Parse basic properties
                if (element.TryGetProperty("Name", out var nameElem))
                    breach.Name = nameElem.GetString() ?? "";
                if (element.TryGetProperty("Title", out var titleElem))
                    breach.Title = titleElem.GetString() ?? "";

                // Parse dates
                if (element.TryGetProperty("BreachDate", out var breachDateElem) && 
                    breachDateElem.GetString() is string breachDateStr &&
                    DateTime.TryParse(breachDateStr, out var breachDate))
                {
                    breach.BreachDate = breachDate;
                }

                if (element.TryGetProperty("AddedDate", out var addedDateElem) &&
                    addedDateElem.GetString() is string addedDateStr &&
                    DateTime.TryParse(addedDateStr, out var addedDate))
                {
                    breach.AddedDate = addedDate;
                }

                if (element.TryGetProperty("ModifiedDate", out var modifiedDateElem) &&
                    modifiedDateElem.GetString() is string modifiedDateStr &&
                    DateTime.TryParse(modifiedDateStr, out var modifiedDate))
                {
                    breach.ModifiedDate = modifiedDate;
                }

                // Parse breach details
                if (element.TryGetProperty("PwnCount", out var pwnCountElem) &&
                    pwnCountElem.GetInt32() is int pwnCount)
                {
                    breach.PwnCount = pwnCount;
                }

                if (element.TryGetProperty("Description", out var descElem))
                    breach.Description = descElem.GetString() ?? "";

                if (element.TryGetProperty("Domain", out var domainElem))
                    breach.Domain = domainElem.GetString() ?? "";

                if (element.TryGetProperty("LogoPath", out var logoElem))
                    breach.LogoPath = logoElem.GetString() ?? "";

                // Parse boolean flags
                if (element.TryGetProperty("IsVerified", out var verifiedElem))
                    breach.IsVerified = verifiedElem.GetBoolean();

                if (element.TryGetProperty("IsFabricated", out var fabricatedElem))
                    breach.IsFabricated = fabricatedElem.GetBoolean();

                if (element.TryGetProperty("IsSensitive", out var sensitiveElem))
                    breach.IsSensitive = sensitiveElem.GetBoolean();

                if (element.TryGetProperty("IsRetired", out var retiredElem))
                    breach.IsRetired = retiredElem.GetBoolean();

                if (element.TryGetProperty("IsSpamList", out var spamElem))
                    breach.IsSpamList = spamElem.GetBoolean();

                // Parse data classes (types of exposed data)
                if (element.TryGetProperty("DataClasses", out var dataClassesElem) &&
                    dataClassesElem.ValueKind == JsonValueKind.Array)
                {
                    var dataClasses = new List<string>();
                    foreach (var dcElem in dataClassesElem.EnumerateArray())
                    {
                        if (dcElem.GetString() is string dc)
                            dataClasses.Add(dc);
                    }
                    breach.DataClasses = dataClasses;
                }

                return breach;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing breach element: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Validates email address format
        /// </summary>
        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Enforces rate limiting for API calls
        /// </summary>
        private static async Task EnforceRateLimitAsync()
        {
            var timeSinceLastCall = DateTime.UtcNow - _lastApiCall;
            if (timeSinceLastCall.TotalMilliseconds < ApiRateLimitMs)
            {
                var delayMs = (int)(ApiRateLimitMs - timeSinceLastCall.TotalMilliseconds);
                await Task.Delay(delayMs);
            }
        }

        /// <summary>
        /// Gets the severity level for a breach based on data types exposed
        /// </summary>
        public static string GetBreachSeverity(BreachData breach)
        {
            if (breach?.DataClasses == null || breach.DataClasses.Count == 0)
                return "Medium";

            var dataClasses = breach.DataClasses;
            
            // High severity indicators
            var highSeverityKeywords = new[] 
            { 
                "passwords", "credit cards", "payment methods", "social security",
                "bank accounts", "passport", "drivers license", "medical"
            };

            if (dataClasses.Any(dc => 
                highSeverityKeywords.Any(hs => dc.Contains(hs, StringComparison.OrdinalIgnoreCase))))
            {
                return "High";
            }

            // Medium severity indicators
            var mediumSeverityKeywords = new[]
            {
                "email addresses", "phone numbers", "usernames", "ip addresses"
            };

            if (dataClasses.Any(dc =>
                mediumSeverityKeywords.Any(ms => dc.Contains(ms, StringComparison.OrdinalIgnoreCase))))
            {
                return "Medium";
            }

            return "Low";
        }
    }
}
