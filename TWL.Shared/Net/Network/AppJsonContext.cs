using System.Text.Json.Serialization;
using TWL.Shared.Net.Messages;

namespace TWL.Shared.Net.Network;

[JsonSerializable(typeof(ServerMessage))]
[JsonSerializable(typeof(ClientMessage))]
[JsonSerializable(typeof(NetMessage))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
public partial class AppJsonContext : JsonSerializerContext
{
}