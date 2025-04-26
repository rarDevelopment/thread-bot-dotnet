namespace ThreadBot.Models;

public class DatabaseSettings(string cluster, string user, string password, string name)
{
    public string Cluster { get; set; } = cluster;
    public string User { get; set; } = user;
    public string Password { get; set; } = password;
    public string Name { get; set; } = name;
}