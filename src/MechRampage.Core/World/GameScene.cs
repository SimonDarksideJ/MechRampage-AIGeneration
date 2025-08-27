using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MechRampage.Core.Entities;
using System.Collections.Generic;

namespace MechRampage.Core.World
{
    /// <summary>
    /// Lightweight placeholder scene containing terrain tiles and props.
    /// </summary>
    public class GameScene
    {
        public List<TerrainTile> Tiles { get; } = new();
        public List<SceneProp> Props { get; } = new();
        public BasicEffect SharedEffect { get; }
        public int GridSize { get; }
        public float TileWorldSize { get; }

        public BoundingBox WorldBounds { get; }
    public List<Entities.Mech> Mechs { get; } = new();
    public List<Entities.Enemy> Enemies { get; } = new();
    public List<Projectile> Projectiles { get; } = new();
    public List<DamageText> DamageTexts { get; } = new();
    public List<Entities.ResourceNode> ResourceNodes { get; } = new();
    public int GlobalResources { get; set; }

        public GameScene(GraphicsDevice graphicsDevice, int gridSize, float tileWorldSize)
        {
            GridSize = gridSize;
            TileWorldSize = tileWorldSize;

            SharedEffect = new BasicEffect(graphicsDevice)
            {
                TextureEnabled = false,
                LightingEnabled = true,
                PreferPerPixelLighting = true,
                AmbientLightColor = new Vector3(0.35f),
                FogEnabled = true,
                FogColor = new Vector3(0.05f, 0.05f, 0.08f),
                FogStart = 20f,
                FogEnd = 120f
            };

            // Single directional light for now.
            SharedEffect.DirectionalLight0.Enabled = true;
            SharedEffect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(-0.3f, -1f, -0.4f));
            SharedEffect.DirectionalLight0.DiffuseColor = new Vector3(0.85f, 0.85f, 0.9f);
            SharedEffect.DirectionalLight0.SpecularColor = new Vector3(0.4f);

            // Precompute world bounds.
            var sizeWorld = gridSize * tileWorldSize;
            WorldBounds = new BoundingBox(Vector3.Zero, new Vector3(sizeWorld, 10f, sizeWorld));
        }

        public void Update(GameTime gameTime)
        {
            // Reserved for animated props, weather cycles, etc.
            foreach (var mech in Mechs)
            {
                mech.Update(gameTime);
            }
            foreach (var enemy in Enemies)
            {
                enemy.Update(gameTime);
            }

            // Remove fully faded dead enemies
            for (int i = Enemies.Count - 1; i >= 0; i--)
            {
                if (Enemies[i].ReadyToRemove)
                {
                    Enemies.RemoveAt(i);
                }
            }

            // Resource nodes (future animations) cleanup if collected
            for (int i = ResourceNodes.Count - 1; i >= 0; i--)
            {
                if (ResourceNodes[i].Collected)
                    ResourceNodes.RemoveAt(i);
            }

            // Projectiles
            for (int i = Projectiles.Count - 1; i >= 0; i--)
            {
                if (!Projectiles[i].Update(gameTime)) Projectiles.RemoveAt(i);
            }

            // Floating damage text
            for (int i = DamageTexts.Count - 1; i >= 0; i--)
            {
                if (!DamageTexts[i].Update(gameTime)) DamageTexts.RemoveAt(i);
            }
        }

        // --- Serialization helpers (prototype) ---
        public SceneSnapshot ToSnapshot()
        {
            var snap = new SceneSnapshot
            {
                GridSize = GridSize,
                TileWorldSize = TileWorldSize,
                GlobalResources = GlobalResources
            };
            foreach (var m in Mechs)
            {
                snap.Mechs.Add(new MechSnap
                {
                    Name = m.Name,
                    X = m.Position.X,
                    Y = m.Position.Y,
                    Z = m.Position.Z,
                    HP = m.Health,
                    Dmg = m.AttackDamage,
                    Spd = m.MoveSpeed,
                    Rng = m.AttackRange
                });
            }
            foreach (var e in Enemies)
            {
                snap.Enemies.Add(new EnemySnap { X = e.Position.X, Y = e.Position.Y, Z = e.Position.Z, HP = e.Health });
            }
            foreach (var r in ResourceNodes)
            {
                snap.Resources.Add(new ResourceSnap { X = r.Position.X, Y = r.Position.Y, Z = r.Position.Z, V = r.Value, C = r.Collected });
            }
            return snap;
        }

        public static GameScene FromSnapshot(GraphicsDevice device, SceneSnapshot snap)
        {
            var scene = new GameScene(device, snap.GridSize, snap.TileWorldSize);
            // Rebuild flat tiles (no noise for now)
            for (int z = 0; z < snap.GridSize; z++)
            {
                for (int x = 0; x < snap.GridSize; x++)
                {
                    float height = 0f;
                    var tileCenter = new Vector3(x * snap.TileWorldSize, height, z * snap.TileWorldSize);
                    var color = new Color(30, 70, 30);
                    var verts = SceneFactory.CreateDefaultArena(device, 1, snap.TileWorldSize); // placeholder misuse; will refactor
                }
            }
            // Instead of rebuilding, just call factory and then override entities
            scene.Mechs.Clear();
            foreach (var m in snap.Mechs)
            {
                var mech = new Entities.Mech(new Vector3(m.X, m.Y, m.Z), m.Name)
                {
                    Health = m.HP,
                    AttackDamage = m.Dmg,
                    MoveSpeed = m.Spd,
                    AttackRange = m.Rng
                };
                scene.Mechs.Add(mech);
            }
            scene.Enemies.Clear();
            foreach (var e in snap.Enemies)
            {
                var enemy = new Entities.Enemy(new Vector3(e.X, e.Y, e.Z));
                enemy.Health = e.HP;
                scene.Enemies.Add(enemy);
            }
            scene.ResourceNodes.Clear();
            foreach (var r in snap.Resources)
            {
                var node = new Entities.ResourceNode(new Vector3(r.X, r.Y, r.Z), r.V) { Collected = r.C };
                scene.ResourceNodes.Add(node);
            }
            scene.GlobalResources = snap.GlobalResources;
            return scene;
        }
    }

    // DTOs (minimal) for proto save
    public class SceneSnapshot
    {
        public int GridSize { get; set; }
        public float TileWorldSize { get; set; }
        public int GlobalResources { get; set; }
        public List<MechSnap> Mechs { get; set; } = new();
        public List<EnemySnap> Enemies { get; set; } = new();
        public List<ResourceSnap> Resources { get; set; } = new();
    }
    public class MechSnap { public string Name; public float X,Y,Z; public int HP; public int Dmg; public float Spd; public float Rng; }
    public class EnemySnap { public float X,Y,Z; public int HP; }
    public class ResourceSnap { public float X,Y,Z; public int V; public bool C; }

    public class Projectile
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public float Life; // seconds remaining
        public Entities.Enemy Target;
        public int Damage;

        public Projectile(Vector3 start, Entities.Enemy target, int damage, float speed = 40f)
        {
            Position = start;
            Target = target;
            Damage = damage;
            Life = 3f;
            if (target != null)
            {
                var to = (target.Position - start);
                to.Y = 0;
                if (to != Vector3.Zero) to.Normalize();
                Velocity = to * speed;
            }
            else
            {
                Velocity = Vector3.Zero;
            }
        }

        public bool Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Life -= dt;
            if (Life <= 0) return false;
            if (Target != null && Target.Alive)
            {
                var to = Target.Position - Position;
                var dist = to.Length();
                if (dist < 1.2f)
                {
                    Target.TakeDamage(Damage);
                    return false;
                }
                to.Normalize();
                Velocity = to * Velocity.Length();
            }
            Position += Velocity * dt;
            return true;
        }
    }

    public class DamageText
    {
        public Vector3 WorldPosition;
        public string Text;
        public float Life = 1f;
        private float _age;

        public DamageText(Vector3 pos, string text)
        {
            WorldPosition = pos;
            Text = text;
        }

        public bool Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _age += dt;
            WorldPosition += new Vector3(0, dt * 1.5f, 0);
            return _age < Life;
        }

        public float Opacity => 1f - (_age / Life);
    }

    public record TerrainTile(VertexPositionColor[] Vertices, short[] Indices, BoundingBox Bounds);
    public record SceneProp(Vector3 Position, Vector3 Scale, BoundingBox Bounds);
}
