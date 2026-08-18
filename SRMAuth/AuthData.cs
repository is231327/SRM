public class AuthData
{
    public string Username { get; set; } = "";
    public string Token { get; set; } = "";
    public DateTime TokenExpire { get; set; } = DateTime.MinValue;
    public int AccessLevel { get; set; } = 0;
}
