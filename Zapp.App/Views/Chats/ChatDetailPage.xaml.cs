using Zapp.App.ViewModels;

namespace Zapp.App.Views.Chats;

// render peer conversation thread and bind chat viewmodel
public partial class ChatDetailPage : ContentPage
{
    public ChatDetailPage(ChatsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
