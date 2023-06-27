namespace ThreadBot;

public class DatabaseSettings
{
    public DatabaseSettings(string cluster, string user, string password, string name)
    {
        Cluster = cluster;
        User = user;
        Password = password;
        Name = name;
    }

    public string Cluster { get; set; }
    public string User { get; set; }
    public string Password { get; set; }
    public string Name { get; set; }
}