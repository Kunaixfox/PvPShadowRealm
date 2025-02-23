using Blish_HUD;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Teh.BHUD.PvPShadowRealmModule.Controls
{
    public class PopupWindow : Container
    {
        // Title label for main title.
        private Label _titleLabel;
        // Subtitle label.
        private Label _subtitleLabel;
        // Labels for displaying messages.
        private Label _upperLabel;
        private Label _lowerLabel;
        private Label _nameLabel;
        // Content background image.
        private Image _contentBackgroundImage;
        // Background panel for the entire popup.
        private Panel _backgroundPanel;
        // Buttons.
        public StandardButton LeftButton;
        public StandardButton RightButton;
        public StandardButton MiddleButton;
        // Close button in title bar.
        private StandardButton _closeButton;

        /// <summary>
        /// Gets or sets the subtitle text.
        /// </summary>
        public string Subtitle
        {
            get => _subtitleLabel?.Text ?? string.Empty;
            set
            {
                if (_subtitleLabel != null)
                {
                    _subtitleLabel.Text = value;
                    _subtitleLabel.Show();
                }
            }
        }

        #region Static Resources

        private static readonly Texture2D _windowBackgroundTexture;
        private static readonly Texture2D _windowEmblemTexture;
        private static readonly Texture2D _contentBackgroundTexture;

        static PopupWindow()
        {
            _windowBackgroundTexture = PvPShadowRealmModule.ModuleInstance.ContentsManager.GetTexture("155960resize.png");
            _windowEmblemTexture = PvPShadowRealmModule.ModuleInstance.ContentsManager.GetTexture("1654245.png");
            _contentBackgroundTexture = PvPShadowRealmModule.ModuleInstance.ContentsManager.GetTexture("156771.png");
        }

        #endregion

        public PopupWindow(string title)
        {
            // Set up the popup container.
            this.Size = new Point(488, 236);
            this.Location = new Point(125, 125);
            this.Visible = true;
            this.Parent = GameService.Graphics.SpriteScreen;
            // Set a background color for the popup.
            this.BackgroundColor = new Color(20, 20, 20, 230);

            BuildContents(title);
        }

        private void BuildContents(string title)
        {
            // Create a background panel that fills the popup.
            _backgroundPanel = new Panel()
            {
                Size = this.Size,
                Location = Point.Zero,
                BackgroundColor = this.BackgroundColor,
                ZIndex = -10,
                Parent = this
            };

            int titleBarHeight = 40;
            // Create title bar panel.
            Panel titleBar = new Panel()
            {
                Size = new Point(this.Width, titleBarHeight),
                Location = new Point(0, 0),
                BackgroundTexture = _windowBackgroundTexture ?? null,
                BackgroundColor = _windowBackgroundTexture == null ? new Color(30, 30, 30, 255) : Color.White,
                Parent = this
            };

            // Create title label.
            _titleLabel = new Label()
            {
                Location = new Point(10, 5),
                Size = new Point(this.Width - 50, 30),
                TextColor = Color.DarkRed,
                Font = GameService.Content.DefaultFont14,
                HorizontalAlignment = HorizontalAlignment.Left,
                Visible = true,
                Parent = titleBar,
                Text = title
            };

            // Create close button.
            _closeButton = new StandardButton()
            {
                Text = "X",
                Size = new Point(30, 30),
                Location = new Point(this.Width - 35, 5),
                Visible = true,
                Parent = titleBar
            };
            _closeButton.Click += (s, e) => this.Dispose();

            // Create subtitle label (below title bar).
            _subtitleLabel = new Label()
            {
                Location = new Point(10, titleBarHeight),
                Size = new Point(this.Width - 20, 30),
                TextColor = Color.DarkRed,
                Font = GameService.Content.DefaultFont14,
                HorizontalAlignment = HorizontalAlignment.Center,
                Visible = false,
                Parent = this
            };

            // Create upper, lower, and name labels.
            _upperLabel = new Label()
            {
                Location = new Point(10, titleBarHeight + 40),
                Size = new Point(this.Width - 20, 40),
                TextColor = Color.DarkRed,
                Font = GameService.Content.DefaultFont14,
                HorizontalAlignment = HorizontalAlignment.Center,
                Visible = false,
                Parent = this
            };

            _lowerLabel = new Label()
            {
                Location = new Point(10, titleBarHeight + 90),
                Size = new Point(this.Width - 20, 40),
                TextColor = Color.DarkRed,
                Font = GameService.Content.DefaultFont14,
                HorizontalAlignment = HorizontalAlignment.Center,
                Visible = false,
                Parent = this
            };

            _nameLabel = new Label()
            {
                Location = new Point(150, titleBarHeight + 40),
                Size = new Point(this.Width - 160, 40),
                TextColor = Color.DarkRed,
                Font = GameService.Content.DefaultFont14,
                HorizontalAlignment = HorizontalAlignment.Left,
                Visible = false,
                Parent = this
            };

            // Create content background image.
            _contentBackgroundImage = new Image(_contentBackgroundTexture)
            {
                Location = new Point(116, titleBarHeight + 10),
                Size = new Point(256, 128),
                Visible = false,
                Parent = this
            };
            // Create buttons.
            LeftButton = new StandardButton()
            {
                Location = new Point(107, this.Height - 50),
                Size = new Point(110, 30),
                Visible = false,
                Parent = this
            };

            RightButton = new StandardButton()
            {
                Location = new Point(269, this.Height - 50),
                Size = new Point(110, 30),
                Visible = false,
                Parent = this
            };

            MiddleButton = new StandardButton()
            {
                Location = new Point(169, this.Height - 50),
                Size = new Point(150, 30),
                Visible = false,
                Parent = this
            };

            // Attach basic button hover events.
            AttachButtonHoverEvents(LeftButton);
            AttachButtonHoverEvents(RightButton);
            AttachButtonHoverEvents(MiddleButton);
        }

        private void AttachButtonHoverEvents(StandardButton button)
        {
            button.MouseEntered += (s, e) => button.Opacity = 0.8f;
            button.MouseLeft += (s, e) => button.Opacity = 1.0f;
        }

        public void ShowLowerLabel(string text)
        {
            _lowerLabel.Text = text;
            _lowerLabel.Show();
        }

        public void ShowUpperLabel(string text)
        {
            _upperLabel.Text = text;
            _upperLabel.Show();
        }

        public void ShowName(string text)
        {
            _nameLabel.Text = text;
            _nameLabel.Show();
        }

        public void ShowLeftButton(string text)
        {
            LeftButton.Text = text;
            LeftButton.Show();
        }

        public void ShowRightButton(string text)
        {
            RightButton.Text = text;
            RightButton.Show();
        }

        public void ShowMiddleButton(string text)
        {
            MiddleButton.Text = text;
            MiddleButton.Show();
        }

        public void ShowBackgroundImage()
        {
            _contentBackgroundImage.Show();
        }

        public void HideBackgroundImage()
        {
            _contentBackgroundImage.Hide();
        }

        public new void Dispose()
        {
            // Dispose of all created controls.
            _titleLabel?.Dispose();
            _subtitleLabel?.Dispose();
            _upperLabel?.Dispose();
            _lowerLabel?.Dispose();
            _nameLabel?.Dispose();
            _contentBackgroundImage?.Dispose();
            LeftButton?.Dispose();
            RightButton?.Dispose();
            MiddleButton?.Dispose();
            _closeButton?.Dispose();
            _backgroundPanel?.Dispose();

            base.Dispose();
        }
    }
}
