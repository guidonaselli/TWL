using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using TWL.Server.Persistence.Database;
using TWL.Shared.Net;
using TWL.Shared.Net.Network;

namespace TWL.Server.Simulation.Networking;

public class ClientSession
{
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

    private async Task ReceiveLoopAsync()
    {
        try
        {
            var buffer = new byte[4096];
            while (true)
            {
                var read = await _stream.ReadAsync(buffer, 0, buffer.Length);
                if (read <= 0) break;

                var msgStr = Encoding.UTF8.GetString(buffer, 0, read);
                var netMsg = JsonConvert.DeserializeObject<NetMessage>(msgStr);
                await HandleMessageAsync(netMsg);
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

        // Use async DB call
        var uid = await _dbService.CheckLoginAsync(loginDto.Username, loginDto.PassHash);

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

        var moveDto = JsonConvert.DeserializeObject<MoveDTO>(payload);
        // Actualizar la pos en el server side:
        // PlayerData data = ...
        // data.X += moveDto.dx * speed
        // etc.
        // Broadcast a otros en la misma zona
    }

    private void Send(NetMessage msg)
    {
        var str = JsonConvert.SerializeObject(msg);
        var bytes = Encoding.UTF8.GetBytes(str);
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
