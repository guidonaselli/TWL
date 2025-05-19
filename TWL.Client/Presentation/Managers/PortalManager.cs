using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Tiled;

namespace TWL.Client.Presentation.Managers;

public class Portal
{
    public Rectangle Bounds;
    public Vector2 SpawnPosition; // Posición en píxeles donde aparecerá el jugador
    public string TargetMap; // Nombre del mapa (ej.: "DungeonMap")
}

public class PortalManager
{
    private readonly List<Portal> _portals;

    public PortalManager()
    {
        _portals = new List<Portal>();
    }

    /// <summary>
    ///     Limpia la lista de portales y carga de la capa "Portals" en Tiled.
    /// </summary>
    public void LoadPortalsFromMap(TiledMap map, string layerName = "Portals")
    {
        _portals.Clear();

        // Obtenemos la capa de objetos
        var objectLayer = map.GetLayer<TiledMapObjectLayer>(layerName);
        if (objectLayer == null)
            return;

        foreach (var obj in objectLayer.Objects)
        {
            // Creamos un rectángulo a partir de la posición y tamaño del objeto
            var portalRect = new Rectangle(
                (int)obj.Position.X,
                (int)obj.Position.Y,
                (int)obj.Size.Width,
                (int)obj.Size.Height
            );

            // Leemos las propiedades "TargetMap", "TargetX", "TargetY"
            var targetMap = "";
            var targetX = 0f;
            var targetY = 0f;

            if (obj.Properties.ContainsKey("TargetMap"))
                targetMap = obj.Properties["TargetMap"];

            if (obj.Properties.ContainsKey("TargetX"))
                targetX = float.Parse(obj.Properties["TargetX"]);

            if (obj.Properties.ContainsKey("TargetY"))
                targetY = float.Parse(obj.Properties["TargetY"]);

            var portal = new Portal
            {
                Bounds = portalRect,
                TargetMap = targetMap,
                SpawnPosition = new Vector2(targetX, targetY)
            };

            _portals.Add(portal);
        }
    }

    /// <summary>
    ///     Verifica si el rectángulo (jugador) colisiona con algún portal.
    ///     Retorna el portal correspondiente o null si no hay colisión.
    /// </summary>
    public Portal CheckCollision(Rectangle playerBounds)
    {
        foreach (var portal in _portals)
            if (playerBounds.Intersects(portal.Bounds))
                return portal;

        return null;
    }
}