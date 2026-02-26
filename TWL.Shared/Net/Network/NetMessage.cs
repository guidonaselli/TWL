using System.Text.Json;

namespace TWL.Shared.Net.Network;

[Serializable]
public class NetMessage
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Opcode Op { get; set; }

    /// <summary>
    /// Protocol Schema Version.
    /// Used to enforce strict version matching between client and server.
    /// If null (legacy client) or mismatched, the message is rejected.
    /// </summary>
    public int? SchemaVersion { get; set; }

    public string JsonPayload { get; set; }

    /// <summary>
    /// Unique nonce per message for replay protection.
    /// Legacy clients may omit this field (nullable for backward safety).
    /// </summary>
    public string? Nonce { get; set; }

    /// <summary>
    /// UTC timestamp of message creation for freshness validation.
    /// Legacy clients may omit this field (nullable for backward safety).
    /// </summary>
    public DateTime? TimestampUtc { get; set; }

    /// <summary>
    /// Protocol Schema Version for compatibility validation.
    /// Used for Fail-Closed security.
    /// </summary>
    public int? SchemaVersion { get; set; }

    public static NetMessage? Deserialize(byte[] buffer, int count)
    {
        var span = new ReadOnlySpan<byte>(buffer, 0, count);
        return JsonSerializer.Deserialize<NetMessage>(span, _jsonOptions);
    }
}