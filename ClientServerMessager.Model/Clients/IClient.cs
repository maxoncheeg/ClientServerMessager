using System.Net;
using System.Net.Sockets;

namespace ClientServerMessager.Model.Clients;

public interface IClient : IEquatable<IClient>
{
    public Guid Id { get; }
    public string Name { get; }
    public Socket SocketClient { get; }

    public event EventHandler<string> ReceivedMessage;

    public Task SendMessageAsync(string message);
    public Task<bool> Connect(IPAddress address, int port);
}