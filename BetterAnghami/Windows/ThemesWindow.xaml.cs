using ColorPicker;
using MRK.Models;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace MRK
{
    /// <summary>
    /// Interaction logic for ThemesWindow.xaml
    /// </summary>
    public partial class ThemesWindow : Window
    {
        public class ColorPropertyEdit(ThemeProperty property, ThemeColor color)
        {
            public ThemeProperty Property { get; init; } = property;
            public ThemeColor Color { get; init; } = color;
        }

        public static Color NormalContainerColor = "#FF151515".ToColor();
        public static Color ActiveContainerColor = "#FF212121".ToColor();

        public static Brush NormalContainerBrush = new SolidColorBrush(NormalContainerColor);
        public static Brush ActiveContainerBrush = new SolidColorBrush(ActiveContainerColor);

        private ThemeMetadata? _selectedTheme;
        private List<ThemeProperty>? _currentThemeProperties;
        private CancellationTokenSource? _searchCancellationTokenSource;

        private readonly Dictionary<ThemeProperty, TextBox> _propertyTextboxes;
        private readonly Dictionary<ThemeProperty, List<ThemeColor>> _propertyColors;
        private ColorPropertyEdit? _currentColorEdit;

        private ThemeMetadata? _lastSelectedLabelOwner;

        /// <summary>
        /// Flag to disable raising of events caused by local changes
        /// </summary>
        private bool _disableLocalEvent;

        /// <summary>
        /// Any changes occurred?
        /// </summary>
        private bool _isThemeDirty;

        private bool IsThemeDirty
        {
            get => _isThemeDirty;
            set
            {
                if (_isThemeDirty != value || saveChangesButton.IsEnabled != value)
                {
                    _isThemeDirty = value;

                    // enable save
                    // disable apply button
                    SetToolbarButtonsState(enableSaveChanges: value, enableApplyTheme: !value);
                }
            }
        }

        public ThemeMetadata? SelectedTheme => _selectedTheme;

        /// <summary>
        /// ThemeManager instance
        /// </summary>
        private static ThemeManager ThemeManager => ThemeManager.Instance;

#nullable disable
        public static ThemesWindow Instance { get; private set; }
#nullable enable

        public ThemesWindow()
        {
            Instance = this;

            _propertyTextboxes = [];
            _propertyColors = [];

            InitializeComponent();
        }

        /// <summary>
        /// Window loaded event handler
        /// </summary>
        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            // attach editor properties itemcontrol status changed handler
            themePropertiesControl.ItemContainerGenerator.StatusChanged += OnPropertiesItemControlStatusChanged;

            // attach themes changed handler
            ThemeManager.ThemesChanged += OnInstalledThemesChanged;

            // load themes
            await ThemeManager.LoadInstalledThemes();
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            // remove themes changed handler
            ThemeManager.ThemesChanged -= OnInstalledThemesChanged;
        }

        /// <summary>
        /// Installed themes changed event handler
        /// </summary>
        private void OnInstalledThemesChanged()
        {
            // invalidate item source
            themesControl.ItemsSource = null;

            // set items source
            themesControl.ItemsSource = ThemeManager.InstalledThemes;

            // show no themes available if empty
            var hasThemes = ThemeManager.InstalledThemes.Count > 0;
            noThemesLabel.Visibility = hasThemes ? Visibility.Collapsed : Visibility.Visible;

            // hide loading label
            loadingLabel.Visibility = Visibility.Collapsed;

            // set selected theme
            // select after a bit
            Utils.DispatchLater(this,
                async () =>
                {
                    var themeToSelect = _selectedTheme;

                    if (themeToSelect == null || !ThemeManager.InstalledThemes.Contains(themeToSelect))
                    {
                        themeToSelect = ThemeManager.InstalledThemes.FirstOrDefault();
                    }

                    await SetSelectedTheme(themeToSelect);

                    // update selected theme
                    UpdateSelectedLabel();
                },
                100);
        }

        /// <summary>
        /// Create themes click handler
        /// </summary>
        private void OnCreateThemeClick(object sender, RoutedEventArgs e)
        {
            // show create theme window
            new CreateThemeWindow().ShowDialog();
        }

        private async void OnThemeClick(object sender, MouseButtonEventArgs e)
        {
            if (((Border)sender).Tag is ThemeMetadata targetTheme && _selectedTheme != targetTheme)
            {
                await SetSelectedTheme(targetTheme);
            }
        }

        /// <summary>
        /// Returns the UI container of the provided metadata
        /// </summary>
        private T? GetThemeContainer<T>(ThemeMetadata themeMetadata, int depth = 1) where T : UIElement
        {
            var contentPresenter = themesControl.ItemContainerGenerator.ContainerFromItem(themeMetadata) as ContentPresenter;
            if (contentPresenter != null && VisualTreeHelper.GetChildrenCount(contentPresenter) > 0)
            {
                UIElement? current = contentPresenter;
                while (depth-- > 0 && current != null)
                {
                    current = VisualTreeHelper.GetChild(current, 0) as UIElement;
                }

                return current as T;
            }

            return null;
        }

        /// <summary>
        /// Sets the provided theme's container background brush
        /// </summary>
        private void SetThemeContainerBrush(ThemeMetadata metadata, Brush brush)
        {
            var container = GetThemeContainer<Border>(metadata, 2);
            if (container != null)
            {
                container.Background = brush;
            }
        }

        /// <summary>
        /// Sets the selected theme
        /// </summary>
        /// <param name="selectedTheme">Theme metadata to load</param>
        /// <param name="properties">If not provided, the theme properties are loaded from ThemeManager</param>
        private async Task SetSelectedTheme(ThemeMetadata? selectedTheme, List<ThemeProperty>? properties = null)
        {
            if (_selectedTheme != null && _selectedTheme != selectedTheme)
            {
                // check for dirty
                if (IsThemeDirty)
                {
                    switch (Utils.ShowDialog(
                            windowTitle: Title,
                            mainInstruction: "You have unsaved changes, save them?",
                            content: "Any unsaved changes will be lost",
                            buttons: [ButtonType.Yes, ButtonType.No, ButtonType.Cancel]
                        ).ButtonType)
                    {
                        // save
                        case ButtonType.Yes:
                            // save theme
                            OnSaveChangesClick(null, null);
                            break;

                        case ButtonType.Cancel:
                            return; // dont change theme
                    }
                }

                // deselect old theme
                SetThemeContainerBrush(_selectedTheme, NormalContainerBrush);
            }

            // set ours as active
            if (selectedTheme != null)
            {
                SetThemeContainerBrush(selectedTheme, ActiveContainerBrush);

                // load up theme data
                _currentThemeProperties = properties ?? await ThemeManager.LoadTheme(selectedTheme);

                // set properties control itemsource
                Utils.DispatchLater(this,
                    () =>
                    {
                        themePropertiesControl.ItemsSource = _currentThemeProperties;
                        editorScrollView.ScrollToTop();
                    }, 100);

                // set loading
                SetThemePropertiesLoadingState(true);

                // update delete theme button
                SetToolbarButtonsState(enableDelete: !selectedTheme.IsBuiltIn);
            }
            else
            {
                // disable all buttons
                SetToolbarButtonsState(
                    enableDelete: false,
                    enableEditCss: false,
                    enableSaveChanges: false,
                    enableApplyTheme: false);

                // clear theme props
                _currentThemeProperties = null;
            }

            _selectedTheme = selectedTheme;

            // clear dirty
            IsThemeDirty = false;

            // clear cache
            _propertyTextboxes.Clear();
            _propertyColors.Clear();

            // remove color edit
            _currentColorEdit = null;

            // hide color picker
            SetColorPickerState(false);
        }

        /// <summary>
        /// Creates the ThemeColor controls from the given textbox
        /// </summary>
        private void InstantiateTextboxColorControls(TextBox textbox)
        {
            // get our colors container
            var parent = VisualTreeHelper.GetParent(textbox);
            var colorContainerControl = (ItemsControl)VisualTreeHelper.GetChild(parent, 0);

            // extract colors
            var colors = ColorUtility.MatchColors(textbox.Text);

            // sort by which comes first
            colors.Sort((x, y) => x.Start.CompareTo(y.Start));
            colorContainerControl.ForceSetItemSource(colors);

            var prop = (ThemeProperty)textbox.Tag;

            // store colors
            _propertyColors[prop] = colors;

            // store owner
            foreach (var color in colors)
            {
                color.Owner = prop;
            }
        }

        /// <summary>
        /// Theme property value textbox text changed handler
        /// </summary>
        private void OnValueTextboxChanged(object sender, TextChangedEventArgs e)
        {
            if (_disableLocalEvent)
            {
                return;
            }

            var textbox = (TextBox)sender;

            // update colors
            InstantiateTextboxColorControls(textbox);

            // property changed, dirty!
            IsThemeDirty = true;

            // set color edit
            AutoStartColorEdit(textbox);
        }

        /// <summary>
        /// Starts a color edit based on <see cref="TextBoxBase.TextChanged"/> or <see cref="UIElement.GotFocus"/> events
        /// </summary>
        private void AutoStartColorEdit(TextBox textbox)
        {
            // get property colors
            var propColors = _propertyColors[(ThemeProperty)textbox.Tag];
            if (propColors.Count == 0)
            {
                // disable the color picker
                SetColorPickerState(false);
                return;
            }

            // if we only have 1 color, start an edit with that
            if (propColors.Count == 1)
            {
                StartColorEdit(propColors[0]);
                return;
            }

            // we have more than one color, choose color based on caret pos

            ThemeColor target = propColors[0];
            foreach (var color in propColors)
            {
                if (color.Start < textbox.CaretIndex)
                {
                    target = color;
                }
            }

            // start edit on chosen color
            StartColorEdit(target);
        }

        /// <summary>
        /// Theme property value textbox loaded handler
        /// </summary>
        private void OnValueTextboxLoaded(object sender, RoutedEventArgs e)
        {
            var textbox = (TextBox)sender;

            // map property to textbox
            _propertyTextboxes[(ThemeProperty)textbox.Tag] = textbox;

            // update colors
            InstantiateTextboxColorControls(textbox);
        }

        /// <summary>
        /// Theme property value textbox lost focus handler
        /// </summary>
        private void OnValueTextboxFocusLost(object sender, RoutedEventArgs e)
        {
            if (_currentColorEdit == null)
            {
                return;
            }

            // hide color picker if color edit property is us
            var prop = (ThemeProperty)((TextBox)sender).Tag;
            if (_currentColorEdit.Property == prop)
            {
                // remove color edit
                _currentColorEdit = null;

                // hide picker
                SetColorPickerState(false);
            }
        }

        /// <summary>
        /// Theme property value textbox selection changed handler
        /// </summary>
        private void OnValueTextboxSelectionChanged(object sender, RoutedEventArgs e)
        {
            // auto start color edit
            AutoStartColorEdit((TextBox)sender);
        }

        /// <summary>
        /// Controls the property loading overlay
        /// </summary>
        private void SetThemePropertiesLoadingState(bool loading)
        {
            themePropertiesLoadingLabel.Visibility = loading ? Visibility.Visible : Visibility.Collapsed;

            // override cursor
            Mouse.OverrideCursor = loading ? Cursors.Wait : null;
        }

        /// <summary>
        /// Properties ItemControl generator status changed handler
        /// </summary>
        private void OnPropertiesItemControlStatusChanged(object? sender, EventArgs e)
        {
            if (themePropertiesControl.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                SetThemePropertiesLoadingState(false);
            }
        }

        /// <summary>
        /// Filters <see cref="_currentThemeProperties"/> with a fuzzy search using <paramref name="query"/>
        /// </summary>
        private async Task FilterProperties(string query, CancellationToken token)
        {
            await Task.Delay(1000, token);

            if (token.IsCancellationRequested)
            {
                return;
            }

            if (string.IsNullOrEmpty(query))
            {
                Dispatcher.Invoke(() => themePropertiesControl.ItemsSource = _currentThemeProperties);
            }
            else
            {
                var result = FuzzySharp.Process.ExtractSorted(
                    new ThemeProperty(query, ""),
                    _currentThemeProperties, (x) => x.Name);

                Dispatcher.Invoke(() => themePropertiesControl.ItemsSource = result
                    .Select(x => x.Value)
                    .ToList());
            }
        }

        /// <summary>
        /// Search box text changed handler
        /// </summary>
        private void OnSearchBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_currentThemeProperties == null) return;

            // cancel old search
            _searchCancellationTokenSource?.Cancel();

            // store query
            var query = searchTextBox.Text.Trim();

            _searchCancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => FilterProperties(query, _searchCancellationTokenSource.Token),
                _searchCancellationTokenSource.Token);
        }

        /// <summary>
        /// Delete theme click handler
        /// </summary>
        private async void OnDeleteThemeClick(object sender, RoutedEventArgs e)
        {
            // we cant delete built in themes
            if (_selectedTheme == null || _selectedTheme.IsBuiltIn)
            {
                return;
            }

            // display confirmation
            if (Utils.ShowDialog(
                    windowTitle: Title,
                    mainInstruction: $"Are you sure you want to delete {_selectedTheme.Name}?",
                    content: "This cannot be undone",
                    buttons: [ButtonType.Yes, ButtonType.Cancel]).ButtonType == ButtonType.Yes)
            {
                // set to false, to prevent the unsaved changes prompt from showing
                IsThemeDirty = false;

                await ThemeManager.RemoveTheme(_selectedTheme);
            }
        }

        /// <summary>
        /// Edit CSS button click handler
        /// </summary>
        private async void OnEditThemeCssClick(object sender, RoutedEventArgs e)
        {
            if (_selectedTheme == null)
            {
                return;
            }

            var window = new CssEditorWindow(_selectedTheme, _currentThemeProperties);
            window.ShowDialog();

            // check if props have changed
            if (window.NewProperties != null)
            {
                // updated current theme props
                await SetSelectedTheme(_selectedTheme, window.NewProperties);

                // set dirty
                IsThemeDirty = true;
            }
        }

        /// <summary>
        /// Save changes button click handler
        /// </summary>
        private async void OnSaveChangesClick(object? sender, RoutedEventArgs? e)
        {
            // do we have unsaved changes?
            if (_selectedTheme == null || _currentThemeProperties == null || !IsThemeDirty)
            {
                return;
            }

            // write new properties
            var error = await ThemeManager.InstallTheme(_selectedTheme, _currentThemeProperties, true);
            if (error != BetterAnghamiError.None)
            {
                Utils.ShowDialog(
                    windowTitle: Title,
                    mainInstruction: "An error has occurred",
                    content: error.ToString(),
                    buttons: [ButtonType.Ok]);

                // keep the dirty flag for now
                return;
            }

            // clean dirty
            IsThemeDirty = false;
        }

        /// <summary>
        /// Apply theme button click handler
        /// </summary>
        private async void OnApplyThemeClick(object sender, RoutedEventArgs e)
        {
            if (_selectedTheme == null || _currentThemeProperties == null || IsThemeDirty)
            {
                return;
            }

            // set selected theme
            ThemeManager.SelectedTheme = _selectedTheme;

            // apply theme properties
            await AnghamiWindow.Instance.ApplyThemeImmediate(_currentThemeProperties);

            // update selected label
            UpdateSelectedLabel();
        }

        /// <summary>
        /// Sets each toolbar button enable state
        /// </summary>
        private void SetToolbarButtonsState(
            bool? enableDelete = null,
            bool? enableEditCss = null,
            bool? enableSaveChanges = null,
            bool? enableApplyTheme = null)
        {
            if (enableDelete.HasValue)
            {
                deleteThemeButton.IsEnabled = enableDelete.Value;
            }

            if (enableEditCss.HasValue)
            {
                editCssButton.IsEnabled = enableEditCss.Value;
            }

            if (enableSaveChanges.HasValue)
            {
                saveChangesButton.IsEnabled = enableSaveChanges.Value;
            }

            if (enableApplyTheme.HasValue)
            {
                applyThemeButton.IsEnabled = enableApplyTheme.Value;
            }
        }

        /// <summary>
        /// Starts a color edit using the given <paramref name="color" /> and displays the color picker
        /// </summary>
        private void StartColorEdit(ThemeColor color)
        {
            if (color.Owner == null)
            {
                return;
            }

            // ensure color picker visible
            SetColorPickerState(true, !_selectedTheme!.IsBuiltIn);

            // set new color edit
            _currentColorEdit = new ColorPropertyEdit(color.Owner, color);

            // set current color in picker
            RunWithNoEvents(() =>
            {
                colorPicker.SelectedColor = color.Color;
            });
        }

        /// <summary>
        /// Theme color rectangle click handler
        /// </summary>
        private void OnThemeColorClick(object sender, MouseButtonEventArgs e)
        {
            var themeColor = (ThemeColor)((Border)sender).Tag;
            StartColorEdit(themeColor);
        }

        /// <summary>
        /// Runs the given action with the <see cref="_disableLocalEvent"/> flag enabled
        /// </summary>
        private void RunWithNoEvents(Action action)
        {
            _disableLocalEvent = true;
            action();
            _disableLocalEvent = false;
        }

        /// <summary>
        /// Color picker ColorChanged event handler
        /// </summary>
        private void OnColorPickerColorChanged(object sender, RoutedEventArgs e)
        {
            if (_currentColorEdit == null || _disableLocalEvent)
            {
                return;
            }

            // update our color and fix other colors' start index

            var selectedColor = colorPicker.SelectedColor;
            var newValue = string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}",
                selectedColor.R,
                selectedColor.G,
                selectedColor.B,
                selectedColor.A);

            // calculate fixup delta for theme colors ahead
            int oldLength = _currentColorEdit.Color.Length;
            int delta = newValue.Length - oldLength;

            // change color into hex
            _currentColorEdit.Color.InternalConstruct(
                    _currentColorEdit.Color.Start,
                    newValue.Length,
                    newValue,
                    ThemeColorType.Hex
                );

            // which colors come next?
            var colors = _propertyColors[_currentColorEdit.Property];

            // flag to know if we have found our color
            bool found = false;
            foreach (var color in colors)
            {
                if (!found)
                {
                    if (color == _currentColorEdit.Color)
                    {
                        found = true;
                    }

                    continue;
                }

                // apply fixup here
                color.Start += delta;
            }

            // update textbox value
            var textbox = _propertyTextboxes[_currentColorEdit.Property];

            // update text
            RunWithNoEvents(() =>
            {
                textbox.Text = textbox.Text.Substring(0, _currentColorEdit.Color.Start) +
                newValue +
                textbox.Text.Substring(_currentColorEdit.Color.Start + oldLength);

                // update property value
                _currentColorEdit.Property.Value = textbox.Text;
            });

            // property changed, dirty!
            IsThemeDirty = true;

            // update source
            var parent = VisualTreeHelper.GetParent(textbox);
            var colorContainerControl = (ItemsControl)VisualTreeHelper.GetChild(parent, 0);
            colorContainerControl.ForceSetItemSource(colors);
        }

        /// <summary>
        /// Controls the color picker visibility and enable status
        /// </summary>
        private void SetColorPickerState(bool visible, bool? enabled = null)
        {
            colorPicker.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

            if (enabled.HasValue)
            {
                colorPicker.IsEnabled = enabled.Value;
            }
        }

        /// <summary>
        /// Controls a ThemeMetadata's selected label visiblity
        /// </summary>
        private void SetSelectedLabelVisible(ThemeMetadata owner, bool visible)
        {
            var container = GetThemeContainer<Border>(owner, 2);
            if (container != null)
            {
                var label = container.FindName("selectedTextBlock") as TextBlock;
                if (label != null)
                {
                    label.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// Updates the selected label visiblity
        /// </summary>
        private void UpdateSelectedLabel()
        {
            var selectedTheme = ThemeManager.SelectedTheme;
            if (_lastSelectedLabelOwner == selectedTheme)
            {
                return;
            }

            // hide old label
            if (_lastSelectedLabelOwner != null)
            {
                SetSelectedLabelVisible(_lastSelectedLabelOwner, false);
            }

            if (selectedTheme != null)
            {
                SetSelectedLabelVisible(selectedTheme, true);
            }

            _lastSelectedLabelOwner = selectedTheme;
        }
    }
}
