using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Tiled;
using MonoGame.Extended.Tiled.Renderers;
using TWL.Client.Managers;
using TWL.Client.Presentation.Core;
using TWL.Client.Presentation.Helpers;
using TWL.Client.Presentation.Managers;
using TWL.Client.Presentation.Models;
using TWL.Client.Presentation.Networking;
using TWL.Client.Presentation.Services;
using TWL.Client.Presentation.UI;
using TWL.Client.Presentation.Views;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Events;
using TWL.Shared.Net;
using TWL.Shared.Net.Abstractions;

namespace TWL.Client.Presentation.Scenes
{
    public sealed class SceneGameplay : SceneBase
    {
        private readonly GameClientManager _gameManager;
        private readonly LoopbackChannel   _netChannel;
        private readonly EncounterManager  _encounter = new();

        private PlayerCharacter  _player     = null!;
        private PlayerView       _playerView = null!;
        private PlayerColors     _colors     = null!;
        private UiGameplay       _ui         = null!;

        private TiledMap         _map;
        private TiledMapRenderer _mapRenderer;
        private Vector2          _clickTarget;

        public SceneGameplay(
            ContentManager    content,
            GraphicsDevice    graphicsDevice,
            ISceneManager     scenes,
            IAssetLoader      assets,
            GameClientManager gameManager,
            LoopbackChannel   netChannel
        ) : base(content, graphicsDevice, scenes, assets)
        {
            _gameManager = gameManager;
            _netChannel  = netChannel;
        }

        public override void Initialize()
        {
            base.Initialize();

            // supongamos que tu GameClientManager te da el playerId
            var dto = new JsonPlayerColorsService(...)
                .Get(_gameManager.PlayerId)
                ?? throw new Exception("No hay colores");

            _player = new PlayerCharacter("Hero", Element.Fire, dto);
            _playerView = new PlayerView(_player);
            _ui = new UiGameplay(_player);

            // 4) eventos de batalla
            EventBus.Subscribe<BattleStarted>(   e => Scenes.ChangeScene("Battle",    e) );
            EventBus.Subscribe<BattleFinished>( e => OnBattleFinished(e)               );
        }

        public override void LoadContent()
        {
            _playerView.Load(Content, GraphicsDevice);
            _ui.LoadContent(Content);

            try {
                _map = Content.Load<TiledMap>("Maps/GreenMap");
            }
            catch {
                _map = new TiledMap(
                    "Empty","",1,1,32,32,
                    TiledMapTileDrawOrder.LeftDown,
                    TiledMapOrientation.Orthogonal
                );
            }
            _mapRenderer = new TiledMapRenderer(GraphicsDevice, _map);

            // extraer colisión
            var layer = _map.GetLayer<TiledMapTileLayer>("Collision");
            int w = _map.Width, h = _map.Height;
            var grid = new bool[w,h];
            for (int x=0;x<w;x++)
            for (int y=0;y<h;y++)
                grid[x,y] = layer.GetTile((ushort)x,(ushort)y).GlobalIdentifier != 0;
            _player.SetCollisionInfo(grid, w, h);
        }

        public override void Update(GameTime gt,
                                    MouseState ms,
                                    KeyboardState ks)
        {
            if (ks.IsKeyDown(Keys.I))
                _ui.ToggleInventory();

            if (ms.LeftButton == ButtonState.Pressed)
            {
                var world = Camera.ScreenToWorld(
                    ms.Position.ToVector2(),
                    GraphicsDevice
                );
                var start = new Point(
                    (int)(_player.Position.X / _player.TileWidth),
                    (int)(_player.Position.Y / _player.TileHeight)
                );
                var end = new Point(
                    (int)(world.X / _player.TileWidth),
                    (int)(world.Y / _player.TileHeight)
                );
                var path = PathFinder.FindPath(start, end, _map);
                _player.SetPath(path);
            }

            _player.Update(gt);
            _encounter.CheckEncounter(_player);
            _playerView.Update(gt);
            _mapRenderer.Update(gt);
            _ui.Update(gt, ms, ks);

            base.Update(gt, ms, ks);
        }

        public override void Draw(SpriteBatch sb)
        {
            _mapRenderer.Draw(Camera.GetViewMatrix());
            _playerView.Draw(sb);
            _ui.Draw(sb);
        }

        private void OnBattleFinished(BattleFinished e)
        {
            if (e.Victory)
            {
                _player.GainExp(e.ExpGained);
                foreach (var loot in e.Loot)
                    _player.Inventory.AddItem(loot.ItemId, loot.Quantity);
            }
            Scenes.ChangeScene("Gameplay");
        }
    }
}
