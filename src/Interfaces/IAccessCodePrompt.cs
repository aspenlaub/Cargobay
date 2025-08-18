namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces;

public interface IAccessCodePrompt {
    string Clue { get; set; }
    bool GoodCode { get; }
    string Identification { get; set; }
    string Password { get; }

    void SetFocusOnAppropriateField();
    bool? ShowDialog();
}