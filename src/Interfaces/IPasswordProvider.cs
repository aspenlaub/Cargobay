using Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces {
    public interface IPasswordProvider {
        Login GetAccessCodes(string clue, IAccessCodePrompt accessCodePrompt);
        Login GetAccessCodes(string clue);
    }
}
