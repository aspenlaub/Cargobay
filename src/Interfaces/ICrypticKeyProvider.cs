using Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces;

public interface ICrypticKeyProvider {
    CrypticKey GetCrypticKey(string clue, string sha1, ICrypticKeyPrompt crypticKeyPrompt);
    CrypticKey GetCrypticKey(string clue, string sha1);
}