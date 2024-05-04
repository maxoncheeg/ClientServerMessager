using System.Windows.Input;

namespace ClientServerMessager.ViewModels.Commands;

public class Command :ICommand
{
    public Command(EventHandler method) => CanExecuteChanged += method;
    
    public bool CanExecute(object? parameter) => CanExecuteChanged != null;

    public void Execute(object? parameter) => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    public event EventHandler? CanExecuteChanged;
}