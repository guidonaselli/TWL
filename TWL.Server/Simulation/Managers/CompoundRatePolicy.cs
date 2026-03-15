using TWL.Shared.Domain.Models;

namespace TWL.Server.Simulation.Managers
{
    public interface ICompoundRatePolicy
    {
        double GetSuccessChance(Item targetItem, Item materialItem);
    }

    public class CompoundRatePolicy : ICompoundRatePolicy
    {
        // Base success chance is 90% for this initial implementation.
        private const double BaseSuccessChance = 0.9;

        public double GetSuccessChance(Item targetItem, Item materialItem)
        {
            // For now, we'll return a fixed value.
            // CMP-03: The real calculation will depend on enhancement level and material bonuses.
            // This will be implemented in a later task.
            return BaseSuccessChance;
        }
    }
}
