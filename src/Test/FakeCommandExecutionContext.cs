using System;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Test {
    public class FakeCommandExecutionContext : IApplicationCommandExecutionContext {
        public async Task ReportAsync(IFeedbackToApplication feedback) { await Task.CompletedTask; }
        public async Task ReportAsync(string message, bool ofNoImportance) { await Task.CompletedTask; }
        public async Task ReportExecutionResultAsync(Type commandType, bool success, string errorMessage) { await Task.CompletedTask;  }
    }
}
