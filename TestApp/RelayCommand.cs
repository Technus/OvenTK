using System.Windows.Input;

namespace OvenTK.TestApp;

public class RelayCommand<T> : ICommand
{
    private readonly Action<T> _action;
    private readonly Func<T, bool>? _predicate;

    public RelayCommand(Action<T> action, Func<T, bool>? predicate = default)
    {
        _action = action;
        _predicate = predicate;
        CanExecuteChanged += static (x, o) => CommandManager.InvalidateRequerySuggested();
    }

    public event EventHandler? CanExecuteChanged;

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    public bool CanExecute(object? parameter) => _predicate?.Invoke((T)parameter!) ?? true;

    public void Execute(object? parameter)
    {
        if (CanExecute(parameter))
            _action?.Invoke((T)parameter!);
    }
}

public class RelayCommand : ICommand
{
    private readonly Action _action;
    private readonly Func<bool>? _predicate;

    public RelayCommand(Action action, Func<bool>? predicate = default)
    {
        _action = action;
        _predicate = predicate;
        CanExecuteChanged += static (x, o) => CommandManager.InvalidateRequerySuggested();
    }

    public event EventHandler? CanExecuteChanged;

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    public bool CanExecute(object? parameter) => _predicate?.Invoke() ?? true;

    public void Execute(object? parameter)
    {
        if (CanExecute(parameter))
            _action?.Invoke();
    }
}
