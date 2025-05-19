// TWL.Shared/Domain/Battle/TurnManager.cs

using TWL.Shared.Domain.Characters;

namespace TWL.Shared.Domain.Battle;

public class TurnManager
{
    readonly Queue<Character> _order = new();
    public void BeginBattle(IEnumerable<Character> allies, IEnumerable<Character> enemies)
    {
        _order.Clear();
        foreach(var c in allies)  _order.Enqueue(c);
        foreach(var c in enemies) _order.Enqueue(c);
    }

    public Character Next() => _order.Dequeue();
    public void EndTurn(Character c) => _order.Enqueue(c);
}