using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Controls.Intern;
using Blish_HUD.Input;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Microsoft.Xna.Framework;
using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using BHUD.PvPShadowRealmModule.Models;
using BHUD.PvPShadowRealmModule.Controls;

namespace BHUD.PvPShadowRealmModule
{
    //VirtualKeyShort enum for use with keyboard simulation.
    public enum VirtualKeyShort : ushort
    {
        RETURN = 0x0D,
        CONTROL = 0x11,
        KEY_A = 0x41,
        BACK = 0x08,
        KEY_V = 0x56
    }

    [Export(typeof(Blish_HUD.Modules.Module))]
    public class PvPShadowRealmModule : Blish_HUD.Modules.Module
    {
        private static readonly Logger Logger = Logger.GetLogger<PvPShadowRealmModule>();
        internal static PvPShadowRealmModule ModuleInstance;

        #region Service Managers
        internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;
        #endregion

        #region Settings
        public static SettingEntry<bool> _settingShowAlertPopup;
        public static SettingEntry<bool> _settingIncludeScam;
        public static SettingEntry<bool> _settingIncludeRMT;
        public static SettingEntry<bool> _settingIncludeGW2E;
        public static SettingEntry<bool> _settingIncludeOther;
        public static SettingEntry<bool> _settingIncludeUnknown;
        public static SettingEntry<int> _settingInputBuffer;
        #endregion

        private PopupWindow _popupWindow;
        private BlacklistCornerIcon _blacklistCornerIcon;
        internal Blacklists _blacklists;
        private volatile bool _doSync = false;
        private double _runningTime;

        [ImportingConstructor]
        public PvPShadowRealmModule([Import("ModuleParameters")] ModuleParameters moduleParameters)
            : base(moduleParameters)
        {
            ModuleInstance = this;
        }

        protected override void DefineSettings(SettingCollection settings)
        {
            _settingShowAlertPopup = settings.DefineSetting("ShowAlertPopup", false, () => "Show alert popup", () => "Displays an alert popup for updates.");
            _settingIncludeScam = settings.DefineSetting("IncludeScam", true, () => "Include scammers", () => "Block individuals marked as scammers.");
            _settingIncludeRMT = settings.DefineSetting("IncludeRMT", true, () => "Include RMT", () => "Block real money traders.");
            _settingIncludeGW2E = settings.DefineSetting("IncludeGW2E", true, () => "Include GW2E", () => "Block individuals blacklisted by r/GW2Exchange.");
            _settingIncludeOther = settings.DefineSetting("IncludeOther", true, () => "Include Other", () => "Block individuals for various reasons.");
            _settingIncludeUnknown = settings.DefineSetting("IncludeUnknown", true, () => "Include Unknown", () => "Block individuals for unknown reasons.");
            _settingInputBuffer = settings.DefineSetting("InputBuffer", 100, () => "Input Buffer", () => "Time between adding names. Range: 100 - 500.");
            _settingInputBuffer.SetRange(100, 500);

            // Update estimated sync time when the input buffer setting changes.
            _settingInputBuffer.SettingChanged += (s, e) => _blacklists.EstimateTime();
        }

        protected override async Task LoadAsync()
        {
            _blacklists = new Blacklists();
            await _blacklists.LoadAll().ConfigureAwait(false);
        }

        protected override void OnModuleLoaded(EventArgs e)
        {
            _blacklistCornerIcon = new BlacklistCornerIcon();
            BindIconMenuItems();

            GameService.GameIntegration.Gw2Instance.Gw2LostFocus += OnGw2LostFocus;
            // Trigger an initial update check
            CheckForBlacklistUpdate(true, true).ConfigureAwait(false);

            base.OnModuleLoaded(e);
        }

        private void BindIconMenuItems()
        {
            // Wire up corner icon menu items to their respective handlers.
            _blacklistCornerIcon.SyncMenuItem.Click += (s, e) => InitiateSync();
            _blacklistCornerIcon.UpdateCheckMenuItem.Click += async (s, e) => await ForceUpdateCheck().ConfigureAwait(false);
            _blacklistCornerIcon.ResetMenuItem.Click += (s, e) => InitiateReset();
            _blacklistCornerIcon.SkipUpdateMenuItem.Click += async (s, e) => await SkipUpdate().ConfigureAwait(false);
        }

        private void OnGw2LostFocus(object sender, EventArgs e)
        {
            if (_doSync)
            {
                _doSync = false;
                _popupWindow?.Dispose();
                MakePauseWindow(1);
            }
        }

        protected override void Update(GameTime gameTime)
        {
            _runningTime += gameTime.ElapsedGameTime.TotalMilliseconds;

            if (_runningTime > 1800000) // Check every 30 minutes
            {
                _runningTime = 0;
                CheckForBlacklistUpdate(true, true).ConfigureAwait(false);
            }
        }

        protected override void Unload()
        {
            _popupWindow?.Dispose();
            _blacklistCornerIcon?.Dispose();
        }

        private async Task ForceUpdateCheck()
        {
            await CheckForBlacklistUpdate(false, true).ConfigureAwait(false);

            if (_blacklists.HasMissingNames())
            {
                ShowPopupWindow("Update Available!", "New Players have been added! Click the button to start sync.\n\n" + _blacklists.NewNamesLabel(), "Begin Sync", InitiateSync);
            }
            else
            {
                ShowPopupWindow("No Updates", "No new players found", "Close", () => _popupWindow.Dispose());
            }
        }

        private void ShowPopupWindow(string title, string message, string buttonText, Action buttonAction)
        {
            _popupWindow = new PopupWindow(title);
            _popupWindow.ShowUpperLabel(message);
            _popupWindow.ShowMiddleButton(buttonText);
            _popupWindow.MiddleButton.Click += (s, e) => buttonAction();
        }

        private async Task CheckForBlacklistUpdate(bool showPopup, bool checkForUpdates)
        {
            if (checkForUpdates)
            {
                await _blacklists.HasUpdate().ConfigureAwait(false);
            }
            else
            {
                _blacklists.LoadMissingList();
            }

            if (_blacklists.HasMissingNames())
            {
                if (showPopup && _settingShowAlertPopup.Value)
                {
                    ShowPopupWindow("Update Available!", "New players have been added! Click the button to start sync.\n\n" + _blacklists.NewNamesLabel(), "Begin Sync", InitiateSync);
                }

                _blacklistCornerIcon.ShowAlert(_blacklists.MissingAll);
            }
            else
            {
                _blacklistCornerIcon.ShowNormal();
            }
        }

        /// <summary>
        /// Initiates the sync process by showing a pre-sync popup.
        /// </summary>
        private void InitiateSync()
        {
            _popupWindow = new PopupWindow("Update Blocklist");
            _popupWindow.ShowUpperLabel("Get to a safe spot and close all other windows, then press Next\n\n" + _blacklists.NewNamesLabel());
            _popupWindow.ShowMiddleButton("Next");
            _popupWindow.MiddleButton.Click += (s, e) =>
            {
                _popupWindow.Dispose();
                ConfirmSync();
            };
        }

        /// <summary>
        /// Shows a confirmation popup with estimated sync time if many names are pending.
        /// </summary>
        private void ConfirmSync()
        {
            _popupWindow = new PopupWindow("Update Blocklist");
            if (_blacklists.MissingAll > 30)
            {
                _popupWindow.ShowUpperLabel("You are about to sync a lot of names, this will take about " + _blacklists.EstimatedTime + " seconds.\n\n");
            }
            _popupWindow.ShowLowerLabel("Please remain still and do not alt-tab or interact during the sync process.");
            _popupWindow.ShowLeftButton("Start Sync");
            _popupWindow.LeftButton.Click += async (s, e) =>
            {
                _popupWindow.Dispose();
                await SyncNames();
            };
            _popupWindow.ShowRightButton("Cancel");
            _popupWindow.RightButton.Click += (s, e) => _popupWindow.Dispose();
        }

        /// <summary>
        /// Loops through missing names, simulating clipboard and keyboard input to update the in-game block list.
        /// </summary>
        private async Task SyncNames()
        {
            _popupWindow = new PopupWindow("Syncing");
            _popupWindow.ShowBackgroundImage();
            _popupWindow.ShowLeftButton("Pause");
            _popupWindow.LeftButton.Click += (s, e) =>
            {
                _doSync = false;
                _popupWindow.Dispose();
                MakePauseWindow(0);
            };
            _popupWindow.ShowRightButton("Cancel");
            _popupWindow.RightButton.Click += (s, e) =>
            {
                _doSync = false;
                _popupWindow.Dispose();
            };
            _popupWindow.ShowUpperLabel("Please do not alt-tab.\n");

            int count = _blacklists.MissingAll;
            _doSync = true;

            foreach (BlacklistedPlayer player in _blacklists.missingBlacklistedPlayers)
            {
                string ign = player.Ign;
                _popupWindow.ShowName(ign);
                _popupWindow.Subtitle = count + " remaining";

                try
                {
                    bool copied = await ClipboardUtil.WindowsClipboardService.SetTextAsync("/block " + ign);
                    if (copied)
                    {
                        // Simulate sending the block command via keyboard strokes.
                        Keyboard.Stroke((Blish_HUD.Controls.Extern.VirtualKeyShort)VirtualKeyShort.RETURN);
                        await Task.Delay(50);

                        if (!Gw2MumbleService.Gw2Mumble.UI.IsTextInputFocused)
                        {
                            Keyboard.Stroke((Blish_HUD.Controls.Extern.VirtualKeyShort)VirtualKeyShort.RETURN);
                            await Task.Delay(50);
                        }

                        if (!Gw2MumbleService.Gw2Mumble.UI.IsTextInputFocused)
                        {
                            await Task.Delay(_settingInputBuffer.Value);
                        }

                        // Clear text using Ctrl+A then BACKSPACE.
                        Keyboard.Press((Blish_HUD.Controls.Extern.VirtualKeyShort)VirtualKeyShort.CONTROL, true);
                        Keyboard.Stroke((Blish_HUD.Controls.Extern.VirtualKeyShort)VirtualKeyShort.KEY_A, true);
                        await Task.Delay(25);
                        Keyboard.Release((Blish_HUD.Controls.Extern.VirtualKeyShort)VirtualKeyShort.CONTROL, true);
                        await Task.Delay(25);
                        Keyboard.Stroke((Blish_HUD.Controls.Extern.VirtualKeyShort)VirtualKeyShort.BACK, true);
                        await Task.Delay(25);

                        // Paste command and send.
                        Keyboard.Press((Blish_HUD.Controls.Extern.VirtualKeyShort)VirtualKeyShort.CONTROL, true);
                        Keyboard.Stroke((Blish_HUD.Controls.Extern.VirtualKeyShort)VirtualKeyShort.KEY_V, true);
                        await Task.Delay(25);
                        Keyboard.Release((Blish_HUD.Controls.Extern.VirtualKeyShort)VirtualKeyShort.CONTROL, true);
                        await Task.Delay(25);
                        Keyboard.Stroke((Blish_HUD.Controls.Extern.VirtualKeyShort)VirtualKeyShort.RETURN, true);
                        await Task.Delay(_settingInputBuffer.Value);

                        count--;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Error copying " + ign + " to clipboard: " + ex);
                }

                if (!_doSync)
                {
                    await _blacklists.PartialSync(player);
                    return;
                }
            }

            await _blacklists.SyncLists();
            _popupWindow.Dispose();

            _popupWindow = new PopupWindow("Sync Complete");
            _doSync = false;
            _popupWindow.ShowLowerLabel("Finished syncing your block list successfully");
            _popupWindow.ShowMiddleButton("Close");
            _popupWindow.MiddleButton.Click += (s, e) => _popupWindow.Dispose();

            await CheckForBlacklistUpdate(false, false);
        }
        /// <summary>
        /// Acts as if the player has synced their lists, skipping the pending update.
        /// </summary>
        private async Task SkipUpdate()
        {
            Logger.Info("SkipUpdate() called.");
            await Task.CompletedTask;
        }
        /// <summary>
        /// Resets the internal blocklist.
        /// </summary>
        private async void InitiateReset()
        {
            Logger.Info("InitiateReset() called.");
            try
            {
                await _blacklists.ResetBlacklists().ConfigureAwait(false);
                _blacklistCornerIcon.ShowNormal();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error during reset.");
            }
        }
        /// <summary>
        /// Creates a pause popup window when syncing is interrupted.
        /// </summary>
        /// <param name="reason">0 for general pause; 1 for losing focus, etc.</param>
        private  void MakePauseWindow(int reason)
        {
            _popupWindow = new PopupWindow("Syncing Paused");

            string reasonStr = "";
            switch (reason)
            {
                case 0:
                    reasonStr = "Syncing Paused\n\nMake sure the text box is clear before resuming.";
                    break;
                case 1:
                    reasonStr = "Syncing Paused\n\nPlease do not alt-tab while syncing is in progress.";
                    break;
                default:
                    reasonStr = "Syncing Paused";
                    break;
            }

            _popupWindow.ShowUpperLabel(reasonStr);
            _popupWindow.ShowLeftButton("Resume");
            _popupWindow.LeftButton.Click += async (s, e) =>
            {
                _popupWindow.Dispose();
                await SyncNames();
            };
            _popupWindow.ShowRightButton("Cancel");
            _popupWindow.RightButton.Click += (s, e) => _popupWindow.Dispose();
        }
    }
}
