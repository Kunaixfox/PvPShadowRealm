using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Blish_HUD;
using Newtonsoft.Json;
using Teh.BHUD.PvPShadowRealmModule.Models;

namespace Teh.BHUD.PvPShadowRealmModule
{
    public static class RemoteDataUtil
    {
        private static readonly Logger Logger = Logger.GetLogger(typeof(RemoteDataUtil));

        // URL to the raw JSON file.
        private const string BLOCKLIST_URI = "https://bhm.blishhud.com/Teh.BHUD.Blacklist_Buddy_Module/blocklist.json";

        // Static HttpClient instance to reuse for all requests.
        private static readonly HttpClient _httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(30) };

        /// <summary>
        /// Returns a list of BlacklistedPlayer objects from the hosted JSON.
        /// </summary>
        public static async Task<BlacklistedPlayer[]> GetBlockedPlayers(CancellationToken cancellationToken = default)
        {
            try
            {
                return await RetryPolicy(async () =>
                {
                    Logger.Info($"Downloading blocklist from {BLOCKLIST_URI}...");
                    var response = await _httpClient.GetStringAsync(BLOCKLIST_URI);

                    // Use Newtonsoft.Json for deserialization
                    var players = JsonConvert.DeserializeObject<BlacklistedPlayer[]>(response);

                    if (players == null || players.Length == 0)
                    {
                        Logger.Warn("No players found in the downloaded blocklist.");
                    }
                    else
                    {
                        Logger.Info($"Successfully downloaded blocklist with {players.Length} players.");
                    }

                    return players ?? new BlacklistedPlayer[0];
                }, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException httpEx)
            {
                Logger.Warn(httpEx, $"HTTP error while downloading blocklist from {BLOCKLIST_URI}: {httpEx.Message}");
            }
            catch (TaskCanceledException tcEx)
            {
                Logger.Warn($"Timeout occurred while downloading blocklist from {BLOCKLIST_URI}: {tcEx.Message}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Unexpected error while downloading blocklist from {BLOCKLIST_URI}.");
            }

            return new BlacklistedPlayer[0];
        }

        /// <summary>
        /// Retry policy for handling transient network issues.
        /// Supports a broader range of exceptions and cancellation.
        /// </summary>
        private static async Task<T> RetryPolicy<T>(Func<Task<T>> action, CancellationToken cancellationToken, int maxAttempts = 3)
        {
            int attempt = 0;
            do
            {
                try
                {
                    return await action().ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
                {
                    attempt++;
                    Logger.Warn($"Network error on attempt {attempt} of {maxAttempts}: {ex.Message}");
                    if (attempt >= maxAttempts)
                    {
                        Logger.Error($"All {maxAttempts} attempts failed. Exception: {ex}");
                        throw;
                    }
                    await Task.Delay(1000 * attempt, cancellationToken).ConfigureAwait(false);
                }
            } while (attempt < maxAttempts);

            throw new HttpRequestException("Failed to retrieve data after multiple attempts.");
        }
    }
}
