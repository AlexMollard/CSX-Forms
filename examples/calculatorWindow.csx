#load "../Window.csx"

using System;
using System.Windows.Forms;
using System.Drawing;
using System.Data;

WindowManager windowManager = new WindowManager();

Form form = new Form
{
    Text = "Calculator",
    Size = new Size(400, 600),
    Font = new Font("Segoe UI", 16F),
    Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),
    StartPosition = FormStartPosition.CenterScreen
};

TextBox display = new TextBox
{
    ReadOnly = true,
    TextAlign = HorizontalAlignment.Right,
    Dock = DockStyle.Top,
    Font = new Font("Segoe UI", 24F),
    Height = 50,
    Margin = new Padding(10)
};

// Create buttons
Button[] buttons = new Button[]
{
    CreateButton("7", 10,  90),
    CreateButton("8", 100, 90),
    CreateButton("9", 190, 90),
    CreateButton("/", 280, 90),
    CreateButton("4", 10,  180),
    CreateButton("5", 100, 180),
    CreateButton("6", 190, 180),
    CreateButton("*", 280, 180),
    CreateButton("1", 10,  270),
    CreateButton("2", 100, 270),
    CreateButton("3", 190, 270),
    CreateButton("-", 280, 270),
    CreateButton("0", 10,  360),
    CreateButton(".", 100, 360),
    CreateButton("+", 190, 360),
    CreateButton("=", 280, 360),
    CreateButton("C", 10,  450)
};

foreach (var button in buttons)
{
    form.Controls.Add(button);
}

Button CreateButton(string text, int x, int y, int width = 80, int height = 80)
{
    Button button = new Button
    {
        Text = text,
        Font = new Font("Segoe UI", 18F),
        Size = new Size(width, height),
        Location = new Point(x, y),
    };
    button.Click += (s, e) => OnButtonClick(text);
    return button;
}

void OnButtonClick(string text)
{
    if (text == "=" || text == "Enter")
    {
        try
        {
            display.Text = new DataTable().Compute(display.Text, null).ToString();
        }
        catch
        {
            display.Text = "Error";
        }
    }
    else if (text == "C")
    {
        display.Clear();
    }
    else
    {
        display.Text += text;
    }
}

form.Controls.Add(display);


// Add all the number keys to the form
foreach (var button in buttons)
{
    // Only add key handlers for number keys and operators
    if (char.IsDigit(button.Text[0]) || "+-*/.".Contains(button.Text))
    {
        Keys[] keys;
        if (char.IsDigit(button.Text[0]))
        {
            // Convert both regular number keys and numpad keys
            int num = int.Parse(button.Text);
            keys = new[] {
                (Keys)((int)Keys.D0 + num),    // Regular number keys
                (Keys)((int)Keys.NumPad0 + num) // Numpad keys
            };
        }
        else
        {
            // Convert operator keys
            keys = new[] {
                button.Text switch
                {
                    "+" => Keys.Add,
                    "-" => Keys.Subtract,
                    "*" => Keys.Multiply,
                    "/" => Keys.Divide,
                    "." => Keys.Decimal,
                    _ => Keys.None
                }
            };
        }

        foreach (var key in keys)
        {
            if (key != Keys.None)
            {
                windowManager.AddKeyEventHandler(form, key, (s, e) => OnButtonClick(button.Text));
            }
        }
    }
}

// Add the Enter key to the form
windowManager.AddKeyEventHandler(form, Keys.Enter, (s, e) => OnButtonClick("Enter"));

windowManager.CreateThemeButton(form);
windowManager.CreateForm(form);
