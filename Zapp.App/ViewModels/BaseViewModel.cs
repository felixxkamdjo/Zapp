using CommunityToolkit.Mvvm.ComponentModel;

namespace Zapp.App.ViewModels;

// encapsulate common reactive viewmodel state and error management
public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    // indicate idle operational status for binding
    public bool IsNotBusy => !IsBusy;

    // execute asynchronous workload safely with state flags
    protected async Task<bool> RunSafeAsync(Func<Task> action)
    {
        if (IsBusy)
        {
            // reject concurrent execution when already running
            return false;
        }

        try
        {
            // set active loading state
            IsBusy = true;
            ErrorMessage = null;
            await action().ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            // capture error message for ui feedback
            ErrorMessage = ex.Message;
            return false;
        }
        finally
        {
            // reset operational state
            IsBusy = false;
        }
    }
}
