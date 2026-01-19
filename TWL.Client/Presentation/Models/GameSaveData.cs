using System;
using System.Collections.Generic;

namespace TWL.Client.Presentation.Models
{
    public class GameSaveData
    {
        public int Level { get; set; }
        public int Exp { get; set; }
        public int ExpToNextLevel { get; set; }

        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public int Sp { get; set; }
        public int MaxSp { get; set; }

        public int Str { get; set; }
        public int Con { get; set; }
        public int Int { get; set; }
        public int Wis { get; set; }
        public int Spd { get; set; }

        public int Gold { get; set; }
        public int TwlPoints { get; set; }

        public float PositionX { get; set; }
        public float PositionY { get; set; }

        public List<InventorySlotSaveData> Inventory { get; set; } = new();
    }

    public class InventorySlotSaveData
    {
        public int ItemId { get; set; }
        public int Quantity { get; set; }
    }
}
