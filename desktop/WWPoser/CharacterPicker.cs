using System.Collections;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using WWPoser.Services;

namespace WWPoser;

/// <summary>Search candidates separately from the actor's committed selection.</summary>
public sealed class CharacterPicker : ComboBox
{
    public static readonly DependencyProperty SearchTextProperty = DependencyProperty.Register(nameof(SearchText), typeof(string), typeof(CharacterPicker),
        new FrameworkPropertyMetadata(""));
    public string SearchText { get => (string)GetValue(SearchTextProperty); set => SetValue(SearchTextProperty, value); }
    public ObservableCollection<OwnedCharacterSelection> Results { get; } = new();
    private CharacterSearchIndex index = new([]);
    private TextBox? search;
    private ListBox? results;

    public CharacterPicker()
    {
        DisplayMemberPath = nameof(OwnedCharacterSelection.DisplayName);
        SelectedValuePath = nameof(OwnedCharacterSelection.CharacterId);
        IsTextSearchEnabled = false; MaxDropDownHeight = 380;
        ToolTip = "展开后输入中文、拼音或首字母搜索角色";
        DropDownOpened += (_, _) =>
        {
            SearchText = ""; RefreshResults();
            Dispatcher.BeginInvoke(DispatcherPriority.Input, () => { if (IsDropDownOpen) search?.Focus(); });
        };
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == SearchTextProperty) RefreshResults();
        else if (e.Property == IsDropDownOpenProperty && !(bool)e.NewValue) SearchText = "";
    }

    protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
    {
        base.OnItemsSourceChanged(oldValue, newValue);
        index = new(newValue?.OfType<OwnedCharacterSelection>() ?? []); RefreshResults();
    }

    public override void OnApplyTemplate()
    {
        if (search is not null) search.PreviewKeyDown -= SearchKeyDown;
        if (results is not null) { results.PreviewKeyDown -= SearchKeyDown; results.PreviewMouseLeftButtonUp -= PickResult; results.SelectionChanged -= CandidateChanged; }
        base.OnApplyTemplate();
        search = GetTemplateChild("PART_SearchBox") as TextBox;
        results = GetTemplateChild("PART_SearchResults") as ListBox;
        if (search is not null) search.PreviewKeyDown += SearchKeyDown;
        if (results is not null) { results.PreviewKeyDown += SearchKeyDown; results.PreviewMouseLeftButtonUp += PickResult; results.SelectionChanged += CandidateChanged; }
        RefreshResults();
    }

    private static void CandidateChanged(object sender, SelectionChangedEventArgs e) => e.Handled = true;

    private void RefreshResults()
    {
        var matches = index.Search(SearchText);
        Results.Clear(); foreach (var character in matches) Results.Add(character);
        if (results is not null) results.SelectedItem = SelectedItem is OwnedCharacterSelection selected && matches.Contains(selected) ? selected : matches.FirstOrDefault();
    }

    private void PickResult(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source && ItemsControl.ContainerFromElement(results!, source) is ListBoxItem { Content: OwnedCharacterSelection character })
        { Commit(character); e.Handled = true; }
    }

    internal bool Commit(OwnedCharacterSelection? character)
    {
        if (character is null || !Results.Contains(character) || !Items.Contains(character)) return false;
        SelectedItem = character; IsDropDownOpen = false; SearchText = ""; Focus(); return true;
    }

    private void SearchKeyDown(object sender, KeyEventArgs e)
    {
        if (e.ImeProcessedKey != Key.None && e.Key == Key.ImeProcessed) return;
        switch (e.Key)
        {
            case Key.Down:
            case Key.Up:
                if (results is not null && Results.Count > 0)
                {
                    int step = e.Key == Key.Down ? 1 : -1;
                    results.SelectedIndex = Math.Clamp(results.SelectedIndex + step, 0, Results.Count - 1);
                    results.ScrollIntoView(results.SelectedItem);
                }
                e.Handled = true; break;
            case Key.Enter: Commit(results?.SelectedItem as OwnedCharacterSelection); e.Handled = true; break;
            case Key.Escape: IsDropDownOpen = false; SearchText = ""; Focus(); e.Handled = true; break;
            case Key.Tab: IsDropDownOpen = false; SearchText = ""; break;
        }
    }

    protected override void OnTextInput(TextCompositionEventArgs e)
    {
        if (!IsDropDownOpen && !string.IsNullOrEmpty(e.Text))
        { IsDropDownOpen = true; SearchText = e.Text; e.Handled = true; }
        base.OnTextInput(e);
    }
}
