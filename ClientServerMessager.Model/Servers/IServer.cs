using ClientServerMessager.Model.Clients;

namespace ClientServerMessager.Model.Servers;

public interface IServer : IDisposable
{
    public IReadOnlyCollection<IClient> Clients { get; }

    public event EventHandler<string> ReceivedMessage;
    public event EventHandler<IClient> AcceptedClient;

    public Task StartListeningAsync();
    public Task SendMessageAsync(string message);
    public Task SendMessageAsync(string message, IClient client);
    public Task<bool> AddClientAsync(IClient client);
    public Task<bool> RemoveClientAsync(Guid id);
}