using System.Collections.Generic;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Access;

public class CrypticKeyProvider : ICrypticKeyProvider {
    protected static Dictionary<string, CrypticKey> CrypticKeys;

    public CrypticKeyProvider() {
        CrypticKeys = new Dictionary<string, CrypticKey>();
    }

    public CrypticKey GetCrypticKey(string clue, string sha1) {
        return GetCrypticKey(clue, sha1, null);
    }

    public CrypticKey GetCrypticKey(string clue, string sha1, ICrypticKeyPrompt crypticKeyPrompt) {
        var silent = crypticKeyPrompt == null;
        if (CrypticKeys.TryGetValue(clue, out var key)) { return key; }
        if (silent) { return null; }

        crypticKeyPrompt.Clue = clue;
        crypticKeyPrompt.ShowDialog();

        var crypticKey = new CrypticKey { Key = crypticKeyPrompt.Key, Sha1 = sha1 };
        if (!crypticKey.GoodCode) { return null; }

        CrypticKeys.Add(clue, crypticKey);
        return crypticKey;
    }
}