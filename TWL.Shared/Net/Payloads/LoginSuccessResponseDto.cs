namespace TWL.Shared.Net.Payloads;

public class LoginSuccessResponseDto
{
    public int PlayerId { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public int Hp { get; set; }

    public int MaxHp { get; set; }
    // ... otras estadísticas y datos del jugador que se necesiten en el cliente
}