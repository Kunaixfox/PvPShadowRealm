using Blish_HUD;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Teh.BHUD.PvPShadowRealmModule.Models;

namespace Teh.BHUD.PvPShadowRealmModule
{
    public class Blacklists
    {
        private static readonly Logger Logger = Logger.GetLogger<Blacklists>();

        // Thread-safety lock object.
        private readonly object _lock = new object();

        private readonly List<BlacklistedPlayer> internalBlacklist = new List<BlacklistedPlayer>();
        private readonly List<BlacklistedPlayer> externalBlacklist = new List<BlacklistedPlayer>();
        public readonly List<BlacklistedPlayer> missingBlacklistedPlayers = new List<BlacklistedPlayer>();

        public int CachedListIndex { get; set; }

        // Counters for various reasons.
        public int TotalScam;
        public int TotalRMT;
        public int TotalGW2E;
        public int TotalOther;
        public int TotalUnknown;
        public int TotalAll;
        public int MissingScam;
        public int MissingRMT;
        public int MissingGW2E;
        public int MissingOther;
        public int MissingUnknown;
        public int MissingAll;
        public double EstimatedTime;

        public int TotalInternalNames
        {
            get
            {
                lock (_lock)
                {
                    return internalBlacklist.Count;
                }
            }
        }

        private readonly string _blacklistFilePath;

        public Blacklists()
        {
            CachedListIndex = 0;
            _blacklistFilePath = Path.Combine(PvPShadowRealmModule.ModuleInstance.DirectoriesManager.GetFullDirectoryPath("blacklistbuddy"), "Blacklist.json");
        }

        public async Task LoadAll()
        {
            LoadInternalList();
            await LoadExternalList().ConfigureAwait(false);
            LoadMissingList();
            EstimateTime();
        }

        private void LoadInternalList()
        {
            try
            {
                if (!File.Exists(_blacklistFilePath))
                {
                    SaveInternalList();
                    return;
                }

                string jsonContent = File.ReadAllText(_blacklistFilePath);
                lock (_lock)
                {
                    internalBlacklist.Clear();
                    var deserialized = JsonSerializer.Deserialize<List<BlacklistedPlayer>>(jsonContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    internalBlacklist.AddRange(deserialized ?? new List<BlacklistedPlayer>());
                }
                Logger.Info($"Loaded {internalBlacklist.Count} names from internal blacklist.");
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error reading Blacklist.json");
                lock (_lock)
                {
                    internalBlacklist.Clear();
                }
            }
        }

        private async Task LoadExternalList()
        {
            try
            {
                BlacklistedPlayer[] newUserList = await RemoteDataUtil.GetBlockedPlayers().ConfigureAwait(false);

                lock (_lock)
                {
                    if (newUserList.Length > externalBlacklist.Count)
                    {
                        externalBlacklist.Clear();
                        externalBlacklist.AddRange(newUserList);
                    }
                }

                Logger.Info($"Loaded {externalBlacklist.Count} names from external blacklist.");
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to load external blacklist.");
            }
        }

        public void LoadMissingList()
        {
            // Reset all counters before recomputing.
            ResetCounters();

            lock (_lock)
            {
                missingBlacklistedPlayers.Clear();
                var missing = externalBlacklist.Where(ext => internalBlacklist.All(intl => intl.Ign != ext.Ign)).ToList();
                foreach (var player in missing)
                {
                    // Count missing by reason:
                    switch (player.Reason)
                    {
                        case BlacklistedPlayer.BlacklistReason.Scam:
                            TotalScam++;
                            MissingScam++;
                            break;
                        case BlacklistedPlayer.BlacklistReason.RMT:
                            TotalRMT++;
                            MissingRMT++;
                            break;
                        case BlacklistedPlayer.BlacklistReason.GW2E:
                            TotalGW2E++;
                            MissingGW2E++;
                            break;
                        case BlacklistedPlayer.BlacklistReason.Other:
                            TotalOther++;
                            MissingOther++;
                            break;
                        default:
                            TotalUnknown++;
                            MissingUnknown++;
                            break;
                    }
                    MissingAll++;
                    missingBlacklistedPlayers.Add(player);
                    TotalAll++;
                }
            }
        }

        private void ResetCounters()
        {
            TotalScam = TotalRMT = TotalGW2E = TotalOther = TotalUnknown = TotalAll = 0;
            MissingScam = MissingRMT = MissingGW2E = MissingOther = MissingUnknown = MissingAll = 0;
        }

        public void EstimateTime()
        {
            // Example estimation: each new name takes (input buffer + 100) milliseconds to process.
            EstimatedTime = Math.Round((double)(MissingAll * (PvPShadowRealmModule._settingInputBuffer.Value + 100)) / 1000, 2);
        }

        private void SaveInternalList()
        {
            try
            {
                string jsonContent;
                lock (_lock)
                {
                    jsonContent = JsonSerializer.Serialize(internalBlacklist, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                }
                File.WriteAllText(_blacklistFilePath, jsonContent);
                Logger.Info("Internal blacklist saved successfully.");
            }
            catch (Exception e)
            {
                Logger.Warn($"Error saving Blacklist.json: {e}");
            }
        }

        public async Task<bool> HasUpdate()
        {
            await LoadAll().ConfigureAwait(false);
            return missingBlacklistedPlayers.Any();
        }

        public async Task SyncLists()
        {
            lock (_lock)
            {
                internalBlacklist.AddRange(missingBlacklistedPlayers);
            }
            SaveInternalList();
            LoadMissingList();
            await Task.CompletedTask;
        }

        public async Task ResetBlacklists()
        {
            lock (_lock)
            {
                internalBlacklist.Clear();
                missingBlacklistedPlayers.Clear();
            }
            SaveInternalList();
            LoadMissingList();
            await Task.CompletedTask;
        }

        public string NewNamesLabel()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Total New IGNs: {MissingAll}");
            if (MissingScam > 0) sb.AppendLine($"New Scammer IGNs: {MissingScam}");
            if (MissingRMT > 0) sb.AppendLine($"New RMT IGNs: {MissingRMT}");
            if (MissingGW2E > 0) sb.AppendLine($"New GW2E IGNs: {MissingGW2E}");
            if (MissingOther > 0) sb.AppendLine($"New Other IGNs: {MissingOther}");
            if (MissingUnknown > 0) sb.AppendLine($"New Unknown IGNs: {MissingUnknown}");
            return sb.ToString().Trim();
        }
        /// <param name="blacklistedPlayer">The last player added to the block list</param>
        public async Task PartialSync(BlacklistedPlayer blacklistedPlayer)
        {
            int index = missingBlacklistedPlayers.IndexOf(blacklistedPlayer);
            internalBlacklist.AddRange(missingBlacklistedPlayers.GetRange(0, index + 1));
            SaveInternalList();
            LoadMissingList();
            //await LoadAll(); ========================================================================================================================================================================
        }
        // Public method to determine if there are any missing names.
        public bool HasMissingNames() => missingBlacklistedPlayers.Any();
    }
}
