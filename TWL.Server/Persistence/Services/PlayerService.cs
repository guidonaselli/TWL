// // TWL.Server/Services/PlayerService.cs
// using TWL.Shared.Domain.Characters;
// using TWL.Shared.Domain.DTO;
//
// public class PlayerService
// {
//     private readonly IPlayerRepository _repo;  // abstracción de acceso a datos
//
//     public PlayerService(IPlayerRepository repo)
//         => _repo = repo;
//
//     public PlayerColorsDto GetColors(Guid playerId)
//         => _repo.FetchPlayerColors(playerId);
//
//     public PlayerCharacter LoadPlayer(Guid playerId)
//     {
//         var data = _repo.FetchPlayerData(playerId);
//         var pc   = new PlayerCharacter(data.Name, data.Element);
//         // hidrata stats:
//         pc.SetStats(/*level, exp, str...*/);
//         pc.Inventory = data.Inventory;
//         pc.Pets      = data.Pets;
//         pc.Gold      = data.Gold;
//         pc.TwlPoints = data.TwlPoints;
//         return pc;
//     }
//
//     public void SavePlayer(PlayerCharacter pc)
//         => _repo.PersistPlayer(pc);
// }