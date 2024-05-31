using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ClientServerMessager.Model.Clients;

public class Client(Guid id, string name, Socket socket) : IClient
{
    public Guid Id { get; private set; } = id;
    public string Name { get; private set; } = name;
    public Socket SocketClient { get; } = socket;

    public event EventHandler<string>? ReceivedMessage;

    public Client(Guid id, string name) : this(id, name,
        new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
    {
    }

    /// <summary>
    /// Подключиться к серверу.
    /// </summary>
    public async Task<bool> Connect(IPAddress address, int port)
    {
        try
        {
            await SocketClient.ConnectAsync(address, port);

            byte[] result = new byte[2048];
            int size = await SocketClient.ReceiveAsync(result);
            string message = Encoding.UTF8.GetString(result, 0, size);
            var pair = message.Split(' ');
            Id = Guid.Parse(pair[0]);
            Name = pair[1];
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }

        await Task.Run(async () =>
        {
            while (true)
            {
                byte[] result = new byte[6000];
                int size = await SocketClient.ReceiveAsync(result);
                string message = Encoding.UTF8.GetString(result, 0, size);

                if (!message.Equals("<closed>"))
                    ReceivedMessage?.Invoke(this, message);
                else break;
            }
        });

        return true;
    }

    
    /// <summary>
    /// Отправить сообщение серверу.
    /// </summary>
    public Task SendMessageAsync(string message)
    {
        return Task.Run(async () =>
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            await SocketClient.SendAsync(buffer);
        });
    }

    public bool Equals(IClient? other)
    {
        return other != null && other.Id == Id;
    }
}