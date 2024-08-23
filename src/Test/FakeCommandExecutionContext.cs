using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Test;

public class FakeCommandExecutionContext : IApplicationCommandExecutionContext {
    public List<IFeedbackToApplication> FeedbackToApplication { get; } = new();

    public async Task ReportAsync(IFeedbackToApplication feedback) {
        FeedbackToApplication.Add(feedback);
        await Task.CompletedTask;
    }
    public async Task ReportAsync(string message, bool ofNoImportance) { await Task.CompletedTask; }
    public async Task ReportExecutionResultAsync(Type commandType, bool success, string errorMessage) { await Task.CompletedTask;  }
}