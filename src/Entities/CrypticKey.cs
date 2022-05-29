using Aspenlaub.Net.GitHub.CSharp.Cargobay.Access;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;

public class CrypticKey {
    public string Sha1 { get; set; }
    public string Key { get; set; }
    public bool GoodCode => Sha1.Length != 0 && Sha1 == new Sha1Encrypter().Encrypt(Key);
}