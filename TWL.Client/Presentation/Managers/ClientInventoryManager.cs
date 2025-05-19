// File: `TWL.Client/Managers/ClientInventoryManager.cs`

using TWL.Shared.Domain.Requests;

namespace TWL.Client.Presentation.Managers;

public class ClientInventoryManager
{
    public void OnInventoryUpdate(InventoryUpdate update)
    {
        // Process the inventory update.
        // For example, update the local player's item list with update.Items.
    }
}