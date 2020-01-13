namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Test {
    public class FakePromptBase {
        public string Clue { get; set; }
        public bool GoodCode { get; set; }
        public bool DialogWasShown { get; set; }
        public bool? DialogResult { get; set; }

        public FakePromptBase() {
            DialogWasShown = false;
        }

        public bool? ShowDialog() {
            DialogWasShown = true;
            return DialogResult;
        }
    }
}
