using TWL.Shared.Domain.DTO;
using TWL.Server.Simulation.Networking;

namespace TWL.Server.Simulation.Managers;

/// <summary>
/// Server-authoritative service for handling item compound (enhancement) operations.
/// </summary>
public interface ICompoundService
{
    /// <summary>
    /// Processes a compound request from a character.
    /// </summary>
    /// <param name="character">The character performing the compound.</param>
    /// <param name="request">The compound request details.</param>
    /// <returns>A response DTO containing the outcome.</returns>
    Task<CompoundResponseDTO> ProcessCompoundRequest(ServerCharacter character, CompoundRequestDTO request);
}
