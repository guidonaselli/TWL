using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json;
using TWL.Server.Persistence.Database;
using TWL.Shared.Net.Network;

namespace TWL.Server.Simulation.Networking;

public class ClientSession
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly TcpClient _client;
    private readonly DbService _dbService;
    private readonly NetworkStream _stream;

    public int UserId = -1; // se setea tras login

    public ClientSession(TcpClient client, DbService db)
    {
        _client = client;
        _stream = client.GetStream();
        _dbService = db;
    }

    public void StartHandling()
    {
        // Fire and forget the async receive loop
        _ = ReceiveLoopAsync();
    }

    private NetMessage? DeserializeMessage(byte[] buffer, int length)
    {
        var span = new ReadOnlySpan<byte>(buffer, 0, length);
        return System.Text.Json.JsonSerializer.Deserialize<NetMessage>(span, _jsonOptions);
    }

    private async Task ReceiveLoopAsync()
    {
        try
        {
            var buffer = new byte[4096];
            while (true)
            {
                var read = await _stream.ReadAsync(buffer, 0, buffer.Length);
                if (read <= 0) break;

                var netMsg = DeserializeMessage(buffer, read);

                if (netMsg != null)
                {
                    await HandleMessageAsync(netMsg);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Client disconnected: " + ex.Message);
        }
        finally
        {
            _stream.Close();
            _client.Close();
        }
    }

    private async Task HandleMessageAsync(NetMessage msg)
    {
        if (msg == null) return;

        switch (msg.Op)
        {
            case Opcode.LoginRequest:
                await HandleLoginAsync(msg.JsonPayload);
                break;
            case Opcode.MoveRequest:
                HandleMove(msg.JsonPayload);
                break;
            // etc.
        }
    }

    private async Task HandleLoginAsync(string payload)
    {
        // payload podría ser {"username":"xxx","passHash":"abc"}
        var loginDto = JsonConvert.DeserializeObject<LoginDTO>(payload);
        var uid = _dbService.CheckLoginAsync(loginDto.Username, loginDto.PassHash).Result;
        if (uid < 0)
        {
            // login fallido
            await SendAsync(new NetMessage
            {
                Op = Opcode.LoginResponse,
                JsonPayload = "{\"success\":false}"
            });
        }
        else
        {
            UserId = uid;
            // cargar PlayerData
            // mandar una LoginResponse
            await SendAsync(new NetMessage
            {
                Op = Opcode.LoginResponse,
                JsonPayload = "{\"success\":true,\"userId\":" + uid + "}"
            });
        }
    }

    private void HandleMove(string payload)
    {
        // EJ: {"dx":1,"dy":0}
        if (UserId < 0) return; // no logueado

        var moveDto = System.Text.Json.JsonSerializer.Deserialize<MoveDTO>(payload, _jsonOptions);

        if (moveDto == null) return;

        // Actualizar la pos en el server side:
        // PlayerData data = ...
        // data.X += moveDto.dx * speed
        // etc.
        // Broadcast a otros en la misma zona
    }

    private async Task SendAsync(NetMessage msg)
    {
        var bytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(msg);
        await _stream.WriteAsync(bytes, 0, bytes.Length);
    }
}

public class LoginDTO
{
    public string Username { get; set; }
    public string PassHash { get; set; }
}

public class MoveDTO
{
    public float dx { get; set; }
    public float dy { get; set; }
}
