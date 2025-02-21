#r "System.Windows.Forms.dll"
#r "System.Drawing.dll"
#r "nuget: Microsoft.Windows.Compatibility, 3.1.0"
#r "nuget: Accessibility, 4.6.0-preview3-27504-2"

using System;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Win32;
using System.Drawing.Drawing2D;

/// <summary>
/// Modern Windows Forms UI library with automatic theming support
/// Provides Windows 11 style components with dark/light mode support
/// </summary>
#nullable enable

/// <summary>
/// Manages window creation and styling with modern Windows 11 features
/// Handles DPI awareness, dark mode, and rounded corners automatically
/// </summary>
public class WindowManager
{
    public WindowManager()
    {
        // Enable modern visual styles
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Enable DPI awareness
        if (Environment.OSVersion.Version.Major >= 6)
        {
            SetProcessDPIAware();
        }
    }

    // DPI awareness P/Invoke
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool SetProcessDPIAware();

    // P/Invoke declarations for dark mode
    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    // P/Invoke declarations for rounded corners
    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref DWM_WINDOW_CORNER_PREFERENCE attrValue, int attrSize);

    private bool _isDarkMode = false;

    private enum DWM_WINDOW_CORNER_PREFERENCE
    {
        DWMWCP_DEFAULT = 0,
        DWMWCP_DONOTROUND = 1,
        DWMWCP_ROUND = 2,
        DWMWCP_ROUNDSMALL = 3
    }

    // Method to enable rounded corners
    private static void EnableRoundedCorners(Form form)
    {
        if (Environment.OSVersion.Version.Major >= 10)
        {
            DWM_WINDOW_CORNER_PREFERENCE preference = DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
            DwmSetWindowAttribute(form.Handle, 33, ref preference, sizeof(int));
        }
    }

    // Method to check the Windows theme
    private bool IsWindowsInDarkMode()
    {
        try
        {
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
            {
                object? value = key?.GetValue("AppsUseLightTheme");
                _isDarkMode = value != null && (int)value == 0;
                return _isDarkMode;
            }
        }
        catch
        {
            _isDarkMode = false;
            return _isDarkMode;
        }
    }

    // Get Windows accent color
    private Color GetWindowsAccentColor()
    {
        try
        {
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM"))
            {
                if (key?.GetValue("ColorizationColor") is int colorValue)
                {
                    return Color.FromArgb(
                        (byte)((colorValue >> 24) & 0xFF),
                        (byte)((colorValue >> 16) & 0xFF),
                        (byte)((colorValue >> 8) & 0xFF),
                        (byte)(colorValue & 0xFF)
                    );
                }
            }
        }
        catch { }

        return SystemColors.Highlight;
    }

    // Helper method to enable dark mode
    private static void UseImmersiveDarkMode(Form form, bool enabled) // All this does is change the title bar color
    {
        if (Environment.OSVersion.Version.Major >= 10)
        {
            int attribute = DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1;
            if (Environment.OSVersion.Version.Build >= 18985)
            {
                attribute = DWMWA_USE_IMMERSIVE_DARK_MODE;
            }

            int useImmersiveDarkMode = enabled ? 1 : 0;
            DwmSetWindowAttribute(form.Handle, attribute, ref useImmersiveDarkMode, sizeof(int));
        }
    }

    /// <summary>
    /// Adds a global keyboard shortcut handler to the form
    /// </summary>
    /// <param name="form">Target form</param>
    /// <param name="key">Key to monitor</param>
    /// <param name="handler">Event handler to execute</param>
    public void AddKeyEventHandler(Form form, System.Windows.Forms.Keys key, KeyEventHandler handler)
    {
        form.KeyPreview = true;
        form.KeyDown += (s, e) =>
        {
            if (e.KeyCode == key)
            {
                handler(s, e);
            }
        };
    }

    public void CreateThemeButton(Form form, MenuStrip? menuStrip = null)
    {
        const int buttonSize = 40;
        const int margin = 20;

        Button themeButton = new Button
        {
            Text = _isDarkMode ? "🌞" : "🌓",
            Font = new Font("Segoe UI Emoji", 12f),
            Width = buttonSize,
            Height = buttonSize,
            Location = new Point(
                form.ClientSize.Width - buttonSize - margin,
                form.ClientSize.Height - buttonSize - margin * 2
            ),
            FlatStyle = FlatStyle.Flat,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };

        themeButton.Click += (s, e) =>
        {
            themeButton.Text = _isDarkMode ? "🌞" : "🌓";
            ChangeTheme(form, menuStrip);
        };

        form.Controls.Add(themeButton);
    }

    /// <summary>
    /// Creates a new window with modern styling and theme support
    /// </summary>
    /// <param name="form">The form to initialize</param>
    /// <param name="menuStrip">Optional menu strip to style</param>
    public void CreateForm(Form form, MenuStrip? menuStrip = null)
    {
        form.AutoScaleMode = AutoScaleMode.Dpi;

        form.Load += (s, e) =>
        {
            var themeManager = new ThemeManager(form, menuStrip);
            themeManager.ApplyTheme(IsWindowsInDarkMode());
            UseImmersiveDarkMode(form, _isDarkMode);
            EnableRoundedCorners(form); // Enable rounded corners
        };
        Application.Run(form);
    }

    private void ChangeTheme(Form form, MenuStrip? menuStrip = null)
    {
        _isDarkMode = !_isDarkMode;
        var themeManager = new ThemeManager(form, menuStrip);
        themeManager.ApplyTheme(_isDarkMode);
        UseImmersiveDarkMode(form, _isDarkMode);
    }

    /// <summary>
    /// Creates a collapsible panel with modern Windows 11 styling
    /// </summary>
    /// <param name="title">Header text for the panel</param>
    /// <param name="headerColor">Optional custom header color</param>
    /// <returns>A styled Panel with collapse functionality</returns>
    public Panel CreateCollapsiblePanel(string title, Color? headerColor = null)
    {
        // Default to a modern blue if no color is provided
        var defaultHeaderColor = Color.FromArgb(51, 153, 255);
        var actualHeaderColor = headerColor ?? defaultHeaderColor;

        var panel = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(12), // Increased padding
            Margin = new Padding(0, 0, 0, 12)  // Increased margin
        };

        // Store the current color in the panel's Tag
        panel.Tag = new CollapsiblePanelData { HeaderColor = actualHeaderColor };

        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 48, // Increased height for better touch targets
            BackColor = actualHeaderColor,
            Padding = new Padding(12, 0, 12, 0), // Increased padding
            Tag = "header-panel" // Add tag to identify header panel
        };

        // Enhanced rounded corners and shadow effect
        headerPanel.Paint += (s, e) =>
        {
            using (var path = new GraphicsPath())
            {
                var radius = 8f; // Increased radius for smoother corners
                path.AddArc(0, 0, radius * 2, radius * 2, 180, 90);
                path.AddArc(headerPanel.Width - radius * 2, 0, radius * 2, radius * 2, 270, 90);
                path.AddArc(headerPanel.Width - radius * 2, headerPanel.Height - radius * 2, radius * 2, radius * 2, 0, 90);
                path.AddArc(0, headerPanel.Height - radius * 2, radius * 2, radius * 2, 90, 90);
                path.CloseFigure();

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                // Optional: Add subtle gradient
                using (var brush = new LinearGradientBrush(
                    headerPanel.ClientRectangle,
                    Color.FromArgb(255, actualHeaderColor),
                    Color.FromArgb(225, actualHeaderColor),
                    LinearGradientMode.Vertical))
                {
                    e.Graphics.FillPath(brush, path);
                }

                headerPanel.Region = new Region(path);
            }
        };

        var toggleButton = new Button
        {
            Text = "▼",
            Dock = DockStyle.Left,
            Width = 36, // Slightly wider
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Tag = "collapsible-toggle",
            FlatAppearance = {
                BorderSize = 0,
                MouseOverBackColor = Color.FromArgb(40, Color.White),
                MouseDownBackColor = Color.FromArgb(60, Color.White)
            }
        };

        var titleLabel = new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI Semibold", 11f), // Changed to Semibold
            ForeColor = Color.White,
            Tag = "collapsible-title",
            Padding = new Padding(8, 0, 0, 0)
        };

        var contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            Padding = new Padding(2, 12, 2, 4), // Adjusted padding
            Tag = "collapsible-content"
        };

        toggleButton.Click += (s, e) =>
        {
            toggleButton.Text = contentPanel.Visible ? "▶" : "▼";
            contentPanel.Visible = !contentPanel.Visible;
        };

        headerPanel.Controls.Add(titleLabel);
        headerPanel.Controls.Add(toggleButton);
        panel.Controls.Add(contentPanel);
        panel.Controls.Add(headerPanel);

        return panel;
    }

    /// <summary>
    /// Updates the color scheme of an existing collapsible panel
    /// </summary>
    /// <param name="panel">Target panel to update</param>
    /// <param name="newColor">New color to apply</param>
    public void SetCollapsiblePanelColor(Panel panel, Color newColor)
    {
        if (panel.Tag is CollapsiblePanelData data)
        {
            data.HeaderColor = newColor;
            var headerPanel = panel.Controls.OfType<Panel>()
                                  .FirstOrDefault(p => p.Tag?.ToString() == "header-panel");

            if (headerPanel != null)
            {
                headerPanel.BackColor = newColor;
                headerPanel.Invalidate(); // Force redraw for gradient
            }
        }
    }

    private class CollapsiblePanelData
    {
        public Color HeaderColor { get; set; }
    }
}

// Custom color table for modern look
public class CustomColorTable : ProfessionalColorTable
{
    public Color MenuItemSelectedColor { get; set; } = Color.FromArgb(242, 242, 242);
    public Color MenuItemBorderColor { get; set; } = Color.FromArgb(204, 204, 204);
    public Color MenuBorderColor { get; set; } = Color.FromArgb(204, 204, 204);

    public override Color MenuItemSelected => this.MenuItemSelectedColor;
    public override Color MenuItemBorder => this.MenuItemBorderColor;
    public override Color MenuBorder => this.MenuBorderColor;
    public override Color MenuItemPressedGradientBegin => this.MenuItemSelectedColor;
    public override Color MenuItemPressedGradientEnd => this.MenuItemSelectedColor;
    public override Color MenuItemSelectedGradientBegin => this.MenuItemSelectedColor;
    public override Color MenuItemSelectedGradientEnd => this.MenuItemSelectedColor;
}

/// <summary>
/// Defines modern color schemes for UI components
/// Supports Windows 11 light and dark themes
/// </summary>
public class ThemeColors
{
    /// <summary>
    /// Primary background color for the window
    /// </summary>
    public Color Background { get; set; }

    /// <summary>
    /// Background color for menu elements
    /// </summary>
    public Color MenuBackground { get; set; }

    /// <summary>
    /// Primary text color
    /// </summary>
    public Color Text { get; set; }

    /// <summary>
    /// Hover state color for interactive elements
    /// </summary>
    public Color Hover { get; set; }

    /// <summary>
    /// Border color for UI elements
    /// </summary>
    public Color Border { get; set; }

    public static ThemeColors Dark => new()
    {
        Background = Color.FromArgb(32, 32, 32),
        MenuBackground = Color.FromArgb(45, 45, 45),
        Text = Color.White,
        Hover = Color.FromArgb(64, 64, 64),
        Border = Color.FromArgb(70, 70, 70)
    };

    public static ThemeColors Light => new()
    {
        Background = SystemColors.Control,
        MenuBackground = SystemColors.Menu,
        Text = SystemColors.ControlText,
        Hover = SystemColors.MenuHighlight,
        Border = SystemColors.ControlDark
    };

    public static ThemeColors Windows11Dark => new()
    {
        Background = Color.FromArgb(25, 25, 25),
        MenuBackground = Color.FromArgb(30, 30, 30),
        Text = Color.White,
        Hover = Color.FromArgb(45, 45, 45),
        Border = Color.FromArgb(50, 50, 50)
    };

    public static ThemeColors Windows11Light => new()
    {
        Background = Color.FromArgb(240, 240, 240),
        MenuBackground = Color.FromArgb(255, 255, 255),
        Text = Color.Black,
        Hover = Color.FromArgb(230, 230, 230),
        Border = Color.FromArgb(200, 200, 200)
    };
}

/// <summary>
/// Manages application-wide theme application and updates
/// Handles Windows 11 system theme detection and styling
/// </summary>
public class ThemeManager
{
    private readonly Form _form;
    private readonly MenuStrip? _menuStrip;
    private ThemeColors _colors;

    public ThemeManager(Form form, MenuStrip? menuStrip = null)
    {
        _colors = ThemeColors.Light;
        _form = form;
        _menuStrip = menuStrip;
    }

    /// <summary>
    /// Applies the specified theme to all form controls
    /// </summary>
    /// <param name="isDark">True for dark theme, false for light theme</param>
    public void ApplyTheme(bool isDark)
    {
        _colors = isDark ? ThemeColors.Windows11Dark : ThemeColors.Windows11Light;

        ApplyFormTheme();
        ApplyMenuStripTheme();
        ApplyControlsTheme();
        ApplySpecialControlsTheme();
    }

    private void ApplyFormTheme()
    {
        _form.BackColor = _colors.Background;
        _form.ForeColor = _colors.Text;
    }

    private void ApplyMenuStripTheme()
    {
        if (_menuStrip == null) return;

        _menuStrip.BackColor = _colors.MenuBackground;
        _menuStrip.ForeColor = _colors.Text;
        _menuStrip.Renderer = new ToolStripProfessionalRenderer(new CustomColorTable
        {
            MenuItemSelectedColor = _colors.Hover,
            MenuItemBorderColor = _colors.Border,
            MenuBorderColor = _colors.Border
        });

        foreach (ToolStripMenuItem item in _menuStrip.Items)
        {
            ApplyMenuItemTheme(item);
        }
    }

    private void ApplyMenuItemTheme(ToolStripMenuItem item)
    {
        item.BackColor = _colors.MenuBackground;
        item.ForeColor = _colors.Text;

        foreach (ToolStripItem subItem in item.DropDownItems)
        {
            if (subItem is ToolStripMenuItem menuItem)
            {
                ApplyMenuItemTheme(menuItem);
            }
        }
    }

    private void ApplyControlsTheme()
    {
        ApplyThemeToControl(_form);
    }

    private void ApplyThemeToControl(Control control)
    {
        switch (control)
        {
            case DataGridView grid:
                StyleDataGridView(grid);
                break;
            case Button button when button.Tag?.ToString() == "collapsible-toggle":
                button.ForeColor = _colors.Text;
                button.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, _colors.Text);
                break;
            case Button btn:
                StyleButton(btn);
                break;
            case TextBox txt:
                StyleTextBox(txt);
                break;
            case Label label when label.Tag?.ToString() == "collapsible-title":
                label.ForeColor = _colors.Text;
                break;
            case Label lbl:
                lbl.ForeColor = _colors.Text;
                break;
            case Panel panel when panel.Tag?.ToString() == "collapsible-content":
                panel.BackColor = _colors.Background;
                break;
            case Panel panel:
                panel.BackColor = _colors.Background;
                break;
            case StatusStrip strip:
                strip.BackColor = _colors.MenuBackground;
                strip.ForeColor = _colors.Text;
                break;
            case ProgressBar bar:
                bar.BackColor = _colors.Background;
                bar.ForeColor = _colors.Text;
                break;
            case TrackBar bar:
                bar.BackColor = _colors.Background;
                bar.ForeColor = _colors.Text;
                break;
                // Add more control types as needed
        }

        foreach (Control childControl in control.Controls)
        {
            ApplyThemeToControl(childControl);
        }
    }

    /// <summary>
    /// Applies modern styling to DataGridView controls
    /// Matches the current theme while maintaining readability
    /// </summary>
    private void StyleDataGridView(DataGridView grid)
    {
        grid.EnableHeadersVisualStyles = false;
        grid.BackgroundColor = _colors.Background;
        grid.ForeColor = _colors.Text;
        grid.GridColor = _colors.Border;
        grid.BorderStyle = BorderStyle.None;

        // Column headers styling
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = _colors.MenuBackground,
            ForeColor = _colors.Text,
            SelectionBackColor = _colors.Hover,
            Font = new Font(grid.Font.FontFamily, grid.Font.Size, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(8, 4, 8, 4)
        };

        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;

        // Create base style
        var baseStyle = new DataGridViewCellStyle
        {
            BackColor = _colors.Background,
            ForeColor = _colors.Text,
            SelectionBackColor = _colors.Hover,
            SelectionForeColor = _colors.Text,
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(8, 4, 8, 4)
        };

        // Apply base style
        grid.DefaultCellStyle = baseStyle;

        // Create and apply alternate style
        var alternateStyle = baseStyle.Clone() as DataGridViewCellStyle;
        if (alternateStyle != null)
        {
            alternateStyle.BackColor = Color.FromArgb(
                255,
                _colors.Background.R + 5,
                _colors.Background.G + 5,
                _colors.Background.B + 5
            );
            grid.AlternatingRowsDefaultCellStyle = alternateStyle;
        }

        // Force a visual refresh
        grid.Update();
        grid.Refresh();
    }

    /// <summary>
    /// Applies appropriate button styling based on theme
    /// Handles both standard and special-case buttons
    /// </summary>
    private void StyleButton(Button button)
    {
        if (_colors == ThemeColors.Dark)
        {
            button.BackColor = _colors.MenuBackground;
            button.ForeColor = _colors.Text;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = _colors.Border;
        }
        else
        {
            button.UseVisualStyleBackColor = true;
            button.FlatStyle = FlatStyle.System;
        }
    }

    /// <summary>
    /// Updates text box appearance for the current theme
    /// Maintains Windows native styling in light mode
    /// </summary>
    private void StyleTextBox(TextBox textBox)
    {
        if (_colors == ThemeColors.Dark)
        {
            textBox.BackColor = _colors.MenuBackground;
            textBox.ForeColor = _colors.Text;
        }
        else
        {
            textBox.BackColor = SystemColors.Window;
            textBox.ForeColor = SystemColors.WindowText;
        }
    }

    private void ApplySpecialControlsTheme()
    {
        ApplyToControl(_form.Controls.OfType<StatusStrip>().FirstOrDefault(), strip =>
        {
            strip.BackColor = _colors.MenuBackground;
            strip.ForeColor = _colors.Text;
        });

        ApplyToControl(_form.Controls.OfType<ProgressBar>().FirstOrDefault(), bar =>
        {
            bar.BackColor = _colors.Background;
            bar.ForeColor = _colors.Text;
        });

        ApplyToControl(_form.Controls.OfType<TrackBar>().FirstOrDefault(), bar =>
        {
            bar.BackColor = _colors.Background;
            bar.ForeColor = _colors.Text;
        });

        var toolTip = _form.Controls.OfType<ToolTip>().FirstOrDefault();
        if (toolTip != null)
        {
            toolTip.BackColor = _colors.MenuBackground;
            toolTip.ForeColor = _colors.Text;
        }
    }

    private void ApplyToControl<T>(T? control, Action<T> styling) where T : Control
    {
        if (control != null)
        {
            styling(control);
        }
    }
}
