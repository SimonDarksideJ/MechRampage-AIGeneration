using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace MechRampage.Core.Rendering
{
    /// <summary>
    /// Simple free-look isometric camera with keyboard pan + mouse wheel zoom.
    /// </summary>
    public class Camera
    {
        public Vector3 Position { get; private set; }
        public Vector3 Target { get; private set; } = Vector3.Zero;
        public float Zoom { get; private set; } = 1f; // Affects distance from target
        public float MinZoom { get; set; } = 0.4f;
        public float MaxZoom { get; set; } = 2.5f;
        public float MoveSpeed { get; set; } = 30f; // world units per second
        public float ZoomSpeed { get; set; } = 0.15f;

        private Matrix _view;
        private Matrix _proj;
        private readonly GraphicsDevice _device;

        public Matrix View => _view;
        public Matrix Projection => _proj;

        public Camera(GraphicsDevice device)
        {
            _device = device;
            RecalculateMatrices();
        }

        public void Update(GameTime gameTime, BoundingBox worldBounds)
        {
            var k = Keyboard.GetState();
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            Vector3 move = Vector3.Zero;
            // Arrow keys pan in isometric plane (X/Z) with slight rotation feel.
            if (k.IsKeyDown(Keys.Left)) move.X -= 1;
            if (k.IsKeyDown(Keys.Right)) move.X += 1;
            if (k.IsKeyDown(Keys.Up)) move.Z -= 1;
            if (k.IsKeyDown(Keys.Down)) move.Z += 1;

            if (move != Vector3.Zero)
            {
                move.Normalize();
                Target += move * MoveSpeed * dt;
            }

            // Clamp target within world bounds.
            Target = Vector3.Clamp(Target, worldBounds.Min, worldBounds.Max);

            // Mouse wheel zoom (desktop only for now)
            if (Mouse.GetState().ScrollWheelValue != _lastWheel)
            {
                int delta = Mouse.GetState().ScrollWheelValue - _lastWheel;
                Zoom -= delta * ZoomSpeed * 0.01f; // reverse so wheel up zooms in
                Zoom = MathHelper.Clamp(Zoom, MinZoom, MaxZoom);
            }
            _lastWheel = Mouse.GetState().ScrollWheelValue;

            RecalculateMatrices();
        }

        private int _lastWheel;

        /// <summary>
        /// Directly set camera target (used by minimap clicks) and immediately recalc matrices.
        /// </summary>
        public void SetTarget(Vector3 target)
        {
            Target = target;
            RecalculateMatrices();
        }

        public void SetZoom(float zoom)
        {
            Zoom = MathHelper.Clamp(zoom, MinZoom, MaxZoom);
            RecalculateMatrices();
        }

        private void RecalculateMatrices()
        {
            // Base distance scaled by zoom.
            float dist = MathHelper.Lerp(20f, 120f, (Zoom - MinZoom) / (MaxZoom - MinZoom));
            var offset = new Vector3(-dist * 0.6f, dist * 0.9f, -dist * 0.6f);
            Position = Target + offset;
            _view = Matrix.CreateLookAt(Position, Target, Vector3.Up);
            _proj = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(60f), _device.Viewport.AspectRatio, 0.1f, 1000f);
        }
    }
}
