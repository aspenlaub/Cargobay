using System.Windows;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Access;

public partial class AccessCodePrompt : IAccessCodePrompt {
    public string Clue {
        get => ClueLabel.Content.ToString();
        set => ClueLabel.Content = value;
    }

    public bool GoodCode { get; private set; }

    public string Identification => IdentificationTextBox.Text;
    public string Password => PasswordTextBox.Password;

    public AccessCodePrompt() {
        InitializeComponent();
        GoodCode = false;
        IdentificationTextBox.Focus();
    }

    private void Click(object sender, RoutedEventArgs e) {
        GoodCode = IdentificationTextBox.Text.Length > 5 && PasswordTextBox.Password.Length > 5;
        Close();
    }
}