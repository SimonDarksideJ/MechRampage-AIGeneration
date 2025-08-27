using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MechRampage.Core.World;
using MechRampage.Core.Entities;

namespace MechRampage.Core.Rendering
{
    public enum DebugOverlayMode { Off, Paths, PathsAndRanges }
    /// <summary>
    /// Responsible for drawing a GameScene with a simple isometric style camera.
    /// This is placeholder scaffolding until the full render pipeline (HDR, glow, etc.) is implemented.
    /// </summary>
    public class SceneRenderer
    {
        private readonly GraphicsDevice _device;
        private readonly SpriteBatch _spriteBatch;
        private readonly Camera _camera;
    public bool DebugOverlayEnabled { get; set; } = true;
    public DebugOverlayMode OverlayMode { get; set; } = DebugOverlayMode.PathsAndRanges;
    public bool FillAttackRanges { get; set; } = false;

        public SceneRenderer(GraphicsDevice device, SpriteBatch spriteBatch)
        {
            _device = device;
            _spriteBatch = spriteBatch;
            _camera = new Camera(device);
        }

        public void Draw(GameScene scene, GameTime gameTime)
        {
            _device.RasterizerState = RasterizerState.CullCounterClockwise;
            _device.DepthStencilState = DepthStencilState.Default;
            _device.BlendState = BlendState.Opaque;

            foreach (var tile in scene.Tiles)
            {
                var effect = scene.SharedEffect;
                effect.World = Matrix.Identity;
                effect.View = _camera.View;
                effect.Projection = _camera.Projection;
                foreach (var pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    _device.DrawUserIndexedPrimitives(
                        primitiveType: PrimitiveType.TriangleList,
                        vertexData: tile.Vertices,
                        vertexOffset: 0,
                        numVertices: tile.Vertices.Length,
                        indexData: tile.Indices,
                        indexOffset: 0,
                        primitiveCount: tile.Indices.Length / 3);
                }
            }

            // Draw mechs (reuse effect but disable fog for clarity)
            if (scene.Mechs.Count > 0)
            {
                var effect = scene.SharedEffect;
                bool prevFog = effect.FogEnabled;
                effect.FogEnabled = false;
                foreach (var mech in scene.Mechs)
                {
                    mech.Draw(effect, _device);
                }
                effect.FogEnabled = prevFog;
            }

            // Draw enemies
            if (scene.Enemies.Count > 0)
            {
                var effect = scene.SharedEffect;
                bool prevFog = effect.FogEnabled;
                effect.FogEnabled = false;
                foreach (var enemy in scene.Enemies)
                {
                    enemy.Draw(effect, _device);
                }
                effect.FogEnabled = prevFog;
            }

            // Draw resource nodes
            if (scene.ResourceNodes.Count > 0)
            {
                var effect = scene.SharedEffect;
                bool prevFog = effect.FogEnabled;
                effect.FogEnabled = false;
                foreach (var node in scene.ResourceNodes)
                {
                    node.Draw(effect, _device);
                }
                effect.FogEnabled = prevFog;
            }

            // Debug: draw paths for selected mechs (simple line list using immediate mode)
            if (DebugOverlayEnabled && OverlayMode != DebugOverlayMode.Off)
            {
            foreach (var mech in scene.Mechs)
            {
                if (!mech.Selected) continue;
                // Build path points list
                var points = new System.Collections.Generic.List<Vector3>();
                points.Add(mech.Position + new Vector3(0, 0.15f, 0));
                if (mech.HasMoveOrder) points.Add(mech.TargetPosition + new Vector3(0, 0.15f, 0));
                points.AddRange(mech.PendingWaypoints);
                if (points.Count >= 2 && (OverlayMode == DebugOverlayMode.Paths || OverlayMode == DebugOverlayMode.PathsAndRanges))
                {
                    float ageNorm = MathHelper.Clamp(mech.PathAge / 5f, 0f, 1f); // fade over 5 seconds
                    Color startCol = Color.Lerp(Color.White, Color.Yellow, 0.7f);
                    Color endCol = Color.Lerp(Color.OrangeRed, Color.Transparent, ageNorm);
                    var lineVerts = new VertexPositionColor[(points.Count - 1) * 2];
                    int vi = 0;
                    for (int i = 0; i < points.Count - 1; i++)
                    {
                        float tEdge = (float)i / (points.Count - 2);
                        var cA = Color.Lerp(startCol, endCol, tEdge);
                        var cB = Color.Lerp(startCol, endCol, (float)(i + 1) / (points.Count - 2));
                        lineVerts[vi++] = new VertexPositionColor(points[i], cA);
                        lineVerts[vi++] = new VertexPositionColor(points[i + 1], cB);
                    }
                    var effect = scene.SharedEffect;
                    bool prevFog = effect.FogEnabled; effect.FogEnabled = false;
                    effect.World = Matrix.Identity;
                    effect.View = _camera.View;
                    effect.Projection = _camera.Projection;
                    foreach (var pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        _device.DrawUserPrimitives(PrimitiveType.LineList, lineVerts, 0, lineVerts.Length / 2);
                    }
                    effect.FogEnabled = prevFog;
                }

                // Target marker
                if (mech.HasMoveOrder && (OverlayMode == DebugOverlayMode.Paths || OverlayMode == DebugOverlayMode.PathsAndRanges))
                {
                    var t = mech.TargetPosition;
                    var cross = new VertexPositionColor[]
                    {
                        new(t + new Vector3(-0.5f,0.05f,0), Color.LightGreen), new(t + new Vector3(0.5f,0.05f,0), Color.LightGreen),
                        new(t + new Vector3(0,0.05f,-0.5f), Color.LightGreen), new(t + new Vector3(0,0.05f,0.5f), Color.LightGreen)
                    };
                    var effect = scene.SharedEffect; bool prevFog = effect.FogEnabled; effect.FogEnabled = false;
                    effect.World = Matrix.Identity; effect.View = _camera.View; effect.Projection = _camera.Projection;
                    foreach (var pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        _device.DrawUserPrimitives(PrimitiveType.LineList, cross, 0, 2);
                    }
                    effect.FogEnabled = prevFog;
                }

                // Attack range circle (flat ring) for situational awareness
                if (OverlayMode == DebugOverlayMode.PathsAndRanges)
                {
                    int segs = 32;
                    float radius = mech.AttackRange;
                    if (FillAttackRanges)
                    {
                        var fanVerts = new System.Collections.Generic.List<VertexPositionColor>();
                        var center = mech.Position + new Vector3(0, 0.03f, 0);
                        Color fillCol = new Color(0, 255, 120) * 0.18f;
                        for (int i = 0; i < segs; i++)
                        {
                            float a0 = MathHelper.TwoPi * (i / (float)segs);
                            float a1 = MathHelper.TwoPi * ((i + 1) / (float)segs);
                            Vector3 p0 = mech.Position + new Vector3(System.MathF.Cos(a0) * radius, 0.03f, System.MathF.Sin(a0) * radius);
                            Vector3 p1 = mech.Position + new Vector3(System.MathF.Cos(a1) * radius, 0.03f, System.MathF.Sin(a1) * radius);
                            fanVerts.Add(new VertexPositionColor(center, fillCol));
                            fanVerts.Add(new VertexPositionColor(p0, fillCol));
                            fanVerts.Add(new VertexPositionColor(p1, fillCol));
                        }
                        var eff = scene.SharedEffect; bool prevFog2 = eff.FogEnabled; eff.FogEnabled = false;
                        eff.World = Matrix.Identity; eff.View = _camera.View; eff.Projection = _camera.Projection;
                        foreach (var pass in eff.CurrentTechnique.Passes)
                        {
                            pass.Apply();
                            _device.DrawUserPrimitives(PrimitiveType.TriangleList, fanVerts.ToArray(), 0, fanVerts.Count / 3);
                        }
                        eff.FogEnabled = prevFog2;
                    }
                    // Outline ring
                    var ringVerts = new VertexPositionColor[segs * 2];
                    for (int i = 0; i < segs; i++)
                    {
                        float a0 = MathHelper.TwoPi * (i / (float)segs);
                        float a1 = MathHelper.TwoPi * ((i + 1) / (float)segs);
                        Vector3 p0 = mech.Position + new Vector3(System.MathF.Cos(a0) * radius, 0.05f, System.MathF.Sin(a0) * radius);
                        Vector3 p1 = mech.Position + new Vector3(System.MathF.Cos(a1) * radius, 0.05f, System.MathF.Sin(a1) * radius);
                        ringVerts[i * 2] = new VertexPositionColor(p0, new Color(0, 255, 120) * 0.35f);
                        ringVerts[i * 2 + 1] = new VertexPositionColor(p1, new Color(0, 255, 120) * 0.35f);
                    }
                    var effect = scene.SharedEffect; bool prevFog = effect.FogEnabled; effect.FogEnabled = false;
                    effect.World = Matrix.Identity; effect.View = _camera.View; effect.Projection = _camera.Projection;
                    foreach (var pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        _device.DrawUserPrimitives(PrimitiveType.LineList, ringVerts, 0, segs);
                    }
                    effect.FogEnabled = prevFog;
                }
            }
            }
        }

        public void UpdateCamera(GameTime gameTime, GameScene scene)
        {
            _camera.Update(gameTime, scene.WorldBounds);
        }

        public Ray GetPickRay(Point mousePoint)
        {
            var vp = _device.Viewport;
            Vector3 nearPoint = vp.Unproject(new Vector3(mousePoint.X, mousePoint.Y, 0f), _camera.Projection, _camera.View, Matrix.Identity);
            Vector3 farPoint = vp.Unproject(new Vector3(mousePoint.X, mousePoint.Y, 1f), _camera.Projection, _camera.View, Matrix.Identity);
            var dir = Vector3.Normalize(farPoint - nearPoint);
            return new Ray(nearPoint, dir);
        }

        public Vector2 ProjectToScreen(Vector3 worldPosition)
        {
            var vp = _device.Viewport;
            var projected = vp.Project(worldPosition, _camera.Projection, _camera.View, Matrix.Identity);
            return new Vector2(projected.X, projected.Y);
        }

        public Matrix View => _camera.View;
        public Matrix Projection => _camera.Projection;
    public void CenterCamera(Vector3 worldTarget) => _camera.SetTarget(worldTarget);
    public Vector3 CameraTarget => _camera.Target;
    public float CameraZoom => _camera.Zoom;
    public void SetCameraState(Vector3 target, float zoom)
    {
        _camera.SetTarget(target);
        _camera.SetZoom(zoom);
    }
    }
}
