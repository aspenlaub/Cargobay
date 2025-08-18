using System.Collections.Generic;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Access;

public class PasswordProvider : IPasswordProvider {
    protected static Dictionary<string, Login> SecurityCodes;

    public PasswordProvider() {
        SecurityCodes = new Dictionary<string, Login>();
    }

    public Login AddAccessCodes(string clue, string identification, string password) {
        if (SecurityCodes.TryGetValue(clue, out var codes)) { return codes; }

        var login = new Login() { Identification = identification, Password = password };
        SecurityCodes.Add(clue, login);
        return login;
    }

    public Login GetAccessCodes(string clue, string userId) {
        return GetAccessCodes(clue, userId, null);
    }

    public Login GetAccessCodes(string clue, string userId, IAccessCodePrompt accessCodePrompt) {
        var silent = accessCodePrompt == null;
        if (SecurityCodes.TryGetValue(clue, out var codes)) { return codes; }
        if (silent) { return null; }

        accessCodePrompt.Clue = clue;
        accessCodePrompt.Identification = userId;
        accessCodePrompt.SetFocusOnAppropriateField();
        accessCodePrompt.ShowDialog();
        return !accessCodePrompt.GoodCode ? null : AddAccessCodes(clue, accessCodePrompt.Identification, accessCodePrompt.Password);
    }
}