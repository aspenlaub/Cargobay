using System.Collections.Generic;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Access {
    public class PasswordProvider : IPasswordProvider {
        protected static Dictionary<string, Login> SecurityCodes;

        public PasswordProvider() {
            SecurityCodes = new Dictionary<string, Login>();
        }

        public Login AddAccessCodes(string clue, string identification, string password) {
            if (SecurityCodes.ContainsKey(clue)) { return SecurityCodes[clue]; }

            var login = new Login() { Identification = identification, Password = password };
            SecurityCodes.Add(clue, login);
            return login;
        }

        public Login GetAccessCodes(string clue) {
            return GetAccessCodes(clue, null);
        }

        public Login GetAccessCodes(string clue, IAccessCodePrompt accessCodePrompt) {
            var silent = accessCodePrompt == null;
            if (SecurityCodes.ContainsKey(clue)) { return SecurityCodes[clue]; }
            if (silent) { return null; }

            accessCodePrompt.Clue = clue;
            accessCodePrompt.ShowDialog();
            return !accessCodePrompt.GoodCode ? null : AddAccessCodes(clue, accessCodePrompt.Identification, accessCodePrompt.Password);
        }
    }
}
