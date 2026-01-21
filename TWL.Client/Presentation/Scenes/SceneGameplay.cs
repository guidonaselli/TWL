using System;
using System.Linq;
using System.Collections.Generic;
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
    public sealed class SceneGameplay : SceneBase, IPayloadReceiver
    {
        private readonly GameClientManager _gameManager;
        private readonly LoopbackChannel   _netChannel;
        private PlayerCharacterData _playerData = new PlayerCharacterData
        {
            PlayerId = 1,
            UserId = 1,
            PosX = 100,
            PosY = 100,
            Hp = 100,
            MaxHp = 100
        };
        private readonly EncounterManager  _encounter = new();

        private PlayerCharacter  _player     = null!;
        private PlayerView       _playerView = null!;
        private PlayerColors     _colors     = null!;
        private UiGameplay       _ui         = null!;

        private TiledMap         _map;
        private TiledMapRenderer _mapRenderer;
        private Vector2          _clickTarget;
        private Point?           _lastTargetTile;
        private readonly PersistenceManager _persistence;

        public SceneGameplay(
            ContentManager    content,
            GraphicsDevice    graphicsDevice,
            ISceneManager     scenes,
            IAssetLoader      assets,
            GameClientManager gameManager,
            LoopbackChannel   netChannel,
            PersistenceManager persistence
        ) : base(content, graphicsDevice, scenes, assets)
        {
            _gameManager = gameManager;
            _netChannel  = netChannel;
            _persistence = persistence;
        }

        public void ReceivePayload(object payload)
        {
            if (payload is PlayerCharacterData playerData)
            {
                _playerData = playerData;
            }
            else if (payload is GameSaveData data && _player != null)
            {
                _player.SetProgress(data.Level, data.Exp, data.ExpToNextLevel);
                _player.Health = data.Health;
                _player.MaxHealth = data.MaxHealth;
                _player.Sp = data.Sp;
                _player.MaxSp = data.MaxSp;
                _player.Str = data.Str;
                _player.Con = data.Con;
                _player.Int = data.Int;
                _player.Wis = data.Wis;
                _player.Agi = data.Spd;
                _player.Gold = data.Gold;
                _player.TwlPoints = data.TwlPoints;
                _player.Position = new Vector2(data.PositionX, data.PositionY);

                _player.Inventory.ItemSlots = data.Inventory.Select(i =>
                    new TWL.Shared.Domain.Characters.ItemSlot(i.ItemId, i.Quantity)).ToList();
            }
            else if (_playerData == null)
            {
                // Fallback or error if payload is not correct
                // For now, create a default player
                _playerData = new PlayerCharacterData
                {
                    PlayerId = 1,
                    UserId = 1,
                    PosX = 100,
                    PosY = 100,
                    Hp = 100,
                    MaxHp = 100
                };
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            var dto = new TWL.Shared.Domain.DTO.PlayerColorsDto();

            _player = new PlayerCharacter(_gameManager.PlayerId, "Hero", Element.Fire, dto);
            _player.Position = new Vector2(_playerData.PosX, _playerData.PosY);
            // You might want to set other stats from _playerData here as well

            _playerView = new PlayerView(_player);
            _ui = new UiGameplay(_player);

            EventBus.Subscribe<BattleStarted>(   e => Scenes.ChangeScene("Battle",    e) );
            EventBus.Subscribe<BattleFinished>( e => OnBattleFinished(e)               );
        }

        public override void LoadContent()
        {
            _playerView.Load(Content, GraphicsDevice);
            _ui.LoadContent(Content, GraphicsDevice);

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

            var layer = _map.GetLayer<TiledMapTileLayer>("Collision");
            if (layer != null)
            {
                int w = _map.Width, h = _map.Height;
                var grid = new bool[w,h];
                for (int x=0;x<w;x++)
                for (int y=0;y<h;y++)
                    grid[x,y] = layer.GetTile((ushort)x,(ushort)y).GlobalIdentifier != 0;
                _player.SetCollisionInfo(grid, w, h);
            }
            else
            {
                 // Create empty grid if no collision layer
                 _player.SetCollisionInfo(new bool[_map.Width,_map.Height], _map.Width, _map.Height);
            }
        }

        public override void UnloadContent()
        {
            _playerView?.Dispose();
            base.UnloadContent();
        }

        public override void Update(GameTime gt,
                                    MouseState ms,
                                    KeyboardState ks)
        {
            if (ks.IsKeyDown(Keys.I))
                _ui.ToggleInventory();

            if (ks.IsKeyDown(Keys.B))
                _encounter.ForceEncounter(_player);

            if (ks.IsKeyDown(Keys.F5))
                _persistence.SaveGame(_player);

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

                if (_lastTargetTile != end)
                {
                    var path = PathFinder.FindPath(start, end, _map);
                    _player.SetPath(path);
                    _lastTargetTile = end;
                }
            }
            else
            {
                _lastTargetTile = null;
            }

            // Explicitly call MovementController (Client Side Input)
            MovementController.UpdateMovement(_player, gt);

            _player.Update(gt);
            if (_player.IsMoving)
            {
                _encounter.CheckEncounter(_player);
            }
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
            // Scene change is now handled by SceneBattle after showing results
        }
    }
}
