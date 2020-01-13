using System;
using System.Collections.Generic;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Jobz;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Application;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Application {
    public class CargobayApplication : IJobPreviewingApplication, IJobRunningApplication {
        public CargoJobs Jobs { get; }
        protected IApplicationCommandController Controller;
        protected IApplicationCommandExecutionContext Context;

        public CargobayApplication(IApplicationCommandController controller, IApplicationCommandExecutionContext context, IJobSelector jobSelector, ICrypticKeyProvider crypticKeyProvider,
                IPasswordProvider passwordProvider, IJobFolderAdjuster jobFolderAdjuster, ISecretRepository secretRepository) {
            var secret = new CargoJobsSecret();
            var errorsAndInfos = new ErrorsAndInfos();
            Jobs = secretRepository.GetAsync(secret, errorsAndInfos).Result;
            if (errorsAndInfos.AnyErrors()) {
                throw new Exception(errorsAndInfos.ErrorsToString());
            }

            Jobs.ForEach(job => jobFolderAdjuster.AdjustJobAndSubFolders(job, errorsAndInfos));
            if (errorsAndInfos.AnyErrors()) {
                throw new Exception(errorsAndInfos.ErrorsToString());
            }

            Context = context;
            Controller = controller;
            Controller.AddCommand(new PreviewCommand(this, jobSelector, passwordProvider), true);
            Controller.AddCommand(new ExecuteCommand(this, jobSelector, crypticKeyProvider, passwordProvider), true);
        }

        protected void Preview(Job job, IJobRunner runner, ISubJobRunner subRunner, ISubJobDetailRunner detailRunner, Dictionary<string, Login> accessCodes) {
            if (job == null) {
                Context.Report(new FeedbackToApplication() { Type = FeedbackType.LogError, Message = Properties.Resources.NoJobSelected });
                return;
            }

            runner.Preview(job, false, Context, subRunner, detailRunner, accessCodes);
        }

        public void Preview(Job job, Dictionary<string, Login> accessCodes) {
            Preview(job, new JobRunner(), new SubJobRunner(), new SubJobDetailRunner(), accessCodes);
        }

        protected void Run(Job job, IJobRunner runner, ISubJobRunner subRunner, ISubJobDetailRunner detailRunner, CrypticKey crypticKey, Dictionary<string, Login> accessCodes) {
            if (job == null) {
                Context.Report(new FeedbackToApplication() { Type = FeedbackType.LogError, Message = Properties.Resources.NoJobSelected });
                return;
            }

            runner.Preview(job, true, Context, subRunner, detailRunner, accessCodes);
            if (runner.IsWrongMachine(job)) { return; }

            runner.Run(job, DateTime.Today, Context, subRunner, detailRunner, crypticKey, accessCodes);
        }

        public void Run(Job job, CrypticKey crypticKey, Dictionary<string, Login> accessCodes) {
            Run(job, new JobRunner(), new SubJobRunner(), new SubJobDetailRunner(), crypticKey, accessCodes);
        }

    }
}