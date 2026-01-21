using System;
using System.IO;
using System.Text.Json;
using TWL.Client.Presentation.Models;
using TWL.Shared.Domain.Characters;

namespace TWL.Client.Presentation.Managers
{
    public class PersistenceManager
    {
        private readonly string _savePath;

        public PersistenceManager()
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TheWonderland");
            Directory.CreateDirectory(folder);
            _savePath = Path.Combine(folder, "savegame.json");
        }

        public bool SaveExists()
        {
            return File.Exists(_savePath);
        }

        public void SaveGame(PlayerCharacter player)
        {
            var data = new GameSaveData
            {
                Level = player.Level,
                Exp = player.Exp,
                ExpToNextLevel = player.ExpToNextLevel,
                Health = player.Health,
                MaxHealth = player.MaxHealth,
                Sp = player.Sp,
                MaxSp = player.MaxSp,
                Str = player.Str,
                Con = player.Con,
                Int = player.Int,
                Wis = player.Wis,
                Spd = player.Agi,
                Gold = player.Gold,
                TwlPoints = player.TwlPoints,
                PositionX = player.Position.X,
                PositionY = player.Position.Y
            };

            foreach (var slot in player.Inventory.ItemSlots)
            {
                data.Inventory.Add(new InventorySlotSaveData
                {
                    ItemId = slot.ItemId,
                    Quantity = slot.Quantity
                });
            }

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_savePath, json);
        }

        public GameSaveData? LoadGame()
        {
            if (!SaveExists()) return null;

            try
            {
                var json = File.ReadAllText(_savePath);
                return JsonSerializer.Deserialize<GameSaveData>(json);
            }
            catch
            {
                return null;
            }
        }
    }
}
