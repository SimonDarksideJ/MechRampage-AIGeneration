using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MechRampage.Core.Entities
{
    /// <summary>
    /// Simple collectible resource node that grants resources on click (when selected mech present nearby).
    /// </summary>
    public class ResourceNode
    {
        public Vector3 Position;
        public int Value;
        public bool Collected;
        private static VertexPositionColor[] _verts;
        private static short[] _indices;
        private static readonly Color BaseColor = new Color(60, 180, 220);

        public BoundingBox Bounds => new(Position + new Vector3(-0.6f, 0, -0.6f), Position + new Vector3(0.6f, 1.8f, 0.6f));

        public ResourceNode(Vector3 pos, int value)
        {
            Position = pos;
            Value = value;
            EnsureGeometry();
        }

        public void Draw(BasicEffect effect, GraphicsDevice device)
        {
            if (Collected) return;
            var world = Matrix.CreateScale(1.2f, 2f, 1.2f) * Matrix.CreateTranslation(Position + new Vector3(0, 1f, 0));
            effect.World = world;
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                for (int i = 0; i < _verts.Length; i++) _verts[i].Color = BaseColor;
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
