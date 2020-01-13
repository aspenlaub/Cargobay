using System;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Test {
    public class FakeCommandExecutionContext : IApplicationCommandExecutionContext {
        public void Report(IFeedbackToApplication feedback) { }
        public void Report(string message, bool ofNoImportance) { }
        public void ReportExecutionResult(Type commandType, bool success, string errorMessage) { }
    }
}
