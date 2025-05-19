// TWL.Shared/Domain/Characters/PlayerCharacter.cs

namespace TWL.Shared.Domain.Characters
{
    public class PlayerCharacter : Character
    {
        public Guid Id { get; private set; }

        // Recursos y monedas
        public int Gold      { get; set; }
        public int TwlPoints { get; set; }

        // Inventario y mascotas
        public Inventory Inventory         { get; private set; }
        public List<PetCharacter> Pets     { get; private set; }

        // Progresión y estadísticas
        public int Level          { get; private set; }
        public int Exp            { get; private set; }
        public int ExpToNextLevel { get; private set; }

        public int Str { get; private set; }
        public int Con { get; private set; }
        public int Int { get; private set; }
        public int Wis { get; private set; }
        public int Spd { get; private set; }

        // Vida y mana derivadas
        public int MaxHealth => Con * 10;
        public int CurrentHealth { get; private set; }
        public int MaxMana   => Int * 5;
        public int CurrentMana   { get; private set; }

        public PlayerCharacter(Guid id, string name, Element element)
            : base(name, element)
        {
            Id        = id;
            Inventory = new Inventory();
            Pets      = new List<PetCharacter>();

            Level          = 1;
            Exp            = 0;
            ExpToNextLevel = 100;

            // Stats iniciales
            Str = Con = Int = Wis = Spd = 5;

            Gold      = 0;
            TwlPoints = 0;

            CurrentHealth = MaxHealth;
            CurrentMana   = MaxMana;
        }

        // -----------------------------------------------------------
        // Experiencia y nivel
        // -----------------------------------------------------------
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

                // Restaurar recursos al subir de nivel
                CurrentHealth = MaxHealth;
                CurrentMana   = MaxMana;
            }
        }

        // -----------------------------------------------------------
        // Monedas y puntos
        // -----------------------------------------------------------
        public void AddGold(int amount)
        {
            Gold = Math.Max(0, Gold + amount);
        }

        public void AddTwlPoints(int amount)
        {
            TwlPoints = Math.Max(0, TwlPoints + amount);
        }

        // -----------------------------------------------------------
        // Inventario
        // -----------------------------------------------------------
        public void AddItem(int itemId, int quantity = 1)
        {
            Inventory.AddItem(itemId, quantity);
        }

        public void RemoveItem(int itemId, int quantity = 1)
        {
            Inventory.RemoveItem(itemId, quantity);
        }

        // -----------------------------------------------------------
        // Mascotas
        // -----------------------------------------------------------
        public void AddPet(PetCharacter pet)
        {
            if (Pets.All(p => p.Id != pet.Id))
                Pets.Add(pet);
        }

        public void RemovePet(int petId)
        {
            var pet = Pets.FirstOrDefault(p => p.Id == petId);
            if (pet != null)
                Pets.Remove(pet);
        }

        // -----------------------------------------------------------
        // Vida y mana
        // -----------------------------------------------------------
        public void Heal(int amount)
        {
            CurrentHealth = Math.Min(MaxHealth, CurrentHealth + amount);
        }

        public void TakeDamage(int amount)
        {
            CurrentHealth = Math.Max(0, CurrentHealth - amount);
        }

        public void RestoreMana(int amount)
        {
            CurrentMana = Math.Min(MaxMana, CurrentMana + amount);
        }

        public void SpendMana(int amount)
        {
            CurrentMana = Math.Max(0, CurrentMana - amount);
        }

        // -----------------------------------------------------------
        // Daño base
        // -----------------------------------------------------------
        public int CalculatePhysicalDamage() => Str * 2;
        public int CalculateMagicalDamage()  => Int * 2;

        // -----------------------------------------------------------
        // Reborn / Renacer
        // -----------------------------------------------------------
        public void Rebirth()
        {
            if (Level < 100)
                throw new InvalidOperationException("Se necesita nivel 100 para renacer.");

            Level          = 1;
            Exp            = 0;
            ExpToNextLevel = 100;

            // Bonus permanentes
            Str += 10;
            Con += 10;
            Int += 10;
            Wis += 10;
            Spd += 10;

            CurrentHealth = MaxHealth;
            CurrentMana   = MaxMana;
        }
    }
}
