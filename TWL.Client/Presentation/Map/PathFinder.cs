// File: TWL.Client/Presentation/Map/Pathfinder.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace TWL.Client.Presentation.Map
{
    /// <summary>A* path-finder para mapas basados en <see cref="TileMap"/>.</summary>
    public sealed class Pathfinder
    {
        private readonly TileMap _tileMap;

        public Pathfinder(TileMap tileMap) => _tileMap = tileMap;

        /// <summary>Devuelve la lista de tiles (incluido <paramref name="start"/> y <paramref name="goal"/>) o
        /// una lista vacía si no existe trayecto.</summary>
        public List<Point> FindPath(Point start, Point goal)
        {
            var open = new PriorityQueue<Node, int>();
            var nodes = new Dictionary<Point, Node>();
            var closed = new HashSet<Point>();

            var startNode = new Node(start, null, g: 0, h: Heuristic(start, goal));
            open.Enqueue(startNode, startNode.F);
            nodes.Add(start, startNode);

            while (open.Count > 0)
            {
                var current = open.Dequeue();

                if (closed.Contains(current.Pos))
                    continue;

                if (current.Pos == goal)
                    return BuildPath(current);

                closed.Add(current.Pos);

                foreach (var nPos in GetNeighbors(current.Pos))
                {
                    if (_tileMap.IsBlocked(nPos) || closed.Contains(nPos))
                        continue;

                    var newG = current.G + 1;

                    if (!nodes.TryGetValue(nPos, out var neighborNode))
                    {
                        neighborNode = new Node(nPos, current, newG, Heuristic(nPos, goal));
                        nodes.Add(nPos, neighborNode);
                        open.Enqueue(neighborNode, neighborNode.F);
                    }
                    else if (newG < neighborNode.G)
                    {
                        neighborNode.G = newG;
                        neighborNode.Parent = current;
                        open.Enqueue(neighborNode, neighborNode.F);
                    }
                }
            }

            return new(); // sin camino
        }

        /* ---------- helpers ---------- */

        private static int Heuristic(Point a, Point b) =>
            Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);      // distancia Manhattan

        private static IEnumerable<Point> GetNeighbors(Point p)
        {
            yield return new Point(p.X,     p.Y - 1);       // Norte
            yield return new Point(p.X,     p.Y + 1);       // Sur
            yield return new Point(p.X - 1, p.Y);           // Oeste
            yield return new Point(p.X + 1, p.Y);           // Este
        }

        private static List<Point> BuildPath(Node endNode)
        {
            var path    = new List<Point>();
            var current = endNode;
            while (current is not null)
            {
                path.Add(current.Pos);
                current = current.Parent;
            }
            path.Reverse();
            return path;
        }

        /* ---------- nodo interno ---------- */

        private sealed class Node
        {
            public Point Pos       { get; }
            public Node? Parent    { get; set; }
            public int   G         { get; set; }   // coste desde el inicio
            private readonly int _h;               // heurística

            public int F => G + _h;                // función de evaluación

            public Node(Point pos, Node? parent, int g, int h)
            {
                Pos    = pos;
                Parent = parent;
                G      = g;
                _h     = h;
            }
        }
    }
}
