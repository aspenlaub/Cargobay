using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Components;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Test.Jobz;

[TestClass]
public class JobRunnerTest {
    private IJobRunner _JobRunner;
    private ISubJobRunner _SubJobRunner;
    private ISubJobDetailRunner _SubJobDetailRunner;

    [TestInitialize]
    public void Initialize() {
        var container = new ContainerBuilder().UseCargobay().Build();
        _JobRunner = container.Resolve<IJobRunner>();
        _SubJobRunner = container.Resolve<ISubJobRunner>();
        _SubJobDetailRunner = container.Resolve<ISubJobDetailRunner>();
    }

    [TestMethod]
    public async Task CanRunCleanUpTaskWithUrl() {
        var job = new Job {
            JobType = CargoJobType.CleanUp,
            Url = "http://localhost/kunden/vvsbigband.de/viperfisch.de/webseiten/viperfisch/nothing/index.php"
        };
        var context = new FakeCommandExecutionContext();
        var result = await _JobRunner.RunAsync(job, DateTime.Today, context,
            _SubJobRunner, _SubJobDetailRunner, new CrypticKey(), new Dictionary<string, Login>());
        Assert.IsTrue(result);
        Assert.HasCount(3, context.FeedbackToApplication);
        VerifyFeedbackToApplication(context.FeedbackToApplication[0],
            FeedbackType.LogInformation, "");
        VerifyFeedbackToApplication(context.FeedbackToApplication[1],
            FeedbackType.LogInformation, job.Url);
        VerifyFeedbackToApplication(context.FeedbackToApplication[2],
            FeedbackType.LogInformation, "This page does nothing");
    }

    [TestMethod]
    public async Task CleanUpTaskWithInvalidUrlFails() {
        var job = new Job {
            JobType = CargoJobType.CleanUp,
            Url = "http://localhost/kunden/vvsdigdand.de/webseiten/viperfisch/nothing/index.php"
        };
        var context = new FakeCommandExecutionContext();
        var result = await _JobRunner.RunAsync(job, DateTime.Today, context,
            _SubJobRunner, _SubJobDetailRunner, new CrypticKey(), new Dictionary<string, Login>());
        Assert.IsFalse(result);
        Assert.HasCount(3, context.FeedbackToApplication);
        VerifyFeedbackToApplication(context.FeedbackToApplication[0],
            FeedbackType.LogInformation, "");
        VerifyFeedbackToApplication(context.FeedbackToApplication[1],
            FeedbackType.LogInformation, job.Url);
        VerifyFeedbackToApplication(context.FeedbackToApplication[2],
            FeedbackType.LogError, "404 Not Found");
    }

    private static void VerifyFeedbackToApplication(IFeedbackToApplication feedbackToApplication,
            FeedbackType expectedType, string message) {
        Assert.AreEqual(expectedType, feedbackToApplication.Type);
        Assert.AreEqual(message, feedbackToApplication.Message.Trim());
    }
}