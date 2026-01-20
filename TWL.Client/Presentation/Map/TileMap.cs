using System;
﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace TWL.Client.Presentation.Map;

public class TileMap
{
    private int _cols;
    private int[,] _mapData = null!;
    private int _rows;
    private Texture2D? _tileSet;
    private int _tilesPerRow;

    public TileMap(int tileWidth = 32, int tileHeight = 32)
    {
        TileWidth  = tileWidth;
        TileHeight = tileHeight;
    }

    public int TileWidth { get; }

    public int TileHeight { get; }

    public void LoadContent(ContentManager content, string tileSetName)
    {
        _tileSet = content.Load<Texture2D>("Tilesets/BasicTileset");


        // Ejemplo: 1 = pasable, 0 = bloqueado
        _mapData = new[,]
        {
            { 1, 1, 1, 1, 1, 1, 1 },
            { 1, 1, 0, 0, 1, 1, 1 },
            { 1, 1, 1, 1, 1, 1, 1 },
            { 1, 1, 1, 0, 1, 0, 1 },
            { 1, 1, 1, 1, 1, 1, 1 }
        };

        _rows = _mapData.GetLength(0);
        _cols = _mapData.GetLength(1);

        _tilesPerRow = _tileSet.Width / TileWidth;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (_tileSet is null) return;                      // null-check

        for (var y = 0; y < _rows; y++)
        for (var x = 0; x < _cols; x++)
        {
            var tileIndex = _mapData[y, x];
            var tileX = tileIndex % _tilesPerRow;
            var tileY = tileIndex / _tilesPerRow;

            var source = new Rectangle(
                tileX * TileWidth,
                tileY * TileHeight,
                TileWidth,
                TileHeight
            );

            var pos = new Vector2(x * TileWidth, y * TileHeight);
            spriteBatch.Draw(_tileSet, pos, source, Color.White);
        }
    }

    public void Draw(SpriteBatch spriteBatch, Camera2D camera, GraphicsDevice graphicsDevice)
    {
        if (_tileSet is null) return;

        // Calculate visible bounds
        var viewport = graphicsDevice.Viewport;
        var corners = new[]
        {
            Vector2.Zero,
            new Vector2(viewport.Width, 0),
            new Vector2(viewport.Width, viewport.Height),
            new Vector2(0, viewport.Height)
        };

        var minWorld = new Vector2(float.MaxValue);
        var maxWorld = new Vector2(float.MinValue);

        foreach (var corner in corners)
        {
            var worldPos = camera.ScreenToWorld(corner, graphicsDevice);
            minWorld = Vector2.Min(minWorld, worldPos);
            maxWorld = Vector2.Max(maxWorld, worldPos);
        }

        // Convert to tile coordinates
        var minX = (int)Math.Floor(minWorld.X / TileWidth);
        var minY = (int)Math.Floor(minWorld.Y / TileHeight);
        var maxX = (int)Math.Ceiling(maxWorld.X / TileWidth);
        var maxY = (int)Math.Ceiling(maxWorld.Y / TileHeight);

        // Clamp to map bounds
        minX = Math.Clamp(minX, 0, _cols);
        minY = Math.Clamp(minY, 0, _rows);
        maxX = Math.Clamp(maxX, 0, _cols);
        maxY = Math.Clamp(maxY, 0, _rows);

        for (var y = minY; y < maxY; y++)
        for (var x = minX; x < maxX; x++)
        {
            var tileIndex = _mapData[y, x];
            var tileX = tileIndex % _tilesPerRow;
            var tileY = tileIndex / _tilesPerRow;

            var source = new Rectangle(
                tileX * TileWidth,
                tileY * TileHeight,
                TileWidth,
                TileHeight
            );

            var pos = new Vector2(x * TileWidth, y * TileHeight);
            spriteBatch.Draw(_tileSet, pos, source, Color.White);
        }
    }

    public bool IsBlocked(Point tilePos)
    {
        if (tilePos.Y < 0 || tilePos.Y >= _rows ||
            tilePos.X < 0 || tilePos.X >= _cols)
            return true;

        // 0 = bloqueado, 1 = libre
        return _mapData[tilePos.Y, tilePos.X] == 0;
    }
}