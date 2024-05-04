namespace ClientServerMessager.Model.Messaging;

public class Message(string header, List<string> drives, List<string> files)
{
    public string? Header { get; set; } = header;
    public List<string> Drives { get; set; } = drives;
    public List<string> Files { get; set; } = files;

}