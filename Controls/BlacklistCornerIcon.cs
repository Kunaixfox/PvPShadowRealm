using System;
using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Teh.BHUD.PvPShadowRealmModule;

namespace Teh.BHUD.PvPShadowRealmModule.Controls
{
    public class BlacklistCornerIcon : CornerIcon
    {
        private readonly ContextMenuStrip _iconMenu;
        internal ContextMenuStripItem SyncMenuItem;
        internal ContextMenuStripItem UpdateCheckMenuItem;
        internal ContextMenuStripItem ResetMenuItem;
        internal ContextMenuStripItem SkipUpdateMenuItem;

        private static readonly Logger Logger = Logger.GetLogger<BlacklistCornerIcon>();

        #region Static Resources

        // These are the underlying Texture2D objects loaded from the ContentsManager.
        private static readonly Texture2D _blacklistIconAlertTexture;
        private static readonly Texture2D _blacklistIconTexture;

        static BlacklistCornerIcon()
        {
            _blacklistIconAlertTexture = PvPShadowRealmModule.ModuleInstance.ContentsManager.GetTexture("BlacklistAlert.png");
            if (_blacklistIconAlertTexture == null)
            {
                Logger.Error("Failed to load BlacklistAlert.png texture.");
            }
            _blacklistIconTexture = PvPShadowRealmModule.ModuleInstance.ContentsManager.GetTexture("Blacklist.png");
            if (_blacklistIconTexture == null)
            {
                Logger.Error("Failed to load Blacklist.png texture.");
            }
        }

        #endregion

        public BlacklistCornerIcon()
        {
            _iconMenu = BuildContextMenu();

            // Directly set the icon by wrapping the Texture2D in an AsyncTexture2D.
            Icon = new AsyncTexture2D(_blacklistIconTexture);
            BasicTooltipText = "Blacklist Buddy - Click to open menu";
            Parent = GameService.Graphics.SpriteScreen;
            Menu = _iconMenu;

            AttachEventHandlers();
        }

        #region Context Menu

        private ContextMenuStrip BuildContextMenu()
        {
            var contextMenu = new ContextMenuStrip();

            SyncMenuItem = CreateMenuItem("Sync Blocklist", "Opens the blacklist sync window");
            UpdateCheckMenuItem = CreateMenuItem("Check for Updates", "Forces the module to check for new blacklist updates");
            ResetMenuItem = CreateMenuItem("Reset Blacklist", "Resets the internal blacklist, allowing a full resync");
            SkipUpdateMenuItem = CreateMenuItem("Skip Sync", "Skips syncing if your blacklist is already up to date");

            if (SyncMenuItem != null)
                contextMenu.AddMenuItem(SyncMenuItem);
            else
                Logger.Warn("SyncMenuItem is null in BuildContextMenu.");

            if (UpdateCheckMenuItem != null)
                contextMenu.AddMenuItem(UpdateCheckMenuItem);
            else
                Logger.Warn("UpdateCheckMenuItem is null in BuildContextMenu.");

            if (ResetMenuItem != null)
                contextMenu.AddMenuItem(ResetMenuItem);
            else
                Logger.Warn("ResetMenuItem is null in BuildContextMenu.");

            if (SkipUpdateMenuItem != null)
                contextMenu.AddMenuItem(SkipUpdateMenuItem);
            else
                Logger.Warn("SkipUpdateMenuItem is null in BuildContextMenu.");

            return contextMenu;
        }

        private ContextMenuStripItem CreateMenuItem(string text, string tooltip)
        {
            return new ContextMenuStripItem(text)
            {
                BasicTooltipText = tooltip
            };
        }

        #endregion

        #region Event Handlers

        private void AttachEventHandlers()
        {
            this.Click += OnIconClick;

            if (SyncMenuItem != null)
                SyncMenuItem.Click += (s, e) => OnMenuItemClick("Sync Blocklist clicked.");
            else
                Logger.Warn("SyncMenuItem is null in AttachEventHandlers.");

            if (UpdateCheckMenuItem != null)
                UpdateCheckMenuItem.Click += (s, e) => OnMenuItemClick("Check for Updates clicked.");
            else
                Logger.Warn("UpdateCheckMenuItem is null in AttachEventHandlers.");

            if (ResetMenuItem != null)
                ResetMenuItem.Click += (s, e) => OnMenuItemClick("Reset Blacklist clicked.");
            else
                Logger.Warn("ResetMenuItem is null in AttachEventHandlers.");

            if (SkipUpdateMenuItem != null)
                SkipUpdateMenuItem.Click += (s, e) => OnMenuItemClick("Skip Sync clicked.");
            else
                Logger.Warn("SkipUpdateMenuItem is null in AttachEventHandlers.");
        }

        private void OnIconClick(object sender, MouseEventArgs e)
        {
            Point location = GameService.Input.Mouse.Position;
            _iconMenu.Location = location;
            _iconMenu.Show();
        }

        private void OnMenuItemClick(string message)
        {
            Logger.Info(message);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows an alert on the corner icon including the number of new names.
        /// </summary>
        public void ShowAlert(int numNewNames)
        {
            // Compare the underlying texture of the current AsyncTexture2D
            if (Icon == null || Icon.Texture != _blacklistIconAlertTexture)
            {
                Icon = new AsyncTexture2D(_blacklistIconAlertTexture);
                BasicTooltipText = $"Update Available - {numNewNames} names to add - Click to open menu";
            }
        }

        /// <summary>
        /// Reverts the corner icon to its normal state.
        /// </summary>
        public void ShowNormal()
        {
            if (Icon == null || Icon.Texture != _blacklistIconTexture)
            {
                Icon = new AsyncTexture2D(_blacklistIconTexture);
                BasicTooltipText = "Blacklist Buddy";
            }
        }

        #endregion

        #region Hover Effects and Animations

        protected override void OnMouseEntered(MouseEventArgs e)
        {
            this.Opacity = 0.8f;
            base.OnMouseEntered(e);
        }

        protected override void OnMouseLeft(MouseEventArgs e)
        {
            this.Opacity = 1.0f;
            base.OnMouseLeft(e);
        }

        #endregion
    }
}
