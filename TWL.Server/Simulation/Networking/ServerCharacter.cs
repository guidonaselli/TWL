using System.IO;
using TWL.Server.Persistence;
using TWL.Server.Security;
using TWL.Shared.Domain.Characters;
using TWL.Shared.Domain.Models;

namespace TWL.Server.Simulation.Networking;

/// <summary>
///     Representa un personaje en el lado del servidor.
/// </summary>
public class ServerCharacter : ServerCombatant
{
    private readonly List<Item> _inventory = new();
    private readonly List<Item> _equipment = new();
    private readonly List<Item> _bank = new();

    private readonly Dictionary<int, long> _itemTotalQuantities = new();

    private readonly List<ServerPet> _pets = new();
    private readonly object _progressLock = new();
    private readonly object _orderLock = new();
    private readonly HashSet<string> _processedOrders = new();

    private int _exp;

    private int _gold;

    // Override IsDirty to include pets
    private bool _isDirty;

    private long _premiumCurrency;

    public ServerCharacter()
    {
        Str = 8;
        Con = 8;
        Int = 8;
        Wis = 8;
        Agi = 8;
    }

    public new bool IsDirty
    {
        get
        {
            if (_isDirty || base.IsDirty)
            {
                return true;
            }

            lock (_pets)
            {
                return _pets.Any(p => p.IsDirty);
            }
        }
        set
        {
            _isDirty = value;
            base.IsDirty = value;
            if (!value)
            {
                lock (_pets)
                {
                    foreach (var p in _pets)
                    {
                        p.IsDirty = false;
                    }
                }
            }
        }
    }

    public IReadOnlyList<Item> Bank
    {
        get
        {
            lock (_bank)
            {
                return _bank.Select(i => new Item
                {
                    ItemId = i.ItemId,
                    Name = i.Name,
                    Type = i.Type,
                    MaxStack = i.MaxStack,
                    Quantity = i.Quantity,
                    ForgeSuccessRateBonus = i.ForgeSuccessRateBonus,
                    Policy = i.Policy,
                    BoundToId = i.BoundToId
                }).ToArray();
            }
        }
    }

    public void AddToBank(Item item)
    {
        lock (_bank)
        {
            var existing = _bank.FirstOrDefault(i => i.ItemId == item.ItemId && i.Policy == item.Policy && i.BoundToId == item.BoundToId);
            if (existing != null)
            {
                existing.Quantity += item.Quantity;
            }
            else
            {
                _bank.Add(item);
            }
            IsDirty = true;
        }
    }

    public ICollection<int> KnownSkills => SkillMastery.Keys;
    public int Level { get; private set; } = 1;
    public int RebirthLevel { get; private set; }
    public int ExpToNextLevel { get; private set; } = 100;
    public int StatPoints { get; private set; }

    public string ActivePetInstanceId { get; private set; }
    public DateTime LastPetSwitchTime { get; set; } = DateTime.MinValue;

    // InstanceId -> ReleaseTime
    public Dictionary<string, DateTime> InstanceLockouts { get; } = new();

    // Utility Modifiers
    public bool IsMounted { get; set; }
    public float MoveSpeedModifier { get; set; } = 1.0f;
    public float GatheringBonus { get; set; }
    public float CraftingAssistBonus { get; set; }

    // Social
    public int? PartyId { get; set; }
    public int? GuildId { get; set; }

    // Legacy/Alias support if needed, but preferable to use MaxHp from base
    public int MaxHealth => MaxHp;

    private int? _overrideMaxHp;
    private int? _overrideMaxSp;

    public override int MaxHp => _overrideMaxHp ?? base.MaxHp;
    public override int MaxSp => _overrideMaxSp ?? base.MaxSp;

    public void SetOverrideStats(int maxHp, int maxSp)
    {
        _overrideMaxHp = maxHp;
        _overrideMaxSp = maxSp;
        Hp = maxHp;
        Sp = maxSp;
    }

    public int Exp
    {
        get
        {
            lock (_progressLock)
            {
                return _exp;
            }
        }
        init => _exp = value;
    }

    public int Gold
    {
        get => _gold;
        init => _gold = value;
    }

    public long PremiumCurrency
    {
        get => Interlocked.Read(ref _premiumCurrency);
        init => _premiumCurrency = value;
    }

    // Security / Anti-Fraud
    public long DailyGiftAccumulator { get; private set; }
    public DateTime LastGiftResetDate { get; private set; }

    // World Position
    private int _mapId;
    public int MapId
    {
        get => _mapId;
        set
        {
            if (_mapId != value)
            {
                _mapId = value;
                OnMapChanged?.Invoke(_mapId);
            }
        }
    }

    public float X { get; set; }
    public float Y { get; set; }

    /// <summary>
    /// Updates the character's position and map, notifying listeners even if the map ID hasn't changed.
    /// This ensures clients receive a MapChange packet to sync position for long-distance teleports.
    /// </summary>
    public void Teleport(int mapId, float x, float y)
    {
        _mapId = mapId;
        X = x;
        Y = y;
        // Always invoke to force client sync
        OnMapChanged?.Invoke(_mapId);
    }

    // Mob Info
    public int MonsterId { get; set; }
    public string? SpritePath { get; set; }

    public int MaxInventorySlots { get; set; } = 100;

    public IReadOnlyList<Item> Inventory
    {
        get
        {
            lock (_inventory)
            {
                // Return deep copies to prevent external modification without lock
                return _inventory.Select(i => new Item
                {
                    ItemId = i.ItemId,
                    Name = i.Name,
                    Type = i.Type,
                    MaxStack = i.MaxStack,
                    Quantity = i.Quantity,
                    ForgeSuccessRateBonus = i.ForgeSuccessRateBonus,
                    Policy = i.Policy,
                    BoundToId = i.BoundToId
                }).ToArray();
            }
        }
    }

    public IReadOnlyList<Item> Equipment
    {
        get
        {
            lock (_equipment)
            {
                return _equipment.Select(i => new Item
                {
                    ItemId = i.ItemId,
                    Name = i.Name,
                    Type = i.Type,
                    MaxStack = i.MaxStack,
                    Quantity = i.Quantity,
                    ForgeSuccessRateBonus = i.ForgeSuccessRateBonus,
                    Policy = i.Policy,
                    BoundToId = i.BoundToId
                }).ToArray();
            }
        }
    }

    public IReadOnlyList<ServerPet> Pets
    {
        get
        {
            lock (_pets)
            {
                return _pets.ToArray();
            }
        }
    }

    // Stats & Progression
    public event Action<Item, int>? OnItemAdded;
    public event Action<ServerPet>? OnPetAdded;
    public event Action<ServerCharacter, int, int>? OnTradeCommitted;
    public event Action<int>? OnMapChanged;
    public void SetLevel(int level) => Level = level; // For mobs

    public bool HasProcessedOrder(string orderId)
    {
        lock (_orderLock)
        {
            return _processedOrders.Contains(orderId);
        }
    }

    public void MarkOrderProcessed(string orderId)
    {
        lock (_orderLock)
        {
            _processedOrders.Add(orderId);
            IsDirty = true;
        }
    }

    public override void ReplaceSkill(int oldId, int newId)
    {
        if (SkillMastery.TryRemove(oldId, out _))
        {
            SkillMastery.TryAdd(newId, new SkillMastery());
            IsDirty = true;
        }
    }

    public bool LearnSkill(int skillId)
    {
        if (SkillMastery.ContainsKey(skillId))
        {
            return false;
        }

        SkillMastery.TryAdd(skillId, new SkillMastery());
        IsDirty = true;
        return true;
    }

    public void AddPet(ServerPet pet)
    {
        lock (_pets)
        {
            _pets.Add(pet);
            IsDirty = true;
            OnPetAdded?.Invoke(pet);
        }
    }

    public void NotifyTradeCommitted(ServerCharacter target, int itemId, int quantity) =>
        OnTradeCommitted?.Invoke(target, itemId, quantity);

    public bool RemovePet(string instanceId)
    {
        lock (_pets)
        {
            var pet = _pets.Find(p => p.InstanceId == instanceId);
            if (pet == null)
            {
                return false;
            }

            _pets.Remove(pet);

            if (ActivePetInstanceId == instanceId)
            {
                ActivePetInstanceId = null;
            }

            IsDirty = true;
            return true;
        }
    }

    public bool RemoveItemExact(int itemId, int quantity, BindPolicy policy, int? boundToId)
    {
        lock (_inventory)
        {
            // Strict match on Policy and BoundToId
            long total = 0;
            var candidates = new List<Item>();
            foreach (var item in _inventory)
            {
                if (item.ItemId == itemId && item.Policy == policy && item.BoundToId == boundToId)
                {
                    total += item.Quantity;
                    candidates.Add(item);
                }
            }

            if (total < quantity)
            {
                return false;
            }

            var remainingToRemove = quantity;
            long totalRemoved = 0;
            foreach (var item in candidates)
            {
                if (remainingToRemove <= 0) break;

                var toTake = Math.Min(item.Quantity, remainingToRemove);
                item.Quantity -= toTake;
                remainingToRemove -= toTake;
                totalRemoved += toTake;

                if (item.Quantity <= 0)
                {
                    _inventory.Remove(item);
                }
            }

            if (_itemTotalQuantities.TryGetValue(itemId, out var currentTotal))
            {
                var newTotal = currentTotal - totalRemoved;
                if (newTotal <= 0)
                {
                    _itemTotalQuantities.Remove(itemId);
                }
                else
                {
                    _itemTotalQuantities[itemId] = newTotal;
                }
            }

            IsDirty = true;
            return true;
        }
    }

    public bool CanAddItem(int itemId, int quantity, BindPolicy policy = BindPolicy.Unbound, int? boundToId = null)
    {
        lock (_inventory)
        {
            // 1. Check if stackable
            var existing = _inventory.Find(i => i.ItemId == itemId && i.Policy == policy && i.BoundToId == boundToId);
            if (existing != null)
            {
                // Assuming unlimited stack size for simplicity or check MaxStack if needed
                // ServerCharacter.AddItem check ignores MaxStack, so we do too.
                return true;
            }

            // 2. Check for free slots
            return _inventory.Count < MaxInventorySlots;
        }
    }

    public bool SetActivePet(string instanceId)
    {
        lock (_pets)
        {
            if (string.IsNullOrEmpty(instanceId))
            {
                ActivePetInstanceId = null;
                IsDirty = true;
                return true;
            }

            var pet = _pets.Find(p => p.InstanceId == instanceId);
            if (pet == null)
            {
                return false; // Pet not owned
            }

            ActivePetInstanceId = instanceId;
            IsDirty = true;
            return true;
        }
    }

    public ServerPet? GetActivePet()
    {
        lock (_pets)
        {
            if (string.IsNullOrEmpty(ActivePetInstanceId))
            {
                return null;
            }

            return _pets.Find(p => p.InstanceId == ActivePetInstanceId);
        }
    }

    public void AddExp(int amount)
    {
        lock (_progressLock)
        {
            _exp += amount;
            while (_exp >= ExpToNextLevel)
            {
                _exp -= ExpToNextLevel;
                Level++;
                ExpToNextLevel = (int)(ExpToNextLevel * 1.2);
                StatPoints += 3;
            }

            IsDirty = true;
        }
    }

    public void AddGold(int amount)
    {
        Interlocked.Add(ref _gold, amount);
        IsDirty = true;
    }

    public bool TryConsumeGold(int amount)
    {
        if (amount < 0)
        {
            return false;
        }

        int initial, current;
        do
        {
            initial = _gold;
            if (initial < amount)
            {
                return false;
            }

            current = initial - amount;
        } while (Interlocked.CompareExchange(ref _gold, current, initial) != initial);

        IsDirty = true;
        return true;
    }

    public void AddPremiumCurrency(long amount)
    {
        Interlocked.Add(ref _premiumCurrency, amount);
        IsDirty = true;
    }

    public bool TryConsumePremiumCurrency(long amount)
    {
        if (amount < 0)
        {
            return false;
        }

        long initial, current;
        do
        {
            initial = Interlocked.Read(ref _premiumCurrency);
            if (initial < amount)
            {
                return false;
            }

            current = initial - amount;
        } while (Interlocked.CompareExchange(ref _premiumCurrency, current, initial) != initial);

        IsDirty = true;
        return true;
    }

    public bool TryConsumeDailyGiftLimit(long amount, long limit)
    {
        lock (_progressLock) // Reuse progress lock for simplicity as it protects stats
        {
            var today = DateTime.UtcNow.Date;
            if (LastGiftResetDate < today)
            {
                DailyGiftAccumulator = 0;
                LastGiftResetDate = today;
                IsDirty = true;
            }

            if (DailyGiftAccumulator + amount > limit)
            {
                SecurityLogger.LogSecurityEvent("DailyGiftLimitExceeded", Id,
                    $"Attempt:{amount} Accumulated:{DailyGiftAccumulator} Limit:{limit}");
                return false;
            }

            DailyGiftAccumulator += amount;
            IsDirty = true;
            return true;
        }
    }

    // ConsumeSp is inherited from ServerCombatant

    public bool AddItem(int itemId, int quantity, BindPolicy policy = BindPolicy.Unbound, int? boundToId = null)
    {
        // Auto-bind logic: If policy is strictly bound to owner, ensure it is bound to this character
        if ((policy == BindPolicy.BindOnPickup || policy == BindPolicy.CharacterBound) && boundToId == null)
        {
            boundToId = Id;
            // HARDENING: Log ownership bind
            SecurityLogger.LogSecurityEvent("ItemBound", Id, $"ItemId:{itemId} Policy:{policy} BoundTo:{boundToId}");
        }

        lock (_inventory)
        {
            // Only stack if ID, Policy and BoundToId match
            var existing = _inventory.Find(i => i.ItemId == itemId && i.Policy == policy && i.BoundToId == boundToId);
            if (existing != null)
            {
                existing.Quantity += quantity;
                if (!_itemTotalQuantities.ContainsKey(itemId))
                {
                    _itemTotalQuantities[itemId] = 0;
                }

                _itemTotalQuantities[itemId] += quantity;
                IsDirty = true;
                OnItemAdded?.Invoke(existing, quantity);
                return true;
            }

            if (_inventory.Count >= MaxInventorySlots)
            {
                return false;
            }

            var newItem = new Item { ItemId = itemId, Quantity = quantity, Policy = policy, BoundToId = boundToId };
            _inventory.Add(newItem);
            if (!_itemTotalQuantities.ContainsKey(itemId))
            {
                _itemTotalQuantities[itemId] = 0;
            }

            _itemTotalQuantities[itemId] += quantity;
            IsDirty = true;
            OnItemAdded?.Invoke(newItem, quantity);
            return true;
        }
    }

    public bool HasItem(int itemId, int quantity)
    {
        lock (_inventory)
        {
            return _itemTotalQuantities.TryGetValue(itemId, out var total) && total >= quantity;
        }
    }

    public bool UseItem(int slotIndex, out Item? modifiedItem)
    {
        modifiedItem = null;
        lock (_inventory)
        {
            if (slotIndex < 0 || slotIndex >= _inventory.Count)
            {
                return false;
            }

            var item = _inventory[slotIndex];

            // Equipment Binding Logic
            if (item.Type == ItemType.Equipment && item.Policy == BindPolicy.BindOnEquip && item.BoundToId == null)
            {
                item.BoundToId = Id;
                IsDirty = true;
                SecurityLogger.LogSecurityEvent("ItemBound", Id, $"ItemId:{item.ItemId} Policy:{item.Policy} BoundTo:{Id}");

                // Return copy
                modifiedItem = new Item
                {
                    ItemId = item.ItemId,
                    Name = item.Name,
                    Type = item.Type,
                    MaxStack = item.MaxStack,
                    Quantity = item.Quantity,
                    Policy = item.Policy,
                    BoundToId = item.BoundToId
                };
                return true;
            }

            // Consumable Logic
            if (item.Type == ItemType.Consumable)
            {
                item.Quantity--;
                IsDirty = true;

                // Return copy representing the state *after* use (potentially 0 qty)
                modifiedItem = new Item
                {
                    ItemId = item.ItemId,
                    Name = item.Name,
                    Type = item.Type,
                    MaxStack = item.MaxStack,
                    Quantity = item.Quantity,
                    Policy = item.Policy,
                    BoundToId = item.BoundToId
                };

                // Update Total Quantities Cache
                if (_itemTotalQuantities.TryGetValue(item.ItemId, out var currentTotal))
                {
                    var newTotal = currentTotal - 1;
                    if (newTotal <= 0)
                    {
                        _itemTotalQuantities.Remove(item.ItemId);
                    }
                    else
                    {
                        _itemTotalQuantities[item.ItemId] = newTotal;
                    }
                }

                if (item.Quantity <= 0)
                {
                    _inventory.RemoveAt(slotIndex);
                }

                return true;
            }

            return false;
        }
    }

    public bool RemoveItem(int itemId, int quantity) => RemoveItem(itemId, quantity, null);

    public bool RemoveItem(int itemId, int quantity, BindPolicy? policyFilter)
    {
        lock (_inventory)
        {
            // Calculate total available first to ensure atomicity
            long total = 0;
            var candidates = new List<Item>();
            foreach (var item in _inventory)
            {
                if (item.ItemId == itemId)
                {
                    if (policyFilter.HasValue && item.Policy != policyFilter.Value)
                    {
                        continue;
                    }

                    total += item.Quantity;
                    candidates.Add(item);
                }
            }

            if (total < quantity)
            {
                return false;
            }

            // Remove from candidates
            var remainingToRemove = quantity;
            long totalRemoved = 0;
            foreach (var item in candidates)
            {
                if (remainingToRemove <= 0)
                {
                    break;
                }

                var toTake = Math.Min(item.Quantity, remainingToRemove);
                item.Quantity -= toTake;
                remainingToRemove -= toTake;
                totalRemoved += toTake;

                if (item.Quantity <= 0)
                {
                    _inventory.Remove(item);
                }
            }

            if (_itemTotalQuantities.TryGetValue(itemId, out var currentTotal))
            {
                var newTotal = currentTotal - totalRemoved;
                if (newTotal <= 0)
                {
                    _itemTotalQuantities.Remove(itemId);
                }
                else
                {
                    _itemTotalQuantities[itemId] = newTotal;
                }
            }

            IsDirty = true;
            return true;
        }
    }

    public List<Item> GetItems(int itemId, BindPolicy? policyFilter = null)
    {
        lock (_inventory)
        {
            var results = new List<Item>();
            foreach (var item in _inventory)
            {
                if (item.ItemId == itemId)
                {
                    if (policyFilter.HasValue && item.Policy != policyFilter.Value)
                    {
                        continue;
                    }

                    // Return copies
                    results.Add(new Item
                    {
                        ItemId = item.ItemId,
                        Name = item.Name,
                        Type = item.Type,
                        MaxStack = item.MaxStack,
                        Quantity = item.Quantity,
                        Policy = item.Policy,
                        BoundToId = item.BoundToId
                    });
                }
            }

            return results;
        }
    }

    public bool Equip(int inventorySlotIndex)
    {
        lock (_inventory)
        {
            if (inventorySlotIndex < 0 || inventorySlotIndex >= _inventory.Count)
            {
                return false;
            }

            var item = _inventory[inventorySlotIndex];
            _inventory.RemoveAt(inventorySlotIndex);

            // Update cache for inventory
            if (_itemTotalQuantities.TryGetValue(item.ItemId, out var currentTotal))
            {
                var newTotal = currentTotal - item.Quantity;
                if (newTotal <= 0)
                {
                    _itemTotalQuantities.Remove(item.ItemId);
                }
                else
                {
                    _itemTotalQuantities[item.ItemId] = newTotal;
                }
            }

            lock (_equipment)
            {
                _equipment.Add(item);
            }

            IsDirty = true;
            return true;
        }
    }

    public bool Unequip(int equipmentSlotIndex)
    {
        // Fix potential deadlock: Always acquire locks in consistent order (_inventory -> _equipment)
        lock (_inventory)
        {
            lock (_equipment)
            {
                if (equipmentSlotIndex < 0 || equipmentSlotIndex >= _equipment.Count)
                {
                    return false;
                }

                // Check inventory capacity
                if (_inventory.Count >= MaxInventorySlots)
                {
                    return false;
                }

                var item = _equipment[equipmentSlotIndex];
                _equipment.RemoveAt(equipmentSlotIndex);

                _inventory.Add(item);

                // Update cache
                if (!_itemTotalQuantities.ContainsKey(item.ItemId))
                {
                    _itemTotalQuantities[item.ItemId] = 0;
                }

                _itemTotalQuantities[item.ItemId] += item.Quantity;

                IsDirty = true;
                return true;
            }
        }
    }

    public bool HasEquippedItem(int itemId)
    {
        lock (_equipment)
        {
            return _equipment.Any(i => i.ItemId == itemId);
        }
    }

    // ApplyDamage, Heal are inherited

    public ServerCharacterData GetSaveData()
    {
        HashSet<string> ordersCopy;
        lock (_orderLock)
        {
            ordersCopy = new HashSet<string>(_processedOrders);
        }

        var data = new ServerCharacterData
        {
            Id = Id,
            ProcessedOrders = ordersCopy,
            Name = Name,
            Hp = _hp,
            Sp = _sp,
            Str = Str,
            Con = Con,
            Int = Int,
            Wis = Wis,
            Agi = Agi,
            Gold = _gold,
            PremiumCurrency = _premiumCurrency,
            DailyGiftAccumulator = DailyGiftAccumulator,
            LastGiftResetDate = LastGiftResetDate,
            ActivePetInstanceId = ActivePetInstanceId,
            InstanceLockouts = new Dictionary<string, DateTime>(InstanceLockouts),
            MapId = MapId,
            X = X,
            Y = Y,
            Element = CharacterElement
        };

        lock (_progressLock)
        {
            data.Exp = _exp;
            data.Level = Level;
            data.RebirthLevel = RebirthLevel;
            data.ExpToNextLevel = ExpToNextLevel;
            data.StatPoints = StatPoints;
        }

        data.Skills = SkillMastery.Select(kvp => new SkillMasteryData
        {
            SkillId = kvp.Key,
            Rank = kvp.Value.Rank,
            UsageCount = kvp.Value.UsageCount
        }).ToList();

        lock (_inventory)
        {
            data.Inventory = _inventory.Select(i => new Item
            {
                ItemId = i.ItemId,
                Name = i.Name,
                Type = i.Type,
                MaxStack = i.MaxStack,
                Quantity = i.Quantity,
                ForgeSuccessRateBonus = i.ForgeSuccessRateBonus,
                Policy = i.Policy,
                BoundToId = i.BoundToId
            }).ToList();
        }

        lock (_equipment)
        {
            data.Equipment = _equipment.Select(i => new Item
            {
                ItemId = i.ItemId,
                Name = i.Name,
                Type = i.Type,
                MaxStack = i.MaxStack,
                Quantity = i.Quantity,
                ForgeSuccessRateBonus = i.ForgeSuccessRateBonus,
                Policy = i.Policy,
                BoundToId = i.BoundToId
            }).ToList();
        }

        lock (_bank)
        {
            data.Bank = _bank.Select(i => new Item
            {
                ItemId = i.ItemId,
                Name = i.Name,
                Type = i.Type,
                MaxStack = i.MaxStack,
                Quantity = i.Quantity,
                ForgeSuccessRateBonus = i.ForgeSuccessRateBonus,
                Policy = i.Policy,
                BoundToId = i.BoundToId
            }).ToList();
        }

        lock (_pets)
        {
            data.Pets = _pets.Select(p => p.GetSaveData()).ToList();
        }

        return data;
    }

    public void LoadSaveData(ServerCharacterData data)
    {
        Id = data.Id;
        lock (_orderLock)
        {
            _processedOrders.Clear();
            if (data.ProcessedOrders != null)
            {
                foreach (var order in data.ProcessedOrders)
                {
                    _processedOrders.Add(order);
                }
            }
        }

        Name = data.Name;
        _hp = data.Hp;
        _sp = data.Sp;
        Str = data.Str;
        Con = data.Con;
        Int = data.Int;
        Wis = data.Wis;
        Agi = data.Agi;
        _gold = data.Gold;
        _premiumCurrency = data.PremiumCurrency;
        DailyGiftAccumulator = data.DailyGiftAccumulator;
        LastGiftResetDate = data.LastGiftResetDate;
        ActivePetInstanceId = data.ActivePetInstanceId;
        InstanceLockouts.Clear();
        if (data.InstanceLockouts != null)
        {
            foreach (var kvp in data.InstanceLockouts)
            {
                InstanceLockouts[kvp.Key] = kvp.Value;
            }
        }
        MapId = data.MapId;
        X = data.X;
        Y = data.Y;
        CharacterElement = data.Element;

        lock (_progressLock)
        {
            _exp = data.Exp;
            Level = data.Level;
            RebirthLevel = data.RebirthLevel;
            ExpToNextLevel = data.ExpToNextLevel;
            StatPoints = data.StatPoints;
        }

        SkillMastery.Clear();
        if (data.Skills != null)
        {
            foreach (var s in data.Skills)
            {
                SkillMastery[s.SkillId] = new SkillMastery
                {
                    Rank = s.Rank,
                    UsageCount = s.UsageCount
                };
            }
        }

        lock (_inventory)
        {
            _inventory.Clear();
            _itemTotalQuantities.Clear();
            if (data.Inventory != null)
            {
                _inventory.AddRange(data.Inventory);
                foreach (var item in _inventory)
                {
                    if (!_itemTotalQuantities.ContainsKey(item.ItemId))
                    {
                        _itemTotalQuantities[item.ItemId] = 0;
                    }

                    _itemTotalQuantities[item.ItemId] += item.Quantity;
                }
            }
        }

        lock (_equipment)
        {
            _equipment.Clear();
            if (data.Equipment != null)
            {
                _equipment.AddRange(data.Equipment);
            }
        }

        lock (_bank)
        {
            _bank.Clear();
            if (data.Bank != null)
            {
                _bank.AddRange(data.Bank);
            }
        }

        lock (_pets)
        {
            _pets.Clear();
            if (data.Pets != null)
            {
                foreach (var petData in data.Pets)
                {
                    var pet = new ServerPet();
                    pet.LoadSaveData(petData);
                    _pets.Add(pet);
                }
            }
        }

        IsDirty = false;

        // Legacy Support & Validation
        if (MonsterId == 0 && CharacterElement == Element.None)
        {
            // If loading legacy data without Element, assign default (Earth)
            // In a real scenario, we might want to prompt user or use random.
            // For now, fail-safe to Earth to prevent lockout.
            CharacterElement = Element.Earth;
            Console.WriteLine($"Warning: Player {Name ?? "Unknown"} loaded with Element.None (Legacy). Defaulted to Earth.");

            // Mark dirty to save the fix
            IsDirty = true;
        }
    }
}