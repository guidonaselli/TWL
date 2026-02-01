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

        // Reused collections to avoid allocations per pathfind call
        private readonly PriorityQueue<Node, int> _open = new();
        private readonly Dictionary<Point, Node> _nodes = new();
        private readonly HashSet<Point> _closed = new();

        public Pathfinder(TileMap tileMap) => _tileMap = tileMap;

        /// <summary>Devuelve la lista de tiles (incluido <paramref name="start"/> y <paramref name="goal"/>) o
        /// una lista vacía si no existe trayecto.</summary>
        public List<Point> FindPath(Point start, Point goal)
        {
            _open.Clear();
            _nodes.Clear();
            _closed.Clear();

            var startNode = new Node(start, null, g: 0, h: Heuristic(start, goal));
            _open.Enqueue(startNode, startNode.F);
            _nodes.Add(start, startNode);

            while (_open.Count > 0)
            {
                var current = _open.Dequeue();

                if (_closed.Contains(current.Pos))
                    continue;

                if (current.Pos == goal)
                    return BuildPath(current);

                _closed.Add(current.Pos);

                foreach (var nPos in GetNeighbors(current.Pos))
                {
                    if (_tileMap.IsBlocked(nPos) || _closed.Contains(nPos))
                        continue;

                    var newG = current.G + 1;

                    if (!_nodes.TryGetValue(nPos, out var neighborNode))
                    {
                        neighborNode = new Node(nPos, current, newG, Heuristic(nPos, goal));
                        _nodes.Add(nPos, neighborNode);
                        _open.Enqueue(neighborNode, neighborNode.F);
                    }
                    else if (newG < neighborNode.G)
                    {
                        neighborNode.G = newG;
                        neighborNode.Parent = current;
                        _open.Enqueue(neighborNode, neighborNode.F);
                    }
                }
            }

            return new(); // sin camino
        }

        /* ---------- helpers ---------- */

        private static int Heuristic(Point a, Point b) =>
            Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);      // distancia Manhattan

        private static NeighborEnumerator GetNeighbors(Point p) => new(p);

        private struct NeighborEnumerator
        {
            private readonly Point _p;
            private int _index;

            public NeighborEnumerator(Point p)
            {
                _p = p;
                _index = -1;
            }

            public Point Current => _index switch
            {
                0 => new Point(_p.X, _p.Y - 1), // Norte
                1 => new Point(_p.X, _p.Y + 1), // Sur
                2 => new Point(_p.X - 1, _p.Y), // Oeste
                3 => new Point(_p.X + 1, _p.Y), // Este
                _ => default
            };

            public bool MoveNext() => ++_index < 4;

            public NeighborEnumerator GetEnumerator() => this;
        }

        private static List<Point> BuildPath(Node endNode)
        {
            var path = new List<Point>();
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
            public Point Pos { get; }
            public Node? Parent { get; set; }
            public int G { get; set; }   // coste desde el inicio
            private readonly int _h;               // heurística

            public int F => G + _h;                // función de evaluación

            public Node(Point pos, Node? parent, int g, int h)
            {
                Pos = pos;
                Parent = parent;
                G = g;
                _h = h;
            }
        }
    }
}
