namespace TWL.Shared.Net.Payloads
{
    public class LoginResponseDto
    {
        public bool Success { get; set; }
        public int UserId { get; set; }
        public float PosX { get; set; }
        public float PosY { get; set; }
        public int Hp { get; set; }
        public int MaxHp { get; set; }
        public string ErrorMessage { get; set; }
    }
}
