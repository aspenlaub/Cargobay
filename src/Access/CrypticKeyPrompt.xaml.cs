using System.Windows;
using System.Windows.Controls;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Access;

public partial class CrypticKeyPrompt : ICrypticKeyPrompt {
    public string Clue {
        get { return ClueLabel.Content.ToString(); }
        set { ClueLabel.Content = value; }
    }

    public string Key => KeyTextBox.Password;

    public CrypticKeyPrompt() {
        InitializeComponent();
        KeyTextBox.Focus();
    }

    private void Click(object sender, RoutedEventArgs e) {
        Close();
    }

    private void KeySampleTextBox_TextChanged(object sender, TextChangedEventArgs e) {
        HashTextBox.Text = new Sha1Encrypter().Encrypt(KeySampleTextBox.Text);
    }
}