using System.Net;
using System.Net.Sockets;
using System.Text;
using ClientServerMessager.Model.Clients;

namespace ClientServerMessager.Model.Servers;

public class Server : IServer
{
    private bool _listening = false;
    private readonly Socket _server;
    private readonly List<IClient> _clients = [];

    public IReadOnlyCollection<IClient> Clients => _clients;
    public event EventHandler<string>? ReceivedMessage;
    public event EventHandler<IClient> AcceptedClient;

    public Server(string ip, int port, int backlog = 10)
    {
        if (!IPAddress.TryParse(ip, out var address))
            throw new ArgumentException("Неправильно задан IP", nameof(ip));
        if (port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
            throw new ArgumentException("Заданный порт не входит в размеры порта", nameof(port));

        _server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _server.Bind(new IPEndPoint(address, port)); // ip 127.0.0.1
        _server.Listen(10);
    }

    public Task StartListeningAsync()
    {
        _listening = true;

        return Task.Run(async () =>
        {
            while (_listening)
            {
                Socket socket = await _server.AcceptAsync();
                IClient client = new Client(Guid.NewGuid(), $"client{_clients.Count}", socket);

                byte[] buffer = Encoding.UTF8.GetBytes($"{client.Id} {client.Name}");
                await client.SocketClient.SendAsync(buffer);
                AcceptedClient?.Invoke(this, client);
                await AddClientAsync(client);
            }
        });
    }

    public Task SendMessageAsync(string message)
    {
        return Task.Run(async () =>
        {
            for (int i = 0; i < _clients.Count; i++)
                if (message.Equals("<closed>"))
                    await RemoveClientAsync(_clients[i--].Id);
                else
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(message);
                    await _clients[i].SocketClient.SendAsync(buffer);
                }
        });
    }

    public Task SendMessageAsync(string message, IClient client)
    {
        return Task.Run(async () =>
        {
            if (message.Equals("<closed>"))
                await RemoveClientAsync(client.Id);
            else
            {
                byte[] buffer = Encoding.UTF8.GetBytes(message);
                await client.SocketClient.SendAsync(buffer);
            }
        });
    }

    public async Task<bool> AddClientAsync(IClient client)
    {
        if (_clients.Contains(client)) return false;
        _clients.Add(client);

        Task.Run(async () =>
        {
            while (_clients.Contains(client))
            {
                byte[] buffer = new byte[6000];

                try
                {
                    await client.SocketClient.ReceiveAsync(buffer);
                }
                catch (Exception e)
                {
                    _clients.Remove(client);
                    Console.WriteLine("error");
                    Console.WriteLine(e);
                    break;
                }

                string message = Encoding.UTF8.GetString(buffer);
                ReceivedMessage?.Invoke(client, message);
            }
        });

        return true;
    }

    public async Task<bool> RemoveClientAsync(Guid id)
    {
        IClient? client = _clients.FirstOrDefault(c => c.Id == id);
        if (client is null) return false;

        byte[] buffer = Encoding.UTF8.GetBytes("<closed>");
        await client.SocketClient.SendAsync(buffer);
        _clients.Remove(client);
        return true;
    }

    public void Dispose()
    {
        _server.Dispose();
    }
}