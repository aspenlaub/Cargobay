using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Jobz;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Application;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Application;

public class CargobayApplication : IJobPreviewingApplication, IJobRunningApplication, IJobsRefreshingApplication {
    public CargoJobs Jobs { get; }
    protected IApplicationCommandController Controller;
    protected IApplicationCommandExecutionContext Context;

    private readonly ISecretRepository SecretRepository;
    private readonly IJobFolderAdjuster JobFolderAdjuster;

    public CargobayApplication(IApplicationCommandController controller, IApplicationCommandExecutionContext context, IJobSelector jobSelector, ICrypticKeyProvider crypticKeyProvider,
        IPasswordProvider passwordProvider, IJobFolderAdjuster jobFolderAdjuster, ISecretRepository secretRepository) {
        SecretRepository = secretRepository;
        JobFolderAdjuster = jobFolderAdjuster;

        Jobs = new CargoJobs();

        Context = context;
        Controller = controller;
        Controller.AddCommand(new PreviewCommand(this, jobSelector, passwordProvider), true);
        Controller.AddCommand(new ExecuteCommand(this, jobSelector, crypticKeyProvider, passwordProvider), true);
        Controller.AddCommand(new RefreshJobsCommand(this), true);
    }

    protected async Task PreviewAsync(Job job, IJobRunner runner, ISubJobRunner subRunner, ISubJobDetailRunner detailRunner, Dictionary<string, Login> accessCodes) {
        if (job == null) {
            await Context.ReportAsync(new FeedbackToApplication() { Type = FeedbackType.LogError, Message = Properties.Resources.NoJobSelected });
            return;
        }

        await runner.PreviewAsync(job, false, Context, subRunner, detailRunner, accessCodes);
    }

    public async Task PreviewAsync(Job job, Dictionary<string, Login> accessCodes) {
        await PreviewAsync(job, new JobRunner(), new SubJobRunner(), new SubJobDetailRunner(), accessCodes);
    }

    protected async Task RunAsync(Job job, IJobRunner runner, ISubJobRunner subRunner, ISubJobDetailRunner detailRunner, CrypticKey crypticKey, Dictionary<string, Login> accessCodes) {
        if (job == null) {
            await Context.ReportAsync(new FeedbackToApplication() { Type = FeedbackType.LogError, Message = Properties.Resources.NoJobSelected });
            return;
        }

        await runner.PreviewAsync(job, true, Context, subRunner, detailRunner, accessCodes);
        if (runner.IsWrongMachine(job)) { return; }

        await runner.RunAsync(job, DateTime.Today, Context, subRunner, detailRunner, crypticKey, accessCodes);
    }

    public async Task RunAsync(Job job, CrypticKey crypticKey, Dictionary<string, Login> accessCodes) {
        await RunAsync(job, new JobRunner(), new SubJobRunner(), new SubJobDetailRunner(), crypticKey, accessCodes);
    }

    public async Task RefreshJobsAsync() {
        Jobs.Clear();

        var secret = new CargoJobsSecret();
        var errorsAndInfos = new ErrorsAndInfos();
        var secretJobs = (await SecretRepository.GetAsync(secret, errorsAndInfos)).OrderBy(j => j.SortValue());
        Jobs.AddRange(secretJobs);
        if (errorsAndInfos.AnyErrors()) {
            throw new Exception(errorsAndInfos.ErrorsToString());
        }

        foreach (var job in Jobs) {
            await JobFolderAdjuster.AdjustJobAndSubFoldersAsync(job, errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) {
                throw new Exception(errorsAndInfos.ErrorsToString());
            }
        }
    }
}