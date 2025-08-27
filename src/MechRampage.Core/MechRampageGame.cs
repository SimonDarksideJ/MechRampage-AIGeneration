using System;
using MechRampage.Core.Localization;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static System.Net.Mime.MediaTypeNames;
using MechRampage.Core.World;
using MechRampage.Core.Rendering;

namespace MechRampage.Core
{
    /// <summary>
    /// The main class for the game, responsible for managing game components, settings, 
    /// and platform-specific configurations.
    /// </summary>
    public class MechRampageGame : Game
    {
        // Resources for drawing.
        private GraphicsDeviceManager graphicsDeviceManager;

        /// <summary>
        /// Indicates if the game is running on a mobile platform.
        /// </summary>
        public readonly static bool IsMobile = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();

        /// <summary>
        /// Indicates if the game is running on a desktop platform.
        /// </summary>
        public readonly static bool IsDesktop = OperatingSystem.IsMacOS() || OperatingSystem.IsLinux() || OperatingSystem.IsWindows();

        /// <summary>
        /// Initializes a new instance of the game. Configures platform-specific settings, 
        /// initializes services like settings and leaderboard managers, and sets up the 
        /// screen manager for screen transitions.
        /// </summary>
        public MechRampageGame()
        {
            graphicsDeviceManager = new GraphicsDeviceManager(this);

            // Share GraphicsDeviceManager as a service.
            Services.AddService(typeof(GraphicsDeviceManager), graphicsDeviceManager);

            Content.RootDirectory = "Content";

            // Configure screen orientations.
            graphicsDeviceManager.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;
            // Enable system mouse cursor
            IsMouseVisible = true;
        }

        /// <summary>
        /// Initializes the game, including setting up localization and adding the 
        /// initial screens to the ScreenManager.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            // Load supported languages and set the default language.
            List<CultureInfo> cultures = LocalizationManager.GetSupportedCultures();
            var languages = new List<CultureInfo>();
            for (int i = 0; i < cultures.Count; i++)
            {
                languages.Add(cultures[i]);
            }

            // TODO You should load this from a settings file or similar,
            // based on what the user or operating system selected.
            var selectedLanguage = LocalizationManager.DEFAULT_CULTURE_CODE;
            LocalizationManager.SetCulture(selectedLanguage);
        }

        /// <summary>
        /// Loads game content, such as textures and particle systems.
        /// </summary>
        protected override void LoadContent()
        {
            base.LoadContent();

            // Initialize core rendering helpers.
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Create the initial scene (arena + lighting + props).
            _scene = SceneFactory.CreateDefaultArena(GraphicsDevice);
            _sceneRenderer = new SceneRenderer(GraphicsDevice, _spriteBatch)
            {
                DebugOverlayEnabled = DebugSettings.OverlayEnabledDefault,
                OverlayMode = DebugSettings.OverlayModeDefault,
                FillAttackRanges = DebugSettings.FillAttackRangesDefault
            };

            // Load HUD font (placeholder). Falls back if missing.
            try
            {
                _hudFont = Content.Load<SpriteFont>("Fonts/Hud");
            }
            catch { }

            // Wire mech projectile spawn callback.
            Entities.Mech.SpawnProjectile = (mech, enemy, dmg) =>
            {
                if (_scene == null) return;
                var proj = new World.Projectile(mech.Position + new Vector3(0, 1.2f, 0), enemy, dmg);
                _scene.Projectiles.Add(proj);
            };
        }

        /// <summary>
        /// Updates the game's logic, called once per frame.
        /// </summary>
        /// <param name="gameTime">
        /// Provides a snapshot of timing values used for game updates.
        /// </param>
        protected override void Update(GameTime gameTime)
        {
            // Exit the game if the Back button (GamePad) or Escape key (Keyboard) is pressed.
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
                || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // Future: hooks for animations, entity updates, procedural changes.
            _scene?.Update(gameTime);
            if (_scene != null && _sceneRenderer != null)
            {
                _sceneRenderer.UpdateCamera(gameTime, _scene);
                HandleMouseInput(gameTime);
            }

            // FPS accumulation
            _frameCounter++;
            _fpsTimer += gameTime.ElapsedGameTime.TotalSeconds;
            if (_fpsTimer >= 1.0)
            {
                _currentFps = _frameCounter;
                _frameCounter = 0;
                _fpsTimer -= 1.0;
            }

            // Overlay toggles
            var kb = Keyboard.GetState();
            if (kb.IsKeyDown(Keys.F3) && !_lastKeyboard.IsKeyDown(Keys.F3))
            {
                if (_sceneRenderer != null) _sceneRenderer.DebugOverlayEnabled = !_sceneRenderer.DebugOverlayEnabled;
            }
            if (kb.IsKeyDown(Keys.F4) && !_lastKeyboard.IsKeyDown(Keys.F4))
            {
                if (_sceneRenderer != null)
                {
                    var mode = _sceneRenderer.OverlayMode;
                    mode = (Rendering.DebugOverlayMode)(((int)mode + 1) % 3);
                    _sceneRenderer.OverlayMode = mode;
                }
            }
            if (kb.IsKeyDown(Keys.F5) && !_lastKeyboard.IsKeyDown(Keys.F5))
            {
                if (_sceneRenderer != null) _sceneRenderer.FillAttackRanges = !_sceneRenderer.FillAttackRanges;
            }
            if (kb.IsKeyDown(Keys.F1) && !_lastKeyboard.IsKeyDown(Keys.F1))
            {
                _showHelp = !_showHelp;
            }
            if (kb.IsKeyDown(Keys.Tab) && !_lastKeyboard.IsKeyDown(Keys.Tab))
            {
                _miniMapSizeIndex = (_miniMapSizeIndex + 1) % _miniMapSizes.Length;
            }
            if (kb.IsKeyDown(Keys.C) && !_lastKeyboard.IsKeyDown(Keys.C))
            {
                Vector3 sum = Vector3.Zero; int count = 0;
                foreach (var m in _scene.Mechs) if (m.Selected) { sum += m.Position; count++; }
                if (count == 0) { foreach (var m in _scene.Mechs) { sum += m.Position; count++; } }
                if (count > 0) _pendingCameraTweenTarget = sum / count;
            }
            if (kb.IsKeyDown(Keys.OemPlus) && !_lastKeyboard.IsKeyDown(Keys.OemPlus))
            {
                _miniMapZoomLevel = Math.Min(_miniMapZoomLevel + 1, _miniMapZoomScales.Length - 1);
            }
            if (kb.IsKeyDown(Keys.OemMinus) && !_lastKeyboard.IsKeyDown(Keys.OemMinus))
            {
                _miniMapZoomLevel = Math.Max(_miniMapZoomLevel - 1, 0);
            }
            if (kb.IsKeyDown(Keys.PageUp) && !_lastKeyboard.IsKeyDown(Keys.PageUp))
            {
                _cameraTweenSpeed = Math.Min(MaxTweenSpeed, _cameraTweenSpeed + 0.02f);
            }
            if (kb.IsKeyDown(Keys.PageDown) && !_lastKeyboard.IsKeyDown(Keys.PageDown))
            {
                _cameraTweenSpeed = Math.Max(MinTweenSpeed, _cameraTweenSpeed - 0.02f);
            }
            if (kb.IsKeyDown(Keys.U) && !_lastKeyboard.IsKeyDown(Keys.U)) _showUpgradePanel = !_showUpgradePanel;
            if (kb.IsKeyDown(Keys.F9) && !_lastKeyboard.IsKeyDown(Keys.F9)) TrySave();
            if (kb.IsKeyDown(Keys.F10) && !_lastKeyboard.IsKeyDown(Keys.F10)) TryLoad();
            UpdateCameraTween();
            _lastKeyboard = kb;

            base.Update(gameTime);
        }

        /// <summary>
        /// Draws the game's graphics, called once per frame.
        /// </summary>
        /// <param name="gameTime">
        /// Provides a snapshot of timing values used for rendering.
        /// </param>
        protected override void Draw(GameTime gameTime)
        {
            // Clears the screen with the MonoGame orange color before drawing.
            GraphicsDevice.Clear(Color.MonoGameOrange);

            if (_scene != null && _sceneRenderer != null)
            {
                _sceneRenderer.Draw(_scene, gameTime);
            }

            if (_hudFont != null && _scene != null)
            {
                _spriteBatch.Begin();
                int y = 10;
                foreach (var mech in _scene.Mechs)
                {
                    if (mech.Selected)
                    {
                        var p = mech.Position;
                        _spriteBatch.DrawString(_hudFont, $"{mech.Name} HP:{mech.Health} Pos:({p.X:0.0},{p.Y:0.0},{p.Z:0.0})", new Vector2(10, y), Color.White);
                        y += 18;
                    }
                }
                _spriteBatch.DrawString(_hudFont, $"Resources: {_scene.GlobalResources}", new Vector2(10, y + 10), Color.Cyan);
                if (_sceneRenderer != null)
                {
                    string overlay = _sceneRenderer.DebugOverlayEnabled ? $"Overlay: {_sceneRenderer.OverlayMode} (F3/F4/F5)" : "Overlay: Off (F3)";
                    _spriteBatch.DrawString(_hudFont, overlay, new Vector2(10, y + 32), Color.LightGray);
                    if (DebugSettings.ShowFps)
                        _spriteBatch.DrawString(_hudFont, $"FPS: {_currentFps}", new Vector2(10, y + 50), Color.LightGreen);
                    if (_showHelp)
                    {
                        int helpBase = y + 70;
                        _spriteBatch.DrawString(_hudFont, "F1 Help  U Upgrades  F9 Save  F10 Load", new Vector2(10, helpBase), Color.Wheat);
                        _spriteBatch.DrawString(_hudFont, "Tab MiniMap Size  +/- Zoom  PgUp/PgDn Tween  C Focus", new Vector2(10, helpBase + 18), Color.Wheat);
                        _spriteBatch.DrawString(_hudFont, "Drag LMB Marquee  LMB Select  RMB Move/Attack  Shift:Add/Instant", new Vector2(10, helpBase + 36), Color.Wheat);
                    }
                }
                DrawMiniMap();
                if (_showUpgradePanel) DrawUpgradePanel();
                if (!string.IsNullOrEmpty(_lastSaveStatus))
                {
                    _spriteBatch.DrawString(_hudFont, _lastSaveStatus, new Vector2(10, GraphicsDevice.Viewport.Height - 28), Color.LightSkyBlue);
                }

                // Health bars & damage text (project world to screen)
                if (_sceneRenderer != null)
                {
                    // Enemies
                    foreach (var enemy in _scene.Enemies)
                    {
                        if (!enemy.Alive) continue;
                        DrawHealthBar(enemy.Position + new Vector3(0, 3.2f, 0), enemy.Health / 150f);
                    }
                    // Mechs
                    foreach (var mech in _scene.Mechs)
                    {
                        DrawHealthBar(mech.Position + new Vector3(0, 2.4f, 0), mech.Health / 100f);
                    }
                    // Damage texts
                    foreach (var dt in _scene.DamageTexts)
                    {
                        var screen = _sceneRenderer.ProjectToScreen(dt.WorldPosition);
                        var color = Color.Lerp(Color.Transparent, Color.OrangeRed, dt.Opacity);
                        _spriteBatch.DrawString(_hudFont, dt.Text, screen, color);
                    }
                }
                _spriteBatch.End();
            }

            // Overlay selection rectangle after HUD so it's visible
            if (_isDragSelecting && _spriteBatch != null)
            {
                _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                DrawSelectionRectangle();
                _spriteBatch.End();
            }

            base.Draw(gameTime);
        }

        #region Private Fields
        private SpriteBatch _spriteBatch;
        private GameScene _scene;
        private SceneRenderer _sceneRenderer;
        private MouseState _lastMouse;
        private KeyboardState _lastKeyboard;
        private SpriteFont _hudFont;
        private Texture2D _whiteTex;
        private bool _isDragSelecting;
        private Point _dragStart;
        private Point _dragCurrent;
        private int _frameCounter;
        private double _fpsTimer;
        private int _currentFps;
        private bool _showHelp = DebugSettings.ShowHelpDefault;
        private bool _showUpgradePanel;
        private List<Upgrade> _upgrades = UpgradeCatalog.CreateDefaults();
        private string _lastSaveStatus;
        private bool _minimapDragging;
        private int _miniMapSizeIndex = 0;
        private static readonly int[] _miniMapSizes = new[] { 140, 200, 260 };
        private Vector3? _pendingCameraTweenTarget;
        private int _miniMapZoomLevel = 0; // minimap zoom index
        private static readonly float[] _miniMapZoomScales = new[] { 1.0f, 0.6f, 0.35f, 0.2f }; // fraction of world displayed
        private float _cameraTweenSpeed = 0.12f; // adjustable lerp factor
        private const float MinTweenSpeed = 0.02f;
        private const float MaxTweenSpeed = 0.4f;

        private void HandleMouseInput(GameTime gameTime)
        {
            var mouse = Mouse.GetState();
            if (_scene == null || _sceneRenderer == null) { _lastMouse = mouse; return; }
            // Mouse wheel zoom for minimap
            var miniMapRect = GetMiniMapRect();
            int wheelDelta = mouse.ScrollWheelValue - _lastMouse.ScrollWheelValue;
            if (miniMapRect.Contains(mouse.Position) && wheelDelta != 0)
            {
                if (wheelDelta > 0) _miniMapZoomLevel = Math.Min(_miniMapZoomLevel + 1, _miniMapZoomScales.Length - 1);
                else _miniMapZoomLevel = Math.Max(_miniMapZoomLevel - 1, 0);
            }

            bool rightClick = mouse.RightButton == ButtonState.Pressed && _lastMouse.RightButton == ButtonState.Released;

            // Drag edges
            bool leftPressedEdge = mouse.LeftButton == ButtonState.Pressed && _lastMouse.LeftButton == ButtonState.Released;
            bool leftReleasedEdge = mouse.LeftButton == ButtonState.Released && _lastMouse.LeftButton == ButtonState.Pressed;

            if (leftPressedEdge)
            {
                if (miniMapRect.Contains(mouse.Position))
                {
                    float worldSize = _scene.GridSize * _scene.TileWorldSize;
                    float nx = (mouse.X - miniMapRect.X) / (float)miniMapRect.Width;
                    float nz = (mouse.Y - miniMapRect.Y) / (float)miniMapRect.Height;
                    var target = new Vector3(nx * worldSize, 0, nz * worldSize);
                    if (Keyboard.GetState().IsKeyDown(Keys.LeftShift) || Keyboard.GetState().IsKeyDown(Keys.RightShift))
                        _sceneRenderer.CenterCamera(target);
                    else
                        _pendingCameraTweenTarget = target;
                    _minimapDragging = true; // enable continuous pan while held
                }
                else
                {
                    _dragStart = new Point(mouse.X, mouse.Y);
                    _dragCurrent = _dragStart;
                    _isDragSelecting = false;
                }
            }
            if (mouse.LeftButton == ButtonState.Pressed)
            {
                if (_minimapDragging)
                {
                    if (miniMapRect.Contains(mouse.Position))
                    {
                        float worldSize = _scene.GridSize * _scene.TileWorldSize;
                        float nx = MathHelper.Clamp((mouse.X - miniMapRect.X) / (float)miniMapRect.Width, 0f, 1f);
                        float nz = MathHelper.Clamp((mouse.Y - miniMapRect.Y) / (float)miniMapRect.Height, 0f, 1f);
                        var target = new Vector3(nx * worldSize, 0, nz * worldSize);
                        if (Keyboard.GetState().IsKeyDown(Keys.LeftShift) || Keyboard.GetState().IsKeyDown(Keys.RightShift))
                            _sceneRenderer.CenterCamera(target);
                        else
                            _pendingCameraTweenTarget = target;
                    }
                }
                _dragCurrent = new Point(mouse.X, mouse.Y);
                if (!_isDragSelecting)
                {
                    int dx = _dragCurrent.X - _dragStart.X;
                    int dy = _dragCurrent.Y - _dragStart.Y;
                    if (dx * dx + dy * dy > 36) _isDragSelecting = true; // threshold
                }
            }
            if (leftReleasedEdge)
            {
                _minimapDragging = false;
                var ray = _sceneRenderer.GetPickRay(new Point(mouse.X, mouse.Y));
                if (_isDragSelecting)
                {
                    var rect = GetDragRectangle();
                    bool addMode = Keyboard.GetState().IsKeyDown(Keys.LeftShift) || Keyboard.GetState().IsKeyDown(Keys.RightShift);
                    if (!addMode) foreach (var m in _scene.Mechs) m.Selected = false;
                    foreach (var mech in _scene.Mechs)
                    {
                        var screen = _sceneRenderer.ProjectToScreen(mech.Position + new Vector3(0, 1f, 0));
                        if (rect.Contains(screen)) mech.Selected = true;
                    }
                }
                else
                {
                    bool addMode = Keyboard.GetState().IsKeyDown(Keys.LeftShift) || Keyboard.GetState().IsKeyDown(Keys.RightShift);
                    bool selectedAny = false;
                    float nearest = float.MaxValue; Entities.Mech picked = null;
                    foreach (var mech in _scene.Mechs)
                    {
                        var hit = mech.RayIntersect(ray);
                        if (hit.HasValue && hit.Value < nearest)
                        {
                            nearest = hit.Value; picked = mech;
                        }
                    }

                    if (picked != null)
                    {
                        if (!addMode) foreach (var m in _scene.Mechs) m.Selected = false;
                        picked.Selected = true; selectedAny = true;
                    }
                    if (!selectedAny)
                    {
                        // Try resource node interaction (requires already-selected mech in range)
                        float rNearest = float.MaxValue; Entities.ResourceNode resNode = null;
                        foreach (var node in _scene.ResourceNodes)
                        {
                            if (node.Collected) continue;
                            var hitR = node.RayIntersect(ray);
                            if (hitR.HasValue && hitR.Value < rNearest)
                            {
                                rNearest = hitR.Value; resNode = node;
                            }
                        }
                        if (resNode != null)
                        {
                            foreach (var mech in _scene.Mechs)
                            {
                                if (!mech.Selected) continue;
                                var diff = mech.Position - resNode.Position; diff.Y = 0;
                                if (diff.Length() <= 5f)
                                {
                                    resNode.Collected = true;
                                    _scene.GlobalResources += resNode.Value;
                                    _scene.DamageTexts.Add(new World.DamageText(resNode.Position + new Vector3(0, 2f, 0), $"+{resNode.Value}"));
                                    break;
                                }
                            }
                        }
                        else if (!addMode)
                        {
                            // Empty ground click clears selection
                            foreach (var mech in _scene.Mechs) mech.Selected = false;
                        }
                    }
                }
                _isDragSelecting = false;
            }

            if (rightClick)
            {
                // Minimap right-click move order
                if (miniMapRect.Contains(mouse.Position))
                {
                    float worldSize = _scene.GridSize * _scene.TileWorldSize;
                    float nx = (mouse.X - miniMapRect.X) / (float)miniMapRect.Width;
                    float nz = (mouse.Y - miniMapRect.Y) / (float)miniMapRect.Height;
                    var point = new Vector3(nx * worldSize, 0, nz * worldSize);
                    var selected = _scene.Mechs.FindAll(m => m.Selected);
                    for (int i = 0; i < selected.Count; i++)
                    {
                        var mech = selected[i];
                        float angle = MathHelper.TwoPi * (i / (float)Math.Max(1, selected.Count));
                        var offset = new Vector3(MathF.Cos(angle), 0, MathF.Sin(angle)) * 3.0f;
                        var dest = new Vector3(point.X, 0, point.Z) + offset;
                        var startGrid = Pathfinding.GridPathfinder.ToGrid(mech.Position, _scene.TileWorldSize);
                        var endGrid = Pathfinding.GridPathfinder.ToGrid(dest, _scene.TileWorldSize);
                        var path = Pathfinding.GridPathfinder.FindPath(startGrid, endGrid, _scene.GridSize, _scene.TileWorldSize);
                        mech.IssuePath(path);
                    }
                    _lastMouse = mouse; return; // consume
                }
                var ray = _sceneRenderer.GetPickRay(new Point(mouse.X, mouse.Y));
                // Attack target check
                float eNearest = float.MaxValue; Entities.Enemy targetEnemy = null;
                foreach (var enemy in _scene.Enemies)
                {
                    var hitE = enemy.RayIntersect(ray);
                    if (hitE.HasValue && hitE.Value < eNearest)
                    {
                        eNearest = hitE.Value; targetEnemy = enemy;
                    }
                }
                if (targetEnemy != null)
                {
                    foreach (var mech in _scene.Mechs) if (mech.Selected) mech.CurrentTarget = targetEnemy;
                }
                else
                {
                    float? t = RayIntersectsPlaneY(ray, 0f);
                    if (t.HasValue)
                    {
                        var point = ray.Position + ray.Direction * t.Value;
                        var selected = _scene.Mechs.FindAll(m => m.Selected);
                        for (int i = 0; i < selected.Count; i++)
                        {
                            var mech = selected[i];
                            float angle = MathHelper.TwoPi * (i / (float)Math.Max(1, selected.Count));
                            var offset = new Vector3(MathF.Cos(angle), 0, MathF.Sin(angle)) * 3.0f;
                            var dest = new Vector3(point.X, 0, point.Z) + offset;
                            var startGrid = Pathfinding.GridPathfinder.ToGrid(mech.Position, _scene.TileWorldSize);
                            var endGrid = Pathfinding.GridPathfinder.ToGrid(dest, _scene.TileWorldSize);
                            var path = Pathfinding.GridPathfinder.FindPath(startGrid, endGrid, _scene.GridSize, _scene.TileWorldSize);
                            mech.IssuePath(path);
                        }
                    }
                }
            }

            // Hover highlighting
            var hoverRay = _sceneRenderer.GetPickRay(new Point(mouse.X, mouse.Y));
            float hNearest = float.MaxValue; Entities.Mech hover = null;
            foreach (var mech in _scene.Mechs)
            {
                var hit = mech.RayIntersect(hoverRay);
                mech.Hovered = false;
                if (hit.HasValue && hit.Value < hNearest)
                {
                    hNearest = hit.Value; hover = mech;
                }
            }
            if (hover != null && !hover.Selected) hover.Hovered = true;

            _lastMouse = mouse;
        }

        private static float? RayIntersectsPlaneY(Ray ray, float planeY)
        {
            if (ray.Direction.Y == 0) return null;
            float t = (planeY - ray.Position.Y) / ray.Direction.Y;
            if (t < 0) return null;
            return t;
        }

        private Rectangle GetDragRectangle()
        {
            int x1 = System.Math.Min(_dragStart.X, _dragCurrent.X);
            int y1 = System.Math.Min(_dragStart.Y, _dragCurrent.Y);
            int x2 = System.Math.Max(_dragStart.X, _dragCurrent.X);
            int y2 = System.Math.Max(_dragStart.Y, _dragCurrent.Y);
            return new Rectangle(x1, y1, x2 - x1, y2 - y1);
        }

        private void DrawSelectionRectangle()
        {
            if (!_isDragSelecting) return;
            if (_whiteTex == null)
            {
                _whiteTex = new Texture2D(GraphicsDevice, 1, 1);
                _whiteTex.SetData(new[] { Color.White });
            }
            var rect = GetDragRectangle();
            // outline thickness 2
            _spriteBatch.Draw(_whiteTex, rect, Color.CornflowerBlue * 0.15f);
            // Top
            _spriteBatch.Draw(_whiteTex, new Rectangle(rect.X, rect.Y, rect.Width, 2), Color.CornflowerBlue);
            // Bottom
            _spriteBatch.Draw(_whiteTex, new Rectangle(rect.X, rect.Bottom - 2, rect.Width, 2), Color.CornflowerBlue);
            // Left
            _spriteBatch.Draw(_whiteTex, new Rectangle(rect.X, rect.Y, 2, rect.Height), Color.CornflowerBlue);
            // Right
            _spriteBatch.Draw(_whiteTex, new Rectangle(rect.Right - 2, rect.Y, 2, rect.Height), Color.CornflowerBlue);
        }

        private void DrawHealthBar(Vector3 worldPos, float normalized)
        {
            if (_sceneRenderer == null) return;
            if (_whiteTex == null)
            {
                _whiteTex = new Texture2D(GraphicsDevice, 1, 1);
                _whiteTex.SetData(new[] { Color.White });
            }
            var screen = _sceneRenderer.ProjectToScreen(worldPos);
            const float barWidth = 60f;
            const float barHeight = 6f;
            var bg = new Rectangle((int)(screen.X - barWidth / 2), (int)(screen.Y - barHeight / 2), (int)barWidth, (int)barHeight);
            var fg = new Rectangle(bg.X + 1, bg.Y + 1, (int)((barWidth - 2) * MathHelper.Clamp(normalized, 0f, 1f)), (int)(barHeight - 2));
            _spriteBatch.Draw(_whiteTex, bg, Color.Black * 0.6f);
            _spriteBatch.Draw(_whiteTex, fg, Color.LimeGreen);
        }

        private void DrawMiniMap()
        {
            if (_scene == null) return;
            if (_whiteTex == null)
            {
                _whiteTex = new Texture2D(GraphicsDevice, 1, 1);
                _whiteTex.SetData(new[] { Color.White });
            }
            var rect = GetMiniMapRect();
            _spriteBatch.Draw(_whiteTex, rect, Color.Black * 0.4f);
            float worldSize = _scene.GridSize * _scene.TileWorldSize;
            float zoomScale = _miniMapZoomScales[_miniMapZoomLevel];
            Vector3 camCenter = _sceneRenderer != null ? _sceneRenderer.CameraTarget : new Vector3(worldSize * 0.5f, 0, worldSize * 0.5f);
            float halfWindow = (worldSize * zoomScale) * 0.5f;
            var windowMin = new Vector3(MathHelper.Clamp(camCenter.X - halfWindow, 0, worldSize), 0, MathHelper.Clamp(camCenter.Z - halfWindow, 0, worldSize));
            var windowMax = new Vector3(MathHelper.Clamp(camCenter.X + halfWindow, 0, worldSize), 0, MathHelper.Clamp(camCenter.Z + halfWindow, 0, worldSize));
            float windowWidth = Math.Max(1f, windowMax.X - windowMin.X);
            float windowDepth = Math.Max(1f, windowMax.Z - windowMin.Z);
            // Draw full world bounds as faint border if zoomed in
            if (_miniMapZoomLevel > 0)
            {
                _spriteBatch.Draw(_whiteTex, new Rectangle(rect.X, rect.Y, rect.Width, 2), Color.White * 0.18f); // Top
                _spriteBatch.Draw(_whiteTex, new Rectangle(rect.X, rect.Bottom - 2, rect.Width, 2), Color.White * 0.18f); // Bottom
                _spriteBatch.Draw(_whiteTex, new Rectangle(rect.X, rect.Y, 2, rect.Height), Color.White * 0.18f); // Left
                _spriteBatch.Draw(_whiteTex, new Rectangle(rect.Right - 2, rect.Y, 2, rect.Height), Color.White * 0.18f); // Right
                // Draw current zoom window rectangle
                float left = rect.X + (windowMin.X / worldSize) * rect.Width;
                float top = rect.Y + (windowMin.Z / worldSize) * rect.Height;
                float right = rect.X + (windowMax.X / worldSize) * rect.Width;
                float bottom = rect.Y + (windowMax.Z / worldSize) * rect.Height;
                int winW = Math.Max(2, (int)(right - left));
                int winH = Math.Max(2, (int)(bottom - top));
                _spriteBatch.Draw(_whiteTex, new Rectangle((int)left, (int)top, winW, 2), Color.Yellow * 0.22f); // Top
                _spriteBatch.Draw(_whiteTex, new Rectangle((int)left, (int)bottom - 2, winW, 2), Color.Yellow * 0.22f); // Bottom
                _spriteBatch.Draw(_whiteTex, new Rectangle((int)left, (int)top, 2, winH), Color.Yellow * 0.22f); // Left
                _spriteBatch.Draw(_whiteTex, new Rectangle((int)right - 2, (int)top, 2, winH), Color.Yellow * 0.22f); // Right
            }
            // Viewport frustum footprint (approx by projecting screen corners onto ground plane)
            if (_sceneRenderer != null)
            {
                Point[] screenCorners = new[] { new Point(0, 0), new Point(GraphicsDevice.Viewport.Width, 0), new Point(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height), new Point(0, GraphicsDevice.Viewport.Height) };
                var pts = new List<Vector2>(4);
                foreach (var sc in screenCorners)
                {
                    var ray = _sceneRenderer.GetPickRay(sc);
                    var t = RayIntersectsPlaneY(ray, 0f);
                    if (t.HasValue)
                    {
                        var world = ray.Position + ray.Direction * t.Value;
                        if (world.X < windowMin.X || world.X > windowMax.X || world.Z < windowMin.Z || world.Z > windowMax.Z) continue;
                        float mx = rect.X + ((world.X - windowMin.X) / windowWidth) * rect.Width;
                        float mz = rect.Y + ((world.Z - windowMin.Z) / windowDepth) * rect.Height;
                        pts.Add(new Vector2(mx, mz));
                    }
                }
                if (pts.Count == 4)
                {
                    DrawMiniMapLine(pts[0], pts[1], Color.White * 0.55f);
                    DrawMiniMapLine(pts[1], pts[2], Color.White * 0.55f);
                    DrawMiniMapLine(pts[2], pts[3], Color.White * 0.55f);
                    DrawMiniMapLine(pts[3], pts[0], Color.White * 0.55f);
                }
            }
            void Plot(Vector3 p, Color c)
            {
                if (p.X < windowMin.X || p.X > windowMax.X || p.Z < windowMin.Z || p.Z > windowMax.Z) return;
                var px = rect.X + ((p.X - windowMin.X) / windowWidth) * rect.Width;
                var py = rect.Y + ((p.Z - windowMin.Z) / windowDepth) * rect.Height;
                _spriteBatch.Draw(_whiteTex, new Rectangle((int)px - 2, (int)py - 2, 4, 4), c);
            }
            foreach (var m in _scene.Mechs) Plot(m.Position, m.Selected ? Color.Yellow : Color.Green);
            foreach (var e in _scene.Enemies) if (e.Alive) Plot(e.Position, Color.Red);
            foreach (var r in _scene.ResourceNodes) if (!r.Collected) Plot(r.Position, Color.Cyan);
            _spriteBatch.DrawString(_hudFont, "Map", new Vector2(rect.X + 4, rect.Y + 4), Color.White);
            // Camera target marker
            if (_sceneRenderer != null)
            {
                var camT = _sceneRenderer.CameraTarget;
                if (camT.X >= windowMin.X && camT.X <= windowMax.X && camT.Z >= windowMin.Z && camT.Z <= windowMax.Z)
                {
                    var px = rect.X + ((camT.X - windowMin.X) / windowWidth) * rect.Width;
                    var py = rect.Y + ((camT.Z - windowMin.Z) / windowDepth) * rect.Height;
                    _spriteBatch.Draw(_whiteTex, new Rectangle((int)px - 4, (int)py - 4, 8, 8), Color.Orange * 0.9f);
                }
                if (_miniMapZoomLevel > 0)
                {
                    _spriteBatch.DrawString(_hudFont, $"Z{_miniMapZoomLevel + 1}/{_miniMapZoomScales.Length}", new Vector2(rect.Right - 54, rect.Y + 4), Color.LightGoldenrodYellow);
                }
            }
            // Hover marker on minimap
            var mouse = Mouse.GetState();
            if (rect.Contains(mouse.Position))
            {
                float nx = (mouse.X - rect.X) / (float)rect.Width;
                float nz = (mouse.Y - rect.Y) / (float)rect.Height;
                var hx = rect.X + nx * rect.Width;
                var hz = rect.Y + nz * rect.Height;
                _spriteBatch.Draw(_whiteTex, new Rectangle((int)hx - 3, (int)hz - 3, 6, 6), Color.White * 0.5f);
            }
        }

        private void DrawUpgradePanel()
        {
            if (_whiteTex == null)
            {
                _whiteTex = new Texture2D(GraphicsDevice, 1, 1);
                _whiteTex.SetData(new[] { Color.White });
            }
            var vp = GraphicsDevice.Viewport;
            var panel = new Rectangle(vp.Width - 260, 170, 250, 210);
            _spriteBatch.Draw(_whiteTex, panel, Color.Black * 0.7f);
            _spriteBatch.DrawString(_hudFont, "Upgrades (U)", new Vector2(panel.X + 10, panel.Y + 8), Color.White);
            int y = panel.Y + 36;
            for (int i = 0; i < _upgrades.Count; i++)
            {
                var up = _upgrades[i];
                var cost = up.CurrentCost;
                var afford = _scene.GlobalResources >= cost;
                var lineColor = afford ? Color.LimeGreen : Color.IndianRed;
                _spriteBatch.DrawString(_hudFont, $"{i + 1}. {up.Name} L{up.Level} Cost:{cost}", new Vector2(panel.X + 10, y), lineColor);
                y += 16;
                _spriteBatch.DrawString(_hudFont, up.Description, new Vector2(panel.X + 18, y), Color.LightGray);
                y += 22;
            }

            // Input: numeric keys 1..9 to buy
            var kb = Keyboard.GetState();
            for (int i = 0; i < _upgrades.Count && i < 9; i++)
            {
                var key = Keys.D1 + i;
                if (kb.IsKeyDown(key) && !_lastKeyboard.IsKeyDown(key))
                {
                    AttemptPurchase(_upgrades[i]);
                }
            }
        }

        private void AttemptPurchase(Upgrade up)
        {
            int cost = up.CurrentCost;
            if (_scene.GlobalResources < cost) { _lastSaveStatus = "Not enough resources"; return; }
            foreach (var mech in _scene.Mechs)
            {
                up.ApplyEffect?.Invoke(mech);
            }
            _scene.GlobalResources -= cost;
            up.Level++;
            _lastSaveStatus = $"Purchased {up.Name} L{up.Level}";
        }

        private void TrySave()
        {
            if (_scene == null) return;
            try
            {
                var snap = _scene.ToSnapshot();
                var save = new SaveData
                {
                    Scene = snap,
                    OverlayEnabled = _sceneRenderer?.DebugOverlayEnabled ?? false,
                    OverlayMode = _sceneRenderer?.OverlayMode ?? Rendering.DebugOverlayMode.Off,
                    FillAttackRanges = _sceneRenderer?.FillAttackRanges ?? false,
                    CameraTargetX = _sceneRenderer?.CameraTarget.X ?? 0f,
                    CameraTargetZ = _sceneRenderer?.CameraTarget.Z ?? 0f,
                    CameraZoom = _sceneRenderer?.CameraZoom ?? 1f,
                    MiniMapSizeIndex = _miniMapSizeIndex,
                    MiniMapZoomLevel = _miniMapZoomLevel,
                    CameraTweenLerpFactor = _cameraTweenSpeed
                };
                foreach (var up in _upgrades)
                {
                    save.Upgrades.Add(new UpgradeLevel { Id = up.Id, Level = up.Level });
                }
                var json = System.Text.Json.JsonSerializer.Serialize(save, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText("scene-save.json", json);
                _lastSaveStatus = $"Saved {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                _lastSaveStatus = "Save failed: " + ex.Message;
            }
        }

        private void TryLoad()
        {
            try
            {
                if (!System.IO.File.Exists("scene-save.json")) { _lastSaveStatus = "No save file"; return; }
                var json = System.IO.File.ReadAllText("scene-save.json");
                SaveData save = null;
                try { save = System.Text.Json.JsonSerializer.Deserialize<SaveData>(json); } catch { }
                if (save?.Scene != null)
                {
                    _scene = World.GameScene.FromSnapshot(GraphicsDevice, save.Scene);
                    if (_sceneRenderer != null)
                    {
                        _sceneRenderer.DebugOverlayEnabled = save.OverlayEnabled;
                        _sceneRenderer.OverlayMode = save.OverlayMode;
                        _sceneRenderer.FillAttackRanges = save.FillAttackRanges;
                        _sceneRenderer.SetCameraState(new Vector3(save.CameraTargetX, 0, save.CameraTargetZ), save.CameraZoom <= 0 ? 1f : save.CameraZoom);
                        if (save.MiniMapSizeIndex >= 0 && save.MiniMapSizeIndex < _miniMapSizes.Length) _miniMapSizeIndex = save.MiniMapSizeIndex;
                        if (save.MiniMapZoomLevel >= 0 && save.MiniMapZoomLevel < _miniMapZoomScales.Length) _miniMapZoomLevel = save.MiniMapZoomLevel;
                        if (save.CameraTweenLerpFactor >= MinTweenSpeed && save.CameraTweenLerpFactor <= MaxTweenSpeed && save.CameraTweenLerpFactor > 0) _cameraTweenSpeed = save.CameraTweenLerpFactor;
                    }
                    _upgrades = UpgradeCatalog.CreateDefaults();
                    if (save.Upgrades != null)
                    {
                        foreach (var ul in save.Upgrades)
                        {
                            var local = _upgrades.Find(u => u.Id == ul.Id);
                            if (local == null) continue;
                            for (int i = 0; i < ul.Level; i++)
                            {
                                foreach (var mech in _scene.Mechs) local.ApplyEffect?.Invoke(mech);
                                local.Level++;
                            }
                        }
                    }
                    _lastSaveStatus = $"Loaded {DateTime.Now:HH:mm:ss}";
                }
                else
                {
                    var snap = System.Text.Json.JsonSerializer.Deserialize<World.SceneSnapshot>(json);
                    if (snap != null)
                    {
                        _scene = World.GameScene.FromSnapshot(GraphicsDevice, snap);
                        _lastSaveStatus = $"Loaded (legacy) {DateTime.Now:HH:mm:ss}";
                    }
                }
            }
            catch (Exception ex)
            {
                _lastSaveStatus = "Load failed: " + ex.Message;
            }
        }

        private Rectangle GetMiniMapRect()
        {
            int size = _miniMapSizes[_miniMapSizeIndex];
            return new Rectangle(GraphicsDevice.Viewport.Width - size - 10, 10, size, size);
        }

        private void UpdateCameraTween()
        {
            if (_pendingCameraTweenTarget.HasValue && _sceneRenderer != null)
            {
                var goal = _pendingCameraTweenTarget.Value;
                // Clamp goal to world bounds if zoomed
                float worldSize = _scene != null ? _scene.GridSize * _scene.TileWorldSize : 100f;
                float zoomScale = _miniMapZoomScales[_miniMapZoomLevel];
                float halfWindow = (worldSize * zoomScale) * 0.5f;
                goal.X = MathHelper.Clamp(goal.X, halfWindow, worldSize - halfWindow);
                goal.Z = MathHelper.Clamp(goal.Z, halfWindow, worldSize - halfWindow);
                var cur = _sceneRenderer.CameraTarget;
                // Exponential ease: fraction increases as we get closer
                float dist = Vector3.Distance(cur, goal);
                float ease = MathHelper.Clamp(_cameraTweenSpeed + (dist * 0.015f), _cameraTweenSpeed, MaxTweenSpeed);
                var next = Vector3.Lerp(cur, goal, ease);
                _sceneRenderer.CenterCamera(next);
                if (Vector3.Distance(next, goal) < 0.5f)
                {
                    _sceneRenderer.CenterCamera(goal);
                    _pendingCameraTweenTarget = null;
                }
            }
        }

        private void DrawMiniMapLine(Vector2 a, Vector2 b, Color color)
        {
            if (_whiteTex == null)
            {
                _whiteTex = new Texture2D(GraphicsDevice, 1, 1);
                _whiteTex.SetData(new[] { Color.White });
            }
            int steps = (int)Vector2.Distance(a, b);
            for (int i = 0; i <= steps; i++)
            {
                var p = Vector2.Lerp(a, b, i / (float)steps);
                _spriteBatch.Draw(_whiteTex, new Rectangle((int)p.X, (int)p.Y, 1, 1), color);
            }
        }
        #endregion
    }
}