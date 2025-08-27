using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using MechRampage.Core.Entities;

namespace MechRampage.Core.World
{
    /// <summary>
    /// Factory responsible for creating basic procedural arenas.
    /// </summary>
    public static class SceneFactory
    {
        public static GameScene CreateDefaultArena(GraphicsDevice device, int size = 32, float tileWorldSize = 2f)
        {
            var scene = new GameScene(device, size, tileWorldSize);

            // Build flat isometric-ish grid (we'll rotate camera later in renderer).
            var rand = new Random(1337);

            for (int z = 0; z < size; z++)
            {
                for (int x = 0; x < size; x++)
                {
                    float height = (float)(rand.NextDouble() * 0.25 - 0.125); // subtle vertical variance
                    var tileCenter = new Vector3(x * tileWorldSize, height, z * tileWorldSize);
                    var color = new Color(20 + rand.Next(10), 60 + rand.Next(30), 20 + rand.Next(10));
                    var verts = CreateQuad(tileCenter, tileWorldSize, color);
                    var indices = new short[] { 0, 1, 2, 2, 1, 3 };
                    var min = tileCenter + new Vector3(-tileWorldSize * 0.5f, -0.1f, -tileWorldSize * 0.5f);
                    var max = tileCenter + new Vector3(tileWorldSize * 0.5f, 0.1f, tileWorldSize * 0.5f);
                    scene.Tiles.Add(new TerrainTile(verts, indices, new BoundingBox(min, max)));
                }
            }

            // Spawn squad at center.
            var spawnCenter = new Vector3(size * tileWorldSize * 0.5f, 0, size * tileWorldSize * 0.5f);
            Vector3[] offsets = new[]
            {
                new Vector3(0,0,0), new Vector3(2,0,0), new Vector3(-2,0,0), new Vector3(0,0,2), new Vector3(0,0,-2)
            };
            int idx = 1;
            foreach (var o in offsets)
            {
                var mech = new Mech(spawnCenter + o, $"Mech-{idx++}");
                if (idx == 2) mech.Selected = true; // first one selected
                scene.Mechs.Add(mech);
            }

            // Spawn enemies toward top-right quadrant
            var enemyBase = spawnCenter + new Vector3(12, 0, -10);
            for (int i = 0; i < 3; i++)
            {
                var enemyPos = enemyBase + new Vector3(i * 3f, 0, i * 2f);
                scene.Enemies.Add(new Enemy(enemyPos));
            }

            // Spawn resource nodes near center outskirts
            var resourceBase = spawnCenter + new Vector3(-8, 0, 10);
            for (int i = 0; i < 2; i++)
            {
                var nodePos = resourceBase + new Vector3(i * 4f, 0, i * 3f);
                scene.ResourceNodes.Add(new ResourceNode(nodePos, 50 + i * 25));
            }
            return scene;
        }

        private static VertexPositionColor[] CreateQuad(Vector3 center, float size, Color color)
        {
            float hs = size * 0.5f;
            return new VertexPositionColor[]
            {
                new(center + new Vector3(-hs,0,-hs), color),
                new(center + new Vector3(-hs,0, hs), color),
                new(center + new Vector3( hs,0,-hs), color),
                new(center + new Vector3( hs,0, hs), color),
            };
        }
    }
}
