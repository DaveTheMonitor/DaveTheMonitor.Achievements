using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StudioForge.Engine;
using StudioForge.Engine.Core;
using StudioForge.Engine.GUI;
using StudioForge.TotalMiner;
using StudioForge.TotalMiner.API;
using System.Collections.Generic;
using System.Text;

namespace DaveTheMonitor.Achievements.Screens
{
    internal sealed class AchievementsMenu : NewGuiMenu
    {
        private class AchievementWindow
        {
            private static readonly RenderProfile _iconRenderProfile;
            private static readonly RenderProfile _bgRenderProfile;
            private static readonly RenderProfile _lockedIconRenderProfile;
            private static readonly RenderProfile _lockedBgRenderProfile;
            private static Window.ColorProfile _enabledColors;
            private static Window.ColorProfile _disabledColors;
            private static Window.ColorProfile _progressBarColors;
            private ITMGame _game;
            private ITMPlayer _player;
            private Achievement _achievement;
            private Window _window;
            private Window _bgWindow;
            private Window _iconWindow;
            private ProgressBar _progressBar;
            private bool _unlocked;

            static AchievementWindow()
            {
                _iconRenderProfile = new RenderProfile()
                {
                    Blend = BlendState.NonPremultiplied,
                    DepthStencil = DepthStencilState.None,
                    Rasterizer = RasterizerState.CullNone,
                    Sampler = SamplerState.PointClamp,
                    Effect = null
                };

                _bgRenderProfile = new RenderProfile()
                {
                    Blend = BlendState.AlphaBlend,
                    DepthStencil = DepthStencilState.None,
                    Rasterizer = RasterizerState.CullNone,
                    Sampler = SamplerState.PointClamp,
                    Effect = null
                };

                _lockedIconRenderProfile = new RenderProfile()
                {
                    Blend = BlendState.NonPremultiplied,
                    DepthStencil = DepthStencilState.None,
                    Rasterizer = RasterizerState.CullNone,
                    Sampler = SamplerState.PointClamp,
                    Effect = AchievementsContent.Grayscale
                };

                _lockedBgRenderProfile = new RenderProfile()
                {
                    Blend = BlendState.AlphaBlend,
                    DepthStencil = DepthStencilState.None,
                    Rasterizer = RasterizerState.CullNone,
                    Sampler = SamplerState.PointClamp,
                    Effect = AchievementsContent.Grayscale
                };

                _enabledColors = Colors.ButtonColors.Clone();
                _enabledColors.BackHoverColor = _enabledColors.BackColor;
                _enabledColors.BackClickColor = _enabledColors.BackColor;

                _disabledColors = _enabledColors.Clone();
                _disabledColors.BackColor = _enabledColors.BackColor * 0.75f;
                _disabledColors.BackColor.A = 255;
                _disabledColors.BackHoverColor = _disabledColors.BackColor;
                _disabledColors.BackClickColor = _disabledColors.BackColor;

                _progressBarColors = ProgressBar.DefaultColorProfile.Clone();
                _progressBarColors.BackColor = _enabledColors.BackColor * 0.85f;
                _progressBarColors.BackColor.A = 255;
                _progressBarColors.BackHoverColor = _progressBarColors.BackColor;
                _progressBarColors.BackClickColor = _progressBarColors.BackColor;
                _progressBarColors.ForeColor = new Color(113, 240, 105);
            }

            public void SetPos(int x, int y)
            {
                _window.Position = new Vector2(x, y);
            }

            public void SetUnlocked(bool unlocked)
            {
                _unlocked = unlocked;
                UpdateWindows();
            }

            public void AddToCanvas(Canvas canvas)
            {
                canvas.AddChild(_window);
            }

            private void UpdateWindows()
            {
                if (_unlocked)
                {
                    _window.Colors = _enabledColors;
                    _bgWindow.Texture.TintColor = Color.White;
                    _bgWindow.RenderProfile = _bgRenderProfile;
                    _iconWindow.Texture.TintColor = Color.White;
                    _iconWindow.RenderProfile = _iconRenderProfile;
                }
                else
                {
                    _window.Colors = _disabledColors;
                    _bgWindow.Texture.TintColor = Color.White * 0.6f;
                    _bgWindow.Texture.TintColor.A = 255;
                    _bgWindow.RenderProfile = _lockedBgRenderProfile;
                    _iconWindow.Texture.TintColor = Color.White * 0.6f;
                    _iconWindow.Texture.TintColor.A = 255;
                    _iconWindow.RenderProfile = _lockedIconRenderProfile;
                }
                UpdateProgressBar();
            }

            private void UpdateProgressBar()
            {
                if (_progressBar == null)
                {
                    return;
                }

                if (_unlocked)
                {
                    _progressBar.IsVisible = false;
                    return;
                }

                float progress = _achievement.GetProgress(_game, _player, out string text);
                if (text == null)
                {
                    text = progress.ToString("P");
                }
                _progressBar.Text = text;
                _progressBar.progress = progress;
            }

            private void BuildWindows(AchievementManager achievementManager)
            {
                int margin = 8;
                int padding = 8;
                int x = padding;
                int y = padding;
                _bgWindow = new Window(x, y, 64, 64);
                _bgWindow.Colors = Window.TransparentColorProfile;
                _bgWindow.LoadTexture(achievementManager.BackgroundTexture);
                _bgWindow.Texture.SrRect = achievementManager.GetBackground(_achievement.Background);
                _window.AddChild(_bgWindow);

                _iconWindow = new Window(0, 0, 64, 64);
                _iconWindow.Colors = Window.TransparentColorProfile;
                _iconWindow.LoadTexture(achievementManager.IconTexture);
                _iconWindow.Texture.SrRect = achievementManager.GetIcon(_achievement.Icon);
                _bgWindow.AddChild(_iconWindow);

                x += 64 + margin;
                SpriteFont font = CoreGlobals.GameFont16;
                int maxWidth = _window.Size.X - x - padding;

                string name = GetName(font, maxWidth);
                Vector2 measure = font.MeasureString(name);
                TextBox nameLabel = new TextBox(name, x, y, maxWidth, (int)measure.Y, 1, WinTextAlignX.Left, WinTextAlignY.Center);
                nameLabel.Font = font;
                nameLabel.Colors = Colors.BlackText;
                _window.AddChild(nameLabel);
                y += (int)measure.Y;
                int maxDescHeight = _window.Size.Y - y - padding;

                if (_achievement.HasProgress)
                {
                    _progressBar = new ProgressBar(x, _window.Size.Y - 16 - margin, maxWidth, 16);
                    _progressBar.TextAlignX = WinTextAlignX.Center;
                    _progressBar.TextAlignY = WinTextAlignY.Center;
                    _progressBar.Colors = _progressBarColors;
                    _progressBar.BorderThickness = 2;
                    _progressBar.Font = CoreGlobals.GameFont12;
                    _window.AddChild(_progressBar);
                    maxDescHeight -= _progressBar.Size.Y + 4;
                }

                if (_achievement.Desc != null)
                {
                    font = CoreGlobals.GameFont12;
                    string desc = GetDesc(font, maxWidth, maxDescHeight);
                    TextBox descLabel = new TextBox(desc, x, y, maxWidth, maxDescHeight, 1, WinTextAlignX.Left, WinTextAlignY.Top);
                    descLabel.Font = font;
                    descLabel.Colors = Colors.BlackText;
                    _window.AddChild(descLabel);
                }

                UpdateWindows();
            }

            private string GetName(SpriteFont font, int maxWidth)
            {
                string name = _achievement.Name;
                if (font.MeasureString(name).X <= maxWidth)
                {
                    return name;
                }

                maxWidth -= (int)font.MeasureString("...").X;
                StringBuilder builder = new StringBuilder(name.Length);
                int w = 0;
                int i = 0;
                while (w < maxWidth)
                {
                    char c = name[i];
                    builder.Append(c);
                    w = (int)font.MeasureString(builder).X;
                    if (w > maxWidth)
                    {
                        builder.Remove(i, 1);
                        break;
                    }
                    i++;
                }

                builder.Append('.', 3);
                return builder.ToString();
            }

            private string GetDesc(SpriteFont font, int maxWidth, int maxHeight)
            {
                return Utils.InsertNewLines(font, maxWidth, 1, _achievement.Desc, true);
            }

            public AchievementWindow(ITMGame game, ITMPlayer player, AchievementManager achievementManager, Achievement achievement, int x, int y, int w, int h, bool unlocked)
            {
                _game = game;
                _player = player;
                _achievement = achievement;
                _window = new Window(x, y, w, h);
                _unlocked = unlocked;
                BuildWindows(achievementManager);
            }
        }

        public override string Name => "Achievements";
        private AchievementManager _achievementManager;
        private TextBox _headingLabel;
        private TextBox _unlockedLabel;
        private TextBox _lockedLabel;
        private List<AchievementWindow> _unlockedWindows;
        private List<AchievementWindow> _lockedWindows;
        private int _startHeight;
        private bool _unlockedVisible;
        private bool _lockedVisible;

        protected override void InitWindows()
        {
            base.InitWindows();
            InitMainContainer();
        }

        private void InitMainContainer()
        {
            int w = 500;
            int labelHeight = 34;
            int winHeight = 64 + 8 + 8;
            int margin = 4;
            int x = Viewport.Width / 2 - w / 2;
            int y = TopEdge;
            _startHeight = y;

            IReadOnlyList<Achievement> unlocked = _achievementManager.GetUnlockedAchievements(player);
            IReadOnlyList<Achievement> locked = _achievementManager.GetLockedAchievements(player);

            _headingLabel = new TextBox("Achievements", x, y, w, labelHeight);
            _headingLabel.Colors = Colors.Heading1;
            y += labelHeight + margin;

            if (unlocked.Count > 0)
            {
                _unlockedLabel = new TextBox("Unlocked Achievements", x, y, w, labelHeight);
                _unlockedLabel.Colors = Colors.LabelColors;

                Window toggle = new Window(w - labelHeight - 2 + 4, 2, labelHeight - 4, labelHeight - 4);
                toggle.Colors = Colors.ButtonGrayBorderColors;
                toggle.LoadTexture(InputManager.KeysTexture, true, false, 1);
                toggle.Texture.SrRect = new Rectangle(126, 0, 7, 8);
                toggle.Texture.TintColor = new Color(100, 100, 100, 255);
                SetTextureData(toggle, !_unlockedVisible);
                toggle.ClickHandler += ClickUnlockedCollapse;
                _unlockedLabel.AddChild(toggle);
                y += labelHeight + margin;

                foreach (Achievement achievement in unlocked)
                {
                    _unlockedWindows.Add(new AchievementWindow(game, player, _achievementManager, achievement, x, y, w, winHeight, true));
                }
            }

            if (locked.Count > 0)
            {
                _lockedLabel = new TextBox("Locked Achievements", x, y, w, labelHeight);
                _lockedLabel.Colors = Colors.LabelColors;

                Window toggle = new Window(w - labelHeight - 2 + 4, 2, labelHeight - 4, labelHeight - 4);
                toggle.Colors = Colors.ButtonGrayBorderColors;
                toggle.LoadTexture(InputManager.KeysTexture, true, false, 1);
                toggle.Texture.SrRect = new Rectangle(126, 0, 7, 8);
                toggle.Texture.TintColor = new Color(100, 100, 100, 255);
                SetTextureData(toggle, !_lockedVisible);
                toggle.ClickHandler += ClickLockedCollapse;
                _lockedLabel.AddChild(toggle);
                y += labelHeight + margin;

                foreach (Achievement achievement in locked)
                {
                    _lockedWindows.Add(new AchievementWindow(game, player, _achievementManager, achievement, x, y, w, winHeight, false));
                }
            }

            UpdateWindows();
        }

        private void UpdateWindows()
        {
            canvas.RemoveAllChildren();
            int w = 500;
            int labelHeight = 34;
            int winHeight = 64 + 8 + 8;
            int margin = 4;
            int x = Viewport.Width / 2 - w / 2;
            int y = _startHeight;

            // invisible window to allow scrolling past the items in the canvas
            Window scrollWindow = new Window(x, 0, w, _startHeight);
            scrollWindow.Colors = Window.TransparentColorProfile;
            canvas.AddChild(scrollWindow);

            _headingLabel.Position = new Vector2(x, TopEdge);
            canvas.AddChild(_headingLabel);
            y += labelHeight + margin;

            if (_unlockedWindows.Count > 0)
            {
                _unlockedLabel.Position = new Vector2(x, y);
                canvas.AddChild(_unlockedLabel);
                y += labelHeight + margin;

                if (_unlockedVisible)
                {
                    foreach (AchievementWindow window in _unlockedWindows)
                    {
                        window.SetPos(x, y);
                        window.AddToCanvas(canvas);
                        y += winHeight + margin;
                    }
                }

                y += labelHeight + margin;
            }

            if (_lockedWindows.Count > 0)
            {
                _lockedLabel.Position = new Vector2(x, y);
                canvas.AddChild(_lockedLabel);
                y += labelHeight + margin;

                if (_lockedVisible)
                {
                    foreach (AchievementWindow window in _lockedWindows)
                    {
                        window.SetPos(x, y);
                        window.AddToCanvas(canvas);
                        y += winHeight + margin;
                    }
                }
            }

            // invisible window to allow scrolling past the items in the canvas
            scrollWindow = new Window(x, y, w, _startHeight);
            scrollWindow.Colors = Window.TransparentColorProfile;
            canvas.AddChild(scrollWindow);
        }

        private void SetTextureData(Window toggle, bool collapsed)
        {
            if (!collapsed)
            {
                toggle.Texture.DestRect = new Rectangle((toggle.Size.X - 6) / 2, (toggle.Size.Y - 7) / 2, 7, 8);
                toggle.Texture.Rotation = 0;
            }
            else
            {
                toggle.Texture.DestRect = new Rectangle((toggle.Size.X - 7) / 2, (toggle.Size.Y + 8) / 2, 7, 8);
                toggle.Texture.Rotation = -MathHelper.PiOver2;
            }
        }

        private void ClickUnlockedCollapse(object sender, WindowEventArgs e)
        {
            SetTextureData((Window)sender, _unlockedVisible);
            _unlockedVisible = !_unlockedVisible;
            UpdateWindows();
        }

        private void ClickLockedCollapse(object sender, WindowEventArgs e)
        {
            SetTextureData((Window)sender, _lockedVisible);
            _lockedVisible = !_lockedVisible;
            UpdateWindows();
        }

        public AchievementsMenu(INewGuiMenuScreen screen, int tabId, ITMGame game, ITMPlayer player, AchievementManager achievementManager) : base(screen, tabId, game, player)
        {
            _achievementManager = achievementManager;
            _unlockedWindows = new List<AchievementWindow>();
            _lockedWindows = new List<AchievementWindow>();
            _unlockedVisible = true;
            _lockedVisible = true;
        }
    }
}
