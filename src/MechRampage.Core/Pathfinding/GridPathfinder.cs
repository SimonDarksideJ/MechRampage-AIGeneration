using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace MechRampage.Core.Pathfinding
{
    /// <summary>
    /// Lightweight A* over implicit uniform grid; obstacles optional for future expansion.
    /// </summary>
    public static class GridPathfinder
    {
        private struct Node
        {
            public Point P;
            public float G; // cost from start
            public float F; // total (G + H)
            public Point Parent;
            public bool HasParent;
        }

        public static List<Vector3> FindPath(Point start, Point goal, int gridSize, float tileWorldSize)
        {
            if (start == goal)
            {
                return new List<Vector3> { ToWorld(start, tileWorldSize) };
            }

            var open = new Dictionary<Point, Node>();
            var openQueue = new PriorityQueue<Point, float>();
            var closed = new HashSet<Point>();
            // Track all discovered nodes for robust reconstruction (both open and closed)
            var nodes = new Dictionary<Point, Node>();

            Node startNode = new()
            {
                P = start,
                G = 0,
                F = Heuristic(start, goal)
            };
            open[start] = startNode;
            nodes[start] = startNode;
            openQueue.Enqueue(start, startNode.F);

            Point[] dirs = new Point[]
            {
                new(1,0), new(-1,0), new(0,1), new(0,-1)
            };

            while (openQueue.Count > 0)
            {
                var currentPoint = openQueue.Dequeue();
                // Skip stale queue entries that were improved/closed later
                if (!open.TryGetValue(currentPoint, out var current))
                {
                    continue;
                }
                if (currentPoint == goal)
                {
                    return Reconstruct(current, nodes, tileWorldSize);
                }
                open.Remove(currentPoint);
                closed.Add(currentPoint);

                foreach (var d in dirs)
                {
                    var np = new Point(currentPoint.X + d.X, currentPoint.Y + d.Y);
                    if (np.X < 0 || np.Y < 0 || np.X >= gridSize || np.Y >= gridSize) continue;
                    if (closed.Contains(np)) continue;

                    float tentativeG = current.G + 1f;
                    if (open.TryGetValue(np, out var existing))
                    {
                        if (tentativeG < existing.G)
                        {
                            existing.G = tentativeG;
                            existing.F = tentativeG + Heuristic(np, goal);
                            existing.Parent = currentPoint;
                            existing.HasParent = true;
                            open[np] = existing;
                            nodes[np] = existing;
                            openQueue.Enqueue(np, existing.F);
                        }
                    }
                    else
                    {
                        Node n = new()
                        {
                            P = np,
                            G = tentativeG,
                            F = tentativeG + Heuristic(np, goal),
                            Parent = currentPoint,
                            HasParent = true
                        };
                        open[np] = n;
                        nodes[np] = n;
                        openQueue.Enqueue(np, n.F);
                    }
                }
            }
            // Fallback: direct
            return new List<Vector3> { ToWorld(start, tileWorldSize), ToWorld(goal, tileWorldSize) };
        }

        private static float Heuristic(Point a, Point b)
        {
            // Manhattan distance with tiny tie-breaker to encourage progress
            float h = System.Math.Abs(a.X - b.X) + System.Math.Abs(a.Y - b.Y);
            return h + 0.001f * (a.X + a.Y);
        }

        private static List<Vector3> Reconstruct(Node end, Dictionary<Point, Node> all, float tileWorldSize)
        {
            var list = new List<Vector3>();
            var cur = end;
            list.Add(ToWorld(cur.P, tileWorldSize));
            int safety = 0;
            while (cur.HasParent && safety++ < 10000)
            {
                if (!all.TryGetValue(cur.Parent, out var parent))
                {
                    // Break if chain is broken to avoid KeyNotFound; return partial path
                    break;
                }
                cur = parent;
                list.Add(ToWorld(cur.P, tileWorldSize));
            }
            list.Reverse();
            return list;
        }

        public static Point ToGrid(Vector3 world, float tileWorldSize)
        {
            return new Point((int)(world.X / tileWorldSize), (int)(world.Z / tileWorldSize));
        }

        public static Vector3 ToWorld(Point grid, float tileWorldSize)
        {
            return new Vector3(grid.X * tileWorldSize + tileWorldSize * 0.5f, 0, grid.Y * tileWorldSize + tileWorldSize * 0.5f);
        }
    }
}
