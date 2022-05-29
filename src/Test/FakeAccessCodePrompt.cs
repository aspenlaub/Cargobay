using Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Test;

public class FakeAccessCodePrompt : FakePromptBase, IAccessCodePrompt {
    public string Identification { get; set; }
    public string Password { get; set; }

}