using System.Collections.ObjectModel;
using System.Net;
using System.Text.Json;
using ClientServerMessager.Model.Clients;
using ClientServerMessager.Model.Messaging;
using ClientServerMessager.ViewModels.Commands;

namespace ClientServerMessager.ViewModels;

public class MainViewModel : AbstractViewModel
{
    private IClient _client;

    private string _prev;

    private bool _receivingFiles;
    private bool _driveWork;
    private List<string> _receivedFiles;

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
            _driveIndex = value;
            OnPropertyChanged();
            if (Drives[_driveIndex].Length == 3 && _prev != Drives[DriveIndex])
            {
                _prev = Drives[DriveIndex];
                SendFile(Drives[DriveIndex]);
            }
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
    /// Команда подключения к серверу. 
    /// </summary>
    public Command StartServer => new(async (_, _) =>
    {
        if (!string.IsNullOrEmpty(Ip) && !string.IsNullOrEmpty(Port))
        {
            int port = int.Parse(Port);
            var ip = IPAddress.Parse(Ip);

            try
            {
                _client = new Client(Guid.NewGuid(), string.Empty);

                _client.ReceivedMessage += OnClientReceivedMessage;

                await _client.Connect(ip, port);
            }
            catch
            {
                return;
            }

            _isServerNotWorking = false;
            EventText = EventText + Environment.NewLine + $"Сервер: сервер с Ip {Ip} и портом {Port} был запущен";
        }
    });

    public RelayCommand ChooseFile => new(async parameter => { await SendFile(parameter.ToString()); });

    /// <summary>
    /// Событие при получении сообщения от клиента. Вывод отправленных путей в приложении.
    /// </summary>
    private async void OnClientReceivedMessage(object? sender, string s)
    {
        try
        {
            Message message = JsonSerializer.Deserialize<Message>(s);
            if (message == null) throw new ApplicationException();

            _receivedFiles = new() { ".", ".." };
            _receivedFiles.AddRange(message.Files);
            Files = new(_receivedFiles);

            if (message.Drives.Count > 0)
            {
                Drives = [..message.Drives];
            }
            if (message.Header != null)
            {
                Drives.Add(message.Header);
                await Task.Delay(1);
                DriveIndex = Drives.Count - 1;
                await Task.Delay(1);
                OnPropertyChanged(nameof(Drives));
                OnPropertyChanged(nameof(DriveIndex));
            }
        }
        catch (Exception e)
        {
            EventText += s + Environment.NewLine;
        }
    }

    public Command Send => new(async (_, _) =>
    {
        var file = Files[FileIndex];
        await SendFile(file);
    });

    #endregion

    public MainViewModel()
    {
        Ip = "127.0.0.1";
        Port = "666";

        //Drives = new(Directory.GetLogicalDrives());
        //DriveIndex = 0;
        //GetFiles();
    }

    private async Task SendFile(string file)
    {
        if (file == "..")
        {
            file = Drives[0].Length < 3 ? "." : Drives[0][..^1][..(Drives[0].LastIndexOf('\\') + 1)];
            Drives = new();
        }

        await _client.SendMessageAsync(file);
    }

    private void GetFiles()
    {
        //string[] entries = Directory.GetFileSystemEntries(Drives[DriveIndex]);
        //Files = new(entries);
        EventText += "Обновлен список файлов" + Environment.NewLine;
    }
}