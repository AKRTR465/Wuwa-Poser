using System.Collections;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WWPoser;
using WWPoser.Services;

internal static class CharacterSearchUiChecks
{
    internal static int Run(MainWindow window, string root, string output)
    {
        int checks = 0;
        void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
        var catalog = OwnedCharacterCatalog.Load(root);
        var index = new CharacterSearchIndex(catalog.Characters);
        string[] Ids(string query) => index.Search(query).Select(character => character.CharacterId).ToArray();
        string[] aimisi = Ids("爱弥斯");
        Check(aimisi.Length == 2, "Chinese names preserve default and alternate forms as separate candidates");
        foreach (string query in new[] { "aimisi", "AMS", "ai mi si", "ａｉｍｉｓｉ", "ài mí sī" })
            Check(Ids(query).SequenceEqual(aimisi), "Chinese, full pinyin, initials, spaces, case and tone marks agree: " + query);
        Check(index.Search("ams 机甲").Single().Form is not null && index.Search("爱弥斯 jijia").Single().Form is not null,
            "mixed Chinese/pinyin terms narrow a role to its exact form");
        Check(index.Search("guizhi").Single().DisplayName.Contains("桂枝宁芙") && Ids("长离").SequenceEqual(Ids("changli")),
            "skin names and polyphonic role names have phrase-aware pronunciation");
        Check(Ids("qiuyuan").SequenceEqual(Ids("chouyuan")) && index.Search("qiuyuan").Single().RoleId == 1411,
            "alternate readings of a polyphonic surname match the same character");
        Check(Ids("pbz nv").SequenceEqual(Ids("漂泊者 女")) && Ids("nv").SequenceEqual(Ids("nǚ")),
            "Rover variants can be narrowed by gender, including accented u-umlaut pinyin");
        Check(index.Search("dny dark").Single().CharacterId.EndsWith("-form-dark"), "Latin form labels combine with role initials");
        Check(index.Search(" ").Count == catalog.Characters.Count && index.Search("not-a-real-character").Count == 0,
            "empty search restores the catalog and unmatched text yields no candidates");
        Check(catalog.Characters.All(character => CharacterSearchIndex.Romanize(character.DisplayName)
            .All(alias => !alias.Any(letter => letter is >= '\u4e00' and <= '\u9fff'))), "every current role, skin and form label has offline pinyin coverage");

        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        var actors = (IList)typeof(MainWindow).GetField("actors", Private)!.GetValue(window)!;
        var picker = (CharacterPicker)actors[0]!.GetType().GetProperty("Character")!.GetValue(actors[0])!;
        picker.ApplyTemplate();
        var search = (TextBox)picker.Template.FindName("PART_SearchBox", picker);
        var results = (ListBox)picker.Template.FindName("PART_SearchResults", picker);
        var empty = (TextBlock)picker.Template.FindName("NoMatches", picker);
        var popup = (Popup)picker.Template.FindName("PART_Popup", picker);
        Check(search is not null && results is not null && popup is not null, "the actual role selector exposes a search box and candidate list");
        object original = picker.SelectedItem; int changed = 0;
        SelectionChangedEventHandler observe = (_, _) => changed++;
        picker.SelectionChanged += observe;
        try
        {
            search!.Text = "ams"; search.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
            Check(picker.SearchText == "ams" && picker.Results.Select(character => character.CharacterId).SequenceEqual(aimisi),
                "typing into the real template filters pinyin candidates");
            Check(ReferenceEquals(picker.SelectedItem, original) && changed == 0 && picker.Items.Count == catalog.Characters.Count,
                "search does not mutate the committed actor or the complete selector catalog");
            var other = new CharacterPicker { ItemsSource = catalog.Characters, SelectedItem = original };
            Check(other.Results.Count == catalog.Characters.Count, "actors sharing a catalog never share a filter");
            Render(popup!.Child, "character-search-pinyin", 560);
            search.Text = "不存在的角色"; search.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
            Check(picker.Results.Count == 0 && empty.Visibility == Visibility.Visible && ReferenceEquals(picker.SelectedItem, original) && changed == 0,
                "no matches show clear feedback while retaining the previous actor");
            Check(!picker.Commit(null), "an empty result cannot clear or invent an actor");
            Render(popup.Child, "character-search-no-results", 560);

            // A test-owned invisible HWND supplies a valid input source. No
            // game, desktop capture, or visible test window is involved.
            using var input = new HwndSource(new HwndSourceParameters("Character search offline input") { Width = 1, Height = 1, WindowStyle = 0 });
            var host = new Border { Child = other, Width = 560, Height = 400 };
            host.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/WWPoser;component/Theme.xaml") });
            input.RootVisual = host;
            void Pump()
            {
                var frame = new DispatcherFrame();
                window.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () => frame.Continue = false); Dispatcher.PushFrame(frame);
            }
            Pump(); other.ApplyTemplate();
            var otherPopup = (Popup)other.Template.FindName("PART_Popup", other);
            otherPopup.Placement = PlacementMode.AbsolutePoint; otherPopup.HorizontalOffset = -32000; otherPopup.VerticalOffset = -32000;
            try
            {
                other.IsDropDownOpen = true; Pump(); other.SearchText = "ams"; Pump();
                Check(other.IsDropDownOpen && otherPopup.IsOpen && other.Results.Count == 2 && ReferenceEquals(other.SelectedItem, original),
                    "the real offscreen popup stays open while searching and retains the committed actor");
            }
            finally { other.IsDropDownOpen = false; Pump(); }
            Check(other.SearchText == "" && other.Results.Count == catalog.Characters.Count, "closing the real popup resets its transient search");
            void Key(Key key) => search.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, input, 0, key) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
            search.Text = "ams"; search.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
            results!.SelectedIndex = 0; Key(System.Windows.Input.Key.Down);
            Check(results.SelectedIndex == 1 && ReferenceEquals(picker.SelectedItem, original), "arrow keys move the candidate without selecting the actor");
            Key(System.Windows.Input.Key.Enter);
            Check(picker.SelectedItem is OwnedCharacterSelection { RoleId: 1210, Form: not null } && changed == 1 && picker.SearchText == "",
                "Enter commits the exact highlighted form and resets the transient query");
            object committed = picker.SelectedItem;
            search.Text = "shouanren"; search.GetBindingExpression(TextBox.TextProperty)!.UpdateSource(); Key(System.Windows.Input.Key.Escape);
            Check(ReferenceEquals(picker.SelectedItem, committed) && picker.SearchText == "" && picker.Results.Count == catalog.Characters.Count && changed == 1,
                "Escape cancels search and restores the complete list without another actor change");
            picker.SearchText = "changli guizhi";
            Render(popup.Child, "character-search-skin", 560);
            var candidate = (ListBoxItem)results.ItemContainerGenerator.ContainerFromIndex(0);
            results.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                { RoutedEvent = UIElement.PreviewMouseLeftButtonUpEvent, Source = candidate });
            Check(picker.SelectedItem is OwnedCharacterSelection { RoleId: 1205, SkinId: 81011205 } && changed == 2,
                "clicking a real filtered result commits its exact skin through the popup handler");
            picker.SearchText = "not-found";
            picker.SelectedValue = catalog.Characters.First(character => character.RoleId == 1505).CharacterId;
            Check(picker.SelectedItem is OwnedCharacterSelection { RoleId: 1505 }, "preset/programmatic selection can choose any catalog item during a search");
        }
        finally
        {
            picker.SelectionChanged -= observe; picker.SearchText = ""; picker.SelectedItem = original;
        }
        Console.WriteLine($"PASS {checks} character search and real selector checks; offline catalog and invisible input host only.");
        return checks;

        void Render(UIElement view, string name, int width)
        {
            view.Measure(new Size(width, 380)); view.Arrange(new Rect(0, 0, width, Math.Max(90, view.DesiredSize.Height)));
            view.UpdateLayout(); int height = Math.Max(1, (int)Math.Ceiling(view.RenderSize.Height));
            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32); bitmap.Render(view);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var file = File.Create(Path.Combine(output, name + ".png")); encoder.Save(file);
        }
    }
}
