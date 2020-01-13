using Aspenlaub.Net.GitHub.CSharp.Cargobay.Access;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Test {
    public class FakeCrypticKeyProvider : ICrypticKeyProvider {
        protected const string Key = "ThisIsNotAKey";

        public CrypticKey GetCrypticKey(string clue, string sha1) {
            return new CrypticKey { Key = Key, Sha1 = new Sha1Encrypter().Encrypt(Key) };
        }

        public CrypticKey GetCrypticKey(string clue, string sha1, ICrypticKeyPrompt crypticKeyPrompt) {
            return GetCrypticKey(clue, sha1);
        }
    }
}
