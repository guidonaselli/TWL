using System.Net.Sockets;
using System.Text.Json;
using TWL.Server.Persistence.Database;
using TWL.Shared.Net.Network;

namespace TWL.Server.Simulation.Networking;

public class ClientSession
{
    private readonly TcpClient _client;
    private readonly DbService _dbService;
    private readonly NetworkStream _stream;

    // Ensure compatibility with camelCase JSON from clients, matching Newtonsoft behavior
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    public int UserId = -1; // se setea tras login

    public ClientSession(TcpClient client, DbService db)
    {
        _client = client;
        _stream = client.GetStream();
        _dbService = db;
    }

    public void StartHandling()
    {
        var t = new Thread(ReceiveLoop);
        t.Start();
    }

    private void ReceiveLoop()
    {
        try
        {
            var buffer = new byte[4096];
            while (true)
            {
                var read = _stream.Read(buffer, 0, buffer.Length);
                if (read <= 0) break;

                var span = new ReadOnlySpan<byte>(buffer, 0, read);
                var netMsg = JsonSerializer.Deserialize<NetMessage>(span, _jsonOptions);

                if (netMsg != null)
                {
                    HandleMessage(netMsg);
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

    private void HandleMessage(NetMessage msg)
    {
        switch (msg.Op)
        {
            case Opcode.LoginRequest:
                HandleLogin(msg.JsonPayload);
                break;
            case Opcode.MoveRequest:
                HandleMove(msg.JsonPayload);
                break;
            // etc.
        }
    }

    private void HandleLogin(string payload)
    {
        // payload podría ser {"username":"xxx","passHash":"abc"}
        var loginDto = JsonSerializer.Deserialize<LoginDTO>(payload, _jsonOptions);

        if (loginDto == null) return;

        // NOTE: Using synchronous wait on async method is necessary here because
        // we are running in a dedicated Thread (StartHandling creates a Thread).
        // Refactoring to full async/await (Task-based) is outside the scope of this optimization task.
        var uid = _dbService.CheckLoginAsync(loginDto.Username, loginDto.PassHash).GetAwaiter().GetResult();
        if (uid < 0)
        {
            // login fallido
            Send(new NetMessage
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
            Send(new NetMessage
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

        var moveDto = JsonSerializer.Deserialize<MoveDTO>(payload, _jsonOptions);

        if (moveDto == null) return;

        // Actualizar la pos en el server side:
        // PlayerData data = ...
        // data.X += moveDto.dx * speed
        // etc.
        // Broadcast a otros en la misma zona
    }

    private void Send(NetMessage msg)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(msg);
        _stream.Write(bytes, 0, bytes.Length);
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
