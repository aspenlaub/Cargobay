using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Access {
    public class Sha1Encrypter {
        public string Encrypt(string s) {
            var data = Encoding.ASCII.GetBytes(s);
            var sHhash = new SHA1Managed();
            var hashValue = sHhash.ComputeHash(data);
            return hashValue.Aggregate("", (current, b) => current + $"{b:x2}");
        }
    }
}
