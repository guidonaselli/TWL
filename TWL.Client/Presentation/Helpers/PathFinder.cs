using Microsoft.Xna.Framework;
using MonoGame.Extended.Tiled;

namespace TWL.Client.Presentation.Helpers;

public static class PathFinder
{
    public static List<Point> FindPath(
        Point start,
        Point end,
        TiledMap map
    )
    {
        var path = new List<Point>();
        int dx = Math.Abs(end.X - start.X), dy = Math.Abs(end.Y - start.Y);
        int sx = start.X < end.X ? 1 : -1, sy = start.Y < end.Y ? 1 : -1;
        var err = dx - dy;
        int x = start.X, y = start.Y;

        while (true)
        {
            path.Add(new Point(x, y));
            if (x == end.X && y == end.Y)
            {
                break;
            }

            var e2 = err * 2;
            if (e2 > -dy)
            {
                err -= dy;
                x += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y += sy;
            }
        }

        return path;
    }
}