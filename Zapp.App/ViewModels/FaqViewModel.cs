using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Zapp.App.ViewModels;

// encapsulate expandable faq item model
public partial class FaqItem : ObservableObject
{
    public string Title { get; }
    public string Subtitle { get; }
    public string Content { get; }
    public string IconGlyph { get; }

    [ObservableProperty]
    private bool _isExpanded;

    public FaqItem(string title, string subtitle, string content, string iconGlyph, bool isExpanded = false)
    {
        Title = title;
        Subtitle = subtitle;
        Content = content;
        IconGlyph = iconGlyph;
        IsExpanded = isExpanded;
    }

    // toggle item accordion expansion
    [RelayCommand]
    private void ToggleExpand() => IsExpanded = !IsExpanded;
}

// manage faq directory and offline assistant interactions
public partial class FaqViewModel : BaseViewModel
{
    private readonly List<FaqItem> _allFaqItems = [];

    public ObservableCollection<FaqItem> FilteredFaqItems { get; } = [];

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    public FaqViewModel()
    {
        Title = "FAQ & Assistant";
        InitializeFaqData();
    }

    // populate static knowledge base entries
    private void InitializeFaqData()
    {
        _allFaqItems.Add(new FaqItem(
            "Portée physique",
            "Distance, obstacles, environnement",
            "La portée dépend des modules BLE : généralement 10 à 30 mètres en espace ouvert. Les murs en béton atténuent fortement le signal radio.",
            "\ue63e"
        ));

        _allFaqItems.Add(new FaqItem(
            "Mode Mesh",
            "Relais, Store & Forward",
            "Le réseau choisit automatiquement le chemin disponible le plus fiable. Les relais ne peuvent pas lire le contenu chiffré.",
            "\ue1eb",
            isExpanded: true
        ));

        _allFaqItems.Add(new FaqItem(
            "Consommation batterie",
            "Optimisation, modes d'économie",
            "L'écoute BLE passive consomme très peu d'énergie. En arrière-plan, la fréquence de scan est réduite pour préserver l'autonomie.",
            "\ue1a4"
        ));

        _allFaqItems.Add(new FaqItem(
            "Chiffrement",
            "Sécurité, identités, clés",
            "Chaque message utilise une paire asymétrique Ed25519. Seul le possesseur de la clé privée associée peut déchiffrer vos messages.",
            "\ue897"
        ));

        ApplyFilter();
    }

    // filter items when search query updates
    partial void OnSearchQueryChanged(string value) => ApplyFilter();

    // refresh filtered list based on query text
    private void ApplyFilter()
    {
        FilteredFaqItems.Clear();
        var query = SearchQuery?.Trim() ?? string.Empty;

        var items = string.IsNullOrWhiteSpace(query)
            ? _allFaqItems
            : _allFaqItems.Where(i => i.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                      i.Content.Contains(query, StringComparison.OrdinalIgnoreCase));

        foreach (var item in items)
        {
            FilteredFaqItems.Add(item);
        }
    }
}
