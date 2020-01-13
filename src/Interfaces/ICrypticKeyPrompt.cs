namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces {
    public interface ICrypticKeyPrompt {
        string Clue { get; set; }
        string Key { get; }

        bool? ShowDialog();
    }
}
