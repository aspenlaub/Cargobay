using Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Test;

public class FakeCrypticKeyPrompt : FakePromptBase, ICrypticKeyPrompt {
    public string Key { get; set; }
}