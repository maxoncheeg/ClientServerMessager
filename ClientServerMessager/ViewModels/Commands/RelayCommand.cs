using System.Windows.Input;

namespace ClientServerMessager.ViewModels.Commands;

public class RelayCommand : ICommand
{
    private Action<object> _action;
    
    public bool CanExecute(object? parameter) => _action != null;

    public void Execute(object? parameter) => _action?.Invoke(parameter);

    public event EventHandler? CanExecuteChanged;

    public RelayCommand(Action<object> method) => _action += method;
}