namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces;

public interface IAccessCodePrompt {
    string Clue { get; set; }
    bool GoodCode { get; }
    string Identification { get; }
    string Password { get; }

    bool? ShowDialog();
}