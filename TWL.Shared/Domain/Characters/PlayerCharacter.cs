using TWL.Shared.Domain.DTO;
using Microsoft.Xna.Framework;

namespace TWL.Shared.Domain.Characters
{
    public class PlayerCharacter : Character
    {
        public Guid GuidId { get; private set; }

        public PlayerColorsDto Colors { get; set; } = new PlayerColorsDto();

        // Resources
        public int Gold { get; set; }
        public int TwlPoints { get; set; }

        // Inventory & Pets
        public Inventory Inventory { get; private set; }
        public List<PetCharacter> Pets { get; private set; }

        // Progression
        public int Level { get; private set; }
        public int Exp { get; private set; }
        public int ExpToNextLevel { get; private set; }

        // Collision & Movement
        private bool[,] _collisionGrid;
        public int MapWidth { get; private set; }
        public int MapHeight { get; private set; }
        public int TileWidth { get; private set; } = 32;
        public int TileHeight { get; private set; } = 32;

        private Queue<Point> _currentPath;
        private Vector2 _targetPosition;
        private bool _isMoving;

        public PlayerCharacter(Guid guidId, string name, Element element)
            : base(name, element)
        {
            GuidId = guidId;
            Inventory = new Inventory();
            Pets = new List<PetCharacter>();

            Level = 1;
            Exp = 0;
            ExpToNextLevel = 100;

            Str = 5;
            Con = 5;
            Int = 5;
            Wis = 5;
            Spd = 5;

            Gold = 0;
            TwlPoints = 0;

            UpdateDerivedStats();
            Health = MaxHealth;
            Sp = MaxSp;
        }

        public PlayerCharacter(Guid guidId, string name, Element element, PlayerColorsDto colors)
            : this(guidId, name, element)
        {
            Colors = colors;
        }

        public void SetCollisionInfo(bool[,] grid, int width, int height)
        {
            _collisionGrid = grid;
            MapWidth = width;
            MapHeight = height;
        }

        public bool IsColliding(Vector2 position)
        {
            if (_collisionGrid == null) return false;
            int x = (int)(position.X / TileWidth);
            int y = (int)(position.Y / TileHeight);

            if (x < 0 || x >= MapWidth || y < 0 || y >= MapHeight) return true;
            return _collisionGrid[x, y];
        }

        public void SetPath(List<Point> path)
        {
            _currentPath = new Queue<Point>(path);
            _isMoving = true;
            if (_currentPath.Count > 0)
            {
                var next = _currentPath.Dequeue();
                _targetPosition = new Vector2(next.X * TileWidth, next.Y * TileHeight);
            }
        }

        public override void Update(GameTime gameTime)
        {
            // Handle Path Movement
            if (_isMoving && _currentPath != null)
            {
                float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
                float step = MovementSpeed * 60f * dt;

                if (Vector2.Distance(Position, _targetPosition) < step)
                {
                    Position = _targetPosition;
                    if (_currentPath.Count > 0)
                    {
                        var next = _currentPath.Dequeue();
                        _targetPosition = new Vector2(next.X * TileWidth, next.Y * TileHeight);
                    }
                    else
                    {
                        _isMoving = false;
                    }
                }
                else
                {
                    Vector2 dir = _targetPosition - Position;
                    dir.Normalize();
                    Position += dir * step;
                }
            }

            // MovementController call removed. Should be handled by Client.

            base.Update(gameTime);
        }

        public void GainExp(int amount)
        {
            Exp += amount;
            while (Exp >= ExpToNextLevel)
            {
                Exp -= ExpToNextLevel;
                Level++;
                ExpToNextLevel = (int)(ExpToNextLevel * 1.2);

                Str++;
                Con++;
                Int++;
                Wis++;
                Spd++;

                UpdateDerivedStats();

                Health = MaxHealth;
                Sp = MaxSp;
            }
        }

        public void UpdateDerivedStats()
        {
            MaxHealth = Con * 10;
            MaxSp = Int * 5;

            if (Health > MaxHealth) Health = MaxHealth;
            if (Sp > MaxSp) Sp = MaxSp;
        }

        public void AddGold(int amount) => Gold = Math.Max(0, Gold + amount);
        public void AddTwlPoints(int amount) => TwlPoints = Math.Max(0, TwlPoints + amount);

        public void AddItem(int itemId, int quantity = 1) => Inventory.AddItem(itemId, quantity);
        public void RemoveItem(int itemId, int quantity = 1) => Inventory.RemoveItem(itemId, quantity);

        public void AddPet(PetCharacter pet)
        {
            if (Pets.All(p => p.Id != pet.Id)) Pets.Add(pet);
        }
        public void RemovePet(int petId)
        {
            var pet = Pets.FirstOrDefault(p => p.Id == petId);
            if (pet != null) Pets.Remove(pet);
        }

        public void Heal(int amount) => Health = Math.Min(MaxHealth, Health + amount);
        public void TakeDamage(int amount) => Health = Math.Max(0, Health - amount);
        public void RestoreMana(int amount) => Sp = Math.Min(MaxSp, Sp + amount);
        public void SpendMana(int amount) => Sp = Math.Max(0, Sp - amount);

        public void Rebirth()
        {
            if (Level < 100) throw new InvalidOperationException("Need level 100 to rebirth.");

            Level = 1;
            Exp = 0;
            ExpToNextLevel = 100;

            Str += 10;
            Con += 10;
            Int += 10;
            Wis += 10;
            Spd += 10;

            UpdateDerivedStats();
            Health = MaxHealth;
            Sp = MaxSp;
        }

        public void SetProgress(int level, int exp, int expToNextLevel)
        {
            Level = level;
            Exp = exp;
            ExpToNextLevel = expToNextLevel;
        }
    }
}
