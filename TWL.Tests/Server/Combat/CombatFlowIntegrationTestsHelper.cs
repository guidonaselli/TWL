using TWL.Server.Simulation.Managers;
namespace TWL.Tests.Server.Combat;
public partial class CombatFlowIntegrationTests
{
    private static object? GetEncounterField(CombatManager manager, int id)
    {
        var field = typeof(CombatManager).GetField("_encounters", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var dict = (System.Collections.Concurrent.ConcurrentDictionary<int, TWL.Server.Features.Combat.ITurnEngine>)field!.GetValue(manager)!;
        dict.TryGetValue(id, out var engine);
        return engine;
    }

    private static void AddEquipmentToCharacter(TWL.Server.Simulation.Networking.ServerCharacter character, TWL.Shared.Domain.Models.Item item)
    {
        var method = typeof(TWL.Server.Simulation.Networking.ServerCharacter).GetMethod("EquipItemForTest", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (method != null)
        {
            method.Invoke(character, new object[] { item });
        }
        else
        {
            var field = typeof(TWL.Server.Simulation.Networking.ServerCharacter).GetField("_equipment", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var list = (System.Collections.Generic.List<TWL.Shared.Domain.Models.Item>)field!.GetValue(character)!;
            list.Add(item);
        }
    }

    private static void AddPetToCharacter(TWL.Server.Simulation.Networking.ServerCharacter character, TWL.Server.Simulation.Networking.ServerPet pet)
    {
        var method = typeof(TWL.Server.Simulation.Networking.ServerCharacter).GetMethod("AddPetForTest", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (method != null)
        {
            method.Invoke(character, new object[] { pet });
        }
        else
        {
            var field = typeof(TWL.Server.Simulation.Networking.ServerCharacter).GetField("_pets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var list = (System.Collections.Generic.List<TWL.Server.Simulation.Networking.ServerPet>)field!.GetValue(character)!;
            list.Add(pet);
        }
    }
}
