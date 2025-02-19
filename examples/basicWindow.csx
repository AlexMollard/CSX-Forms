#load "../Window.csx"

using System;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Win32;

WindowManager windowManager = new WindowManager();

void AdjustLayout(Form form, Button button)
{
    int centerX = form.ClientSize.Width / 2;
    int centerY = form.ClientSize.Height / 2;

    button.Location = new Point(centerX - button.Width / 2, centerY - 150);
}

MenuStrip CreateMenuStrip()
{
    MenuStrip menuStrip = new MenuStrip();
    ToolStripMenuItem fileMenu = new ToolStripMenuItem("File");
    ToolStripMenuItem exitMenuItem = new ToolStripMenuItem("Exit", null, (s, e) => Application.Exit());
    fileMenu.DropDownItems.Add(exitMenuItem);
    menuStrip.Items.Add(fileMenu);

    return menuStrip;
}

Button CreateButton()
{
    int count = 0;
    Button button = new Button
    {
        Text = "Click me!",
        Size = new Size(200, 50),
        Anchor = AnchorStyles.None,
        AutoSize = true
    };

    button.Click += (s, e) =>
    {
        count++;
        button.Text = $"Clicked {count} times!";
        AdjustLayout(button.FindForm(), button);
    };

    return button;
}

StatusStrip CreateStatusStrip()
{
    StatusStrip statusStrip = new StatusStrip();
    ToolStripStatusLabel toolStripStatusLabel = new ToolStripStatusLabel("Ready");
    statusStrip.Items.Add(toolStripStatusLabel);
    return statusStrip;
}

NotifyIcon CreateNotifyIcon(Form form)
{
    NotifyIcon notifyIcon = new NotifyIcon
    {
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),
        Text = "WinFormsApp",
        Visible = true
    };

    notifyIcon.DoubleClick += (s, e) => form.Show();
    form.Resize += (s, e) =>
    {
        if (form.WindowState == FormWindowState.Minimized)
        {
            form.Hide();
        }
    };

    return notifyIcon;
}

ToolTip CreateToolTip(Control control)
{
    ToolTip toolTip = new ToolTip();
    toolTip.SetToolTip(control, "Click me!");
    return toolTip;
}

Timer CreateTimer(StatusStrip statusStrip)
{
    Timer timer = new Timer { Interval = 1000 };
    timer.Tick += (s, e) =>
    {
        statusStrip.Items[0].Text = DateTime.Now.ToString();
    };
    return timer;
}

Form form = new Form
{
    Text = "CSX WinForms App",
    Size = new Size(800, 600),
    Font = new Font("Segoe UI", 16F),
    Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)
};

MenuStrip menuStrip = CreateMenuStrip();
form.Controls.Add(menuStrip);

Button button = CreateButton();
form.Controls.Add(button);

StatusStrip statusStrip = CreateStatusStrip();
form.Controls.Add(statusStrip);

NotifyIcon notifyIcon = CreateNotifyIcon(form);
ToolTip toolTip = CreateToolTip(button);

Timer timer = CreateTimer(statusStrip);
timer.Start();

form.Resize += (s, e) => AdjustLayout(form, button);

// Force a layout adjustment to center the button
AdjustLayout(form, form.Controls[1] as Button);

// Add a toggle theme button to the form
windowManager.CreateThemeButton(form, form.Controls[0] as MenuStrip);
windowManager.AddKeyEventHandler(form, Keys.Escape, (s, e) => Application.Exit()); // Exit app on Escape key press
windowManager.CreateForm(form, form.Controls[0] as MenuStrip);