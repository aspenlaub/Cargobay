using Aspenlaub.Net.GitHub.CSharp.Cargobay.Access;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Test {
    [TestClass]
    public class CrypticKeyProviderTest {
        protected const string Clue = "Viperfisch.de encryption centre";

        [TestMethod]
        public void OnlyPromptsForKeyOnce() {
            var crypticKeyPrompt = new FakeCrypticKeyPrompt { Key = "someKey", DialogWasShown = false, DialogResult = true, GoodCode = true };
            var sha1 = new Sha1Encrypter().Encrypt(crypticKeyPrompt.Key);
            var crypticKeyProvider = new CrypticKeyProvider();
            var key = crypticKeyProvider.GetCrypticKey(Clue, sha1, crypticKeyPrompt);
            Assert.IsTrue(crypticKeyPrompt.DialogWasShown);
            Assert.IsTrue(key.GoodCode);
            crypticKeyPrompt.DialogWasShown = false;
            var checkKey = crypticKeyProvider.GetCrypticKey(Clue, sha1, crypticKeyPrompt);
            Assert.IsNotNull(checkKey);
            Assert.IsFalse(crypticKeyPrompt.DialogWasShown);
            Assert.IsTrue(key.GoodCode);
            Assert.AreEqual(key.Key, checkKey.Key);
            checkKey = crypticKeyProvider.GetCrypticKey(Clue, sha1, null);
            Assert.IsNotNull(checkKey);
            Assert.IsFalse(crypticKeyPrompt.DialogWasShown);
            Assert.IsTrue(key.GoodCode);
            Assert.AreEqual(key.Key, checkKey.Key);
        }

        [TestMethod]
        public void Sha1MismatchIsNotGood() {
            var crypticKeyPrompt = new FakeCrypticKeyPrompt { Key = "someKey", DialogWasShown = false, DialogResult = true, GoodCode = true };
            var sha1 = new Sha1Encrypter().Encrypt(crypticKeyPrompt.Key);
            var crypticKeyProvider = new CrypticKeyProvider();
            var key = crypticKeyProvider.GetCrypticKey(Clue, ' ' + sha1, crypticKeyPrompt);
            Assert.IsNull(key);
        }
    }
}
