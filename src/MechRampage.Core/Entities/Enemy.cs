using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace MechRampage.Core.Entities
{
    /// <summary>
    /// Simple enemy placeholder that can take damage and be destroyed.
    /// </summary>
    public class Enemy
    {
        public Vector3 Position;
        public int Health = 150;
    public bool Alive => Health > 0;
    private float _deathTimer;
    public bool ReadyToRemove => !Alive && _deathTimer <= 0f;
        public BoundingBox Bounds => new(Position + new Vector3(-0.6f, 0, -0.6f), Position + new Vector3(0.6f, 3f, 0.6f));

        private static VertexPositionColor[] _verts;
        private static short[] _indices;
        private static readonly Color BaseColor = new Color(200, 60, 60);

        public Enemy(Vector3 position)
        {
            Position = position;
            EnsureGeometry();
        }

        public void TakeDamage(int dmg)
        {
            Health -= dmg;
            if (Health < 0) Health = 0;
            if (!Alive) _deathTimer = 1.2f; // fade duration
        }

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (!Alive && _deathTimer > 0f)
            {
                _deathTimer -= dt;
            }
        }

        public void Draw(BasicEffect effect, GraphicsDevice device)
        {
            if (!Alive && _deathTimer <= 0f) return;
            var world = Matrix.CreateScale(1.2f, 3f, 1.2f) * Matrix.CreateTranslation(Position + new Vector3(0, 1.5f, 0));
            effect.World = world;
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                float alpha = Alive ? 1f : MathHelper.Clamp(_deathTimer / 1.2f, 0f, 1f);
                var tint = new Color(BaseColor.R, BaseColor.G, BaseColor.B) * alpha;
                for (int i = 0; i < _verts.Length; i++) _verts[i].Color = tint;
                device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _verts, 0, _verts.Length, _indices, 0, _indices.Length / 3);
            }
        }

        public float? RayIntersect(Ray ray) => ray.Intersects(Bounds);

        private static void EnsureGeometry()
        {
            if (_verts != null) return;
            Vector3[] corners = new Vector3[]
            {
                new(-0.5f,-0.5f,-0.5f), new(-0.5f,0.5f,-0.5f), new(0.5f,0.5f,-0.5f), new(0.5f,-0.5f,-0.5f),
                new(-0.5f,-0.5f, 0.5f), new(-0.5f,0.5f, 0.5f), new(0.5f,0.5f, 0.5f), new(0.5f,-0.5f, 0.5f)
            };
            _verts = new VertexPositionColor[corners.Length];
            for (int i = 0; i < corners.Length; i++) _verts[i] = new VertexPositionColor(corners[i], BaseColor);
            _indices = new short[]
            {
                0,1,2, 0,2,3,
                4,6,5, 4,7,6,
                4,5,1, 4,1,0,
                3,2,6, 3,6,7,
                1,5,6, 1,6,2,
                4,0,3, 4,3,7
            };
        }
    }
}
