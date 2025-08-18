using System.Windows;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Access;

public partial class AccessCodePrompt : IAccessCodePrompt {
    public string Clue {
        get { return ClueLabel.Content.ToString(); }
        set { ClueLabel.Content = value; }
    }

    public bool GoodCode { get; private set; }

    public string Identification {
        get { return IdentificationTextBox.Text; }
        set { IdentificationTextBox.Text = value; }
    }

    public string Password => PasswordTextBox.Password;

    public AccessCodePrompt() {
        InitializeComponent();
        GoodCode = false;
    }

    public void SetFocusOnAppropriateField() {
        if (IdentificationTextBox.Text != "") {
            PasswordTextBox.Focus();
        } else {
            IdentificationTextBox.Focus();
        }
    }

    private void Click(object sender, RoutedEventArgs e) {
        GoodCode = IdentificationTextBox.Text.Length > 5 && PasswordTextBox.Password.Length > 5;
        Close();
    }
}