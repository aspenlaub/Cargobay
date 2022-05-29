using Aspenlaub.Net.GitHub.CSharp.Cargobay.Access;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Test;

[TestClass]
public class PasswordProviderTest {
    protected const string Clue = "Viperfisch.de webmaster access";

    [TestMethod]
    public void OnlyPromptsForCredentialsOnce() {
        var accessPrompt = new FakeAccessCodePrompt { Identification = "someIdentification", Password = "somePassword", DialogWasShown = false, DialogResult = true, GoodCode = true };
        var passwordProvider = new PasswordProvider();
        var login = passwordProvider.GetAccessCodes(Clue, accessPrompt);
        Assert.IsTrue(accessPrompt.DialogWasShown);
        accessPrompt.DialogWasShown = false;
        var checkLogin = passwordProvider.GetAccessCodes(Clue, accessPrompt);
        Assert.IsNotNull(checkLogin);
        Assert.IsFalse(accessPrompt.DialogWasShown);
        Assert.AreEqual(login.Identification, checkLogin.Identification);
        Assert.AreEqual(login.Password, checkLogin.Password);
        checkLogin = passwordProvider.GetAccessCodes(Clue, null);
        Assert.IsNotNull(checkLogin);
        Assert.IsFalse(accessPrompt.DialogWasShown);
        Assert.AreEqual(login.Identification, checkLogin.Identification);
        Assert.AreEqual(login.Password, checkLogin.Password);
    }
}