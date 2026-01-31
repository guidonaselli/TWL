using System.Text.Json;

namespace TWL.Server.Persistence;

public class FilePlayerRepository : IPlayerRepository
{
    private readonly string _saveDirectory;
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public FilePlayerRepository(string saveDirectory = "Data/Saves")
    {
        _saveDirectory = saveDirectory;
        if (!Directory.Exists(_saveDirectory))
        {
            Directory.CreateDirectory(_saveDirectory);
        }
    }

    public async Task SaveAsync(int userId, PlayerSaveData data)
    {
        var filePath = Path.Combine(_saveDirectory, $"{userId}.json");
        var tmpPath = filePath + ".tmp";

        var json = JsonSerializer.Serialize(data, _jsonOptions);
        await File.WriteAllTextAsync(tmpPath, json);

        // Atomic move
        File.Move(tmpPath, filePath, overwrite: true);
    }

    public PlayerSaveData? Load(int userId)
    {
        var filePath = Path.Combine(_saveDirectory, $"{userId}.json");
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<PlayerSaveData>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading save for user {userId}: {ex.Message}");
            return null;
        }
    }
}
