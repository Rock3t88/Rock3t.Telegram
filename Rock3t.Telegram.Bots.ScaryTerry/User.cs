namespace Rock3t.Telegram.Bots.ScaryTerry;

public class User
{
    public int Id { get; set; } = -1;
    public long UserId { get; set; } = -1;
    public string Name { get; set; } = string.Empty;
    public DateTime Created { get; set; } = DateTime.Now;
    public DateTime LastUpdate { get; set; } = DateTime.Now;

    public User(string name, long userId)
    {
        Name = name;
        UserId = userId;
    }

    public User()
    {

    }
}