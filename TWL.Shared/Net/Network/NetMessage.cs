using System.Text.Json;

﻿namespace TWL.Shared.Net.Network;

[Serializable]
public class NetMessage
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Opcode Op { get; set; }
    public string JsonPayload { get; set; }

    public static NetMessage? Deserialize(byte[] buffer, int count)
    {
        var span = new ReadOnlySpan<byte>(buffer, 0, count);
        return JsonSerializer.Deserialize<NetMessage>(span, _jsonOptions);
    }
}