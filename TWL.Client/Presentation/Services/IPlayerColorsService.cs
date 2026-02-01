using TWL.Shared.Domain.DTO;

namespace TWL.Client.Presentation.Services;

public interface IPlayerColorsService
{
    PlayerColorsDto? Get(Guid playerId);
}