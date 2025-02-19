#r "System.Windows.Forms.dll"
#r "System.Drawing.dll"
#r "nuget: Microsoft.Windows.Compatibility, 3.1.0"
#r "nuget: Accessibility, 4.6.0-preview3-27504-2"

using System;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Win32;
using System.Drawing.Drawing2D;

// nullable enable
#nullable enable

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

public class ThemeColors
{
    public Color Background { get; set; }
    public Color MenuBackground { get; set; }
    public Color Text { get; set; }
    public Color Hover { get; set; }
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
            case Button btn:
                StyleButton(btn);
                break;
            case TextBox txt:
                StyleTextBox(txt);
                break;
            case Label lbl:
                lbl.ForeColor = _colors.Text;
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
