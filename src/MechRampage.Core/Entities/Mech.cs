using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace MechRampage.Core.Entities
{
    /// <summary>
    /// Minimal mech entity placeholder with simple movement toward a target.
    /// </summary>
    public class Mech
    {
        public Vector3 Position;
        public Vector3 TargetPosition;
        public bool HasMoveOrder;
        public bool Selected;
    public bool Hovered;
        public float MoveSpeed = 12f;
    public string Name { get; set; }
    public int Health { get; set; } = 100;
    public int AttackDamage { get; set; } = 10;
    public float AttackRange { get; set; } = 6f;
    public float AttackCooldown { get; set; } = 0.9f;
    private float _attackTimer;
    public Enemy CurrentTarget;
    // Time since current move/path order was issued (seconds) for debug visualization.
    public float PathAge { get; private set; }

        private readonly Color _baseColor = new Color(180, 180, 210);
        private readonly Color _selectedColor = new Color(255, 230, 80);
    private readonly Color _hoverColor = new Color(120, 200, 255);

    private readonly Queue<Vector3> _waypoints = new();
    // Expose remaining path (copy) for debug visualization.
    public IEnumerable<Vector3> PendingWaypoints => _waypoints.ToArray();

        // Simple cached cube geometry (shared static to avoid per-instance allocation)
        private static VertexPositionColor[] _cubeVerts;
        private static short[] _cubeIndices;

        public Mech(Vector3 start, string name = "Mech")
        {
            Position = start;
            TargetPosition = start;
            Name = name;
            EnsureGeometry();
        }

        public void IssueMove(Vector3 destination)
        {
            TargetPosition = destination;
            HasMoveOrder = true;
            _waypoints.Clear();
            PathAge = 0f;
        }

        public void IssuePath(IEnumerable<Vector3> path)
        {
            _waypoints.Clear();
            foreach (var p in path)
            {
                _waypoints.Enqueue(p);
            }
            if (_waypoints.Count > 0)
            {
                TargetPosition = _waypoints.Dequeue();
                HasMoveOrder = true;
                PathAge = 0f;
            }
        }

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _attackTimer -= dt;
            if (HasMoveOrder)
            {
                PathAge += dt;
                var to = TargetPosition - Position;
                float dist = to.Length();
                if (dist < 0.2f)
                {
                    Position = TargetPosition;
                    if (_waypoints.Count > 0)
                    {
                        TargetPosition = _waypoints.Dequeue();
                    }
                    else
                    {
                        HasMoveOrder = false;
                        PathAge = 0f; // reset after completing path
                    }
                }
                else
                {
                    to.Normalize();
                    Position += to * MoveSpeed * dt;
                }
            }

            {
                PathAge = 0f; // keep at zero when idle
            }
            // Combat: if we have a target and in range, attack.
            if (CurrentTarget != null && CurrentTarget.Alive)
            {
                var diff = CurrentTarget.Position - Position;
                var flatDist = new Vector2(diff.X, diff.Z).Length();
                if (flatDist <= AttackRange)
                {
                    // stop moving while in range
                    HasMoveOrder = false;
                    if (_attackTimer <= 0f)
                    {
                        // Defer damage application to projectile impact by spawning projectile.
                        SpawnProjectile?.Invoke(this, CurrentTarget, AttackDamage);
                        _attackTimer = AttackCooldown;
                    }
                }
                else if (!HasMoveOrder)
                {
                    // move closer (direct for now)
                    IssueMove(CurrentTarget.Position);
                }
            }
            else if (CurrentTarget != null && !CurrentTarget.Alive)
            {
                CurrentTarget = null;
            }
        }

        public void Draw(BasicEffect effect, GraphicsDevice device)
        {
            var world = Matrix.CreateScale(0.8f, 2f, 0.8f) * Matrix.CreateTranslation(Position + new Vector3(0, 1f, 0));
            effect.World = world;
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                var color = Selected ? _selectedColor : (Hovered ? _hoverColor : _baseColor);
                TintVertexArray(color);
                device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _cubeVerts, 0, _cubeVerts.Length, _cubeIndices, 0, _cubeIndices.Length / 3);
            }
        }

        private static void EnsureGeometry()
        {
            if (_cubeVerts != null) return;
            // Unit cube centered at origin
            Vector3[] corners = new Vector3[]
            {
                new(-0.5f,-0.5f,-0.5f), new(-0.5f, 0.5f,-0.5f), new(0.5f,0.5f,-0.5f), new(0.5f,-0.5f,-0.5f), // back
                new(-0.5f,-0.5f, 0.5f), new(-0.5f, 0.5f, 0.5f), new(0.5f,0.5f, 0.5f), new(0.5f,-0.5f, 0.5f)  // front
            };
            _cubeVerts = new VertexPositionColor[corners.Length];
            for (int i = 0; i < corners.Length; i++) _cubeVerts[i] = new VertexPositionColor(corners[i], Color.White);
            _cubeIndices = new short[]
            {
                0,1,2, 0,2,3, // back
                4,6,5, 4,7,6, // front
                4,5,1, 4,1,0, // left
                3,2,6, 3,6,7, // right
                1,5,6, 1,6,2, // top
                4,0,3, 4,3,7  // bottom
            };
        }

        private void TintVertexArray(Color c)
        {
            for (int i = 0; i < _cubeVerts.Length; i++)
            {
                _cubeVerts[i].Color = c;
            }
        }

        public BoundingBox GetBounds()
        {
            var min = Position + new Vector3(-0.4f, 0, -0.4f);
            var max = Position + new Vector3(0.4f, 2f, 0.4f);
            return new BoundingBox(min, max);
        }

        public float? RayIntersect(Ray ray)
        {
            return ray.Intersects(GetBounds());
        }

    // Callback assigned externally to spawn projectiles into scene.
    public static System.Action<Mech, Enemy, int> SpawnProjectile;
    }
}
