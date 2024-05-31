using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using ClientServerMessager.Model.Clients;
using ClientServerMessager.Model.Messaging;
using ClientServerMessager.Model.Servers;
using ClientServerMessager.Server.ViewModels.Commands;

namespace ClientServerMessager.Server.ViewModels;

public class MainViewModel : AbstractViewModel
{
    private IServer? _server;
    private int _delay = 1;

    #region Bindings

    private bool _isServerNotWorking = true;
    private string _eventText = string.Empty;
    private string _ip = string.Empty;
    private string _port = string.Empty;
    private int _fileIndex = 0;
    private int _driveIndex = 0;
    private ObservableCollection<string> _files = [];
    private ObservableCollection<string> _drives = [];

    public bool IsServerNotWorking
    {
        get => _isServerNotWorking;
        set => SetField(ref _isServerNotWorking, value);
    }

    public string EventText
    {
        get => _eventText;
        set => SetField(ref _eventText, value);
    }

    public string Ip
    {
        get => _ip;
        set => SetField(ref _ip, value);
    }

    public string Port
    {
        get => _port;
        set => SetField(ref _port, value);
    }

    public int DriveIndex
    {
        get => _driveIndex;
        set
        {
            if (SetField(ref _driveIndex, value))
                GetFiles();
        }
    }

    public int FileIndex
    {
        get => _fileIndex;
        set => SetField(ref _fileIndex, value);
    }

    public ObservableCollection<string> Files
    {
        get => _files;
        set => SetField(ref _files, value);
    }

    public ObservableCollection<string> Drives
    {
        get => _drives;
        set => SetField(ref _drives, value);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Запуск сервера.
    /// </summary>
    public Command StartServer => new((_, _) =>
    {
        if (!string.IsNullOrEmpty(Ip) && !string.IsNullOrEmpty(Port))
        {
            int port = int.Parse(Port);
            
            try
            {
                _server = new Model.Servers.Server(Ip, port);
                _server.AcceptedClient += OnServerAcceptedClient;

                _server.ReceivedMessage += OnServerReceivedMessage;

                _server.StartListeningAsync();
            }
            catch
            {
                return;
            }

            _isServerNotWorking = false;
            EventText = EventText + Environment.NewLine + $"Сервер: сервер с Ip {Ip} и портом {Port} был запущен";
        }
    });

    /// <summary>
    /// Получение сервером сообщения. Если это корректный путь, то отправка клиенту файлов лежащих в этой директории.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="line"></param>
    private async void OnServerReceivedMessage(object? sender, string line)
    {
        line = line.Substring(0, line.IndexOf('\0'));
        
        IClient client = sender as IClient;
        // Если есть точка, значит клиент хочет вернуться в самое начало, к выбору диска.
        if (line.Equals("."))
        {
            EventText = EventText + Environment.NewLine + $"({client.Name} {client.SocketClient.RemoteEndPoint}) запросил диски";
            
            var drives = Directory.GetLogicalDrives();
            Message message = new(null, [..drives], []);
            string sendings = JsonSerializer.Serialize(message);
            
            await _server.SendMessageAsync(sendings, client);
        }
        else if (line.IndexOf('.') != -1)
        {
            // Если файл текстовый, прочитать и отправить его содержимое.
            if (line.Contains(".txt"))
            {
                try
                {
                    using Stream stream = File.OpenRead(line);
                    using StreamReader reader = new StreamReader(stream);
                    await _server.SendMessageAsync(reader.ReadToEnd(), client);
                }
                catch
                {
                    await _server.SendMessageAsync("Не удалось прочесть " + line, client);
                }
                EventText = EventText + Environment.NewLine + $"({client.Name} {client.SocketClient.RemoteEndPoint}) запросил прочтение файла {line}";
            }
        }
        else
        {
            // Если это просто путь, то отправить все файлы и папки в этой директории.
            EventText = EventText + Environment.NewLine + $"({client.Name} {client.SocketClient.RemoteEndPoint}) запросил содержимое {line}";
            try
            {
                var files = Directory.GetFileSystemEntries(line);
                Message message = new(line, [..Directory.GetLogicalDrives()], [..files]);
                string sendings = JsonSerializer.Serialize(message);
                await _server.SendMessageAsync(sendings, client);
            }
            catch
            {
                
                await _server.SendMessageAsync("Не удалось открыть директорию " + line, client);
            }
        }
    }

    /// <summary>
    /// Принятие запроса на подключение клиента.
    /// </summary>
    private async void OnServerAcceptedClient(object? sender, IClient client)
    {
        var drives = Directory.GetLogicalDrives();
        Message message = new(null, [..drives], []);
        string sendings = JsonSerializer.Serialize(message);
        await _server.SendMessageAsync(sendings, client);
        
        EventText = EventText + Environment.NewLine + $"Подключился новый клиент ({client.Name} {client.SocketClient.RemoteEndPoint})";
    }

    public Command StopServer => new((_, _) =>
    {
        if (_server != null)
        {
            _server.Dispose();
            _server = null;
        }
    });

    #endregion

    public MainViewModel()
    {
        Ip = "127.0.0.1";
        Port = "666";

        Drives = new(Directory.GetLogicalDrives());
        DriveIndex = 0;
        GetFiles();
    }

    private void GetFiles()
    {
        string[] entries = Directory.GetFileSystemEntries(Drives[DriveIndex]);
        Files = new(entries);
        EventText += "Обновлен список файлов" + Environment.NewLine;
    }
}