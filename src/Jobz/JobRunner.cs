using System;
using System.Collections.Generic;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Application;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Jobz {
    public class JobRunner : IJobRunner {
        protected bool IsPrimaryMachine(Job job) {
            return Environment.MachineName.ToUpper() == job.Machine.ToUpper();
        }

        protected bool IsSecondaryMachine(Job job) {
            return Environment.MachineName.ToUpper() == job.SecondaryMachine.ToUpper() || job.Machine == "";
        }

        public bool IsWrongMachine(Job job) {
            return !(IsPrimaryMachine(job) || IsSecondaryMachine(job));
        }

        public bool IsRightMachine(Job job) {
            return IsPrimaryMachine(job) || IsSecondaryMachine(job);
        }

        private void ExecutionLogEntry(IApplicationCommandExecutionContext context, string caption, string value) {
            context.Report(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = (caption + "            ").Substring(0, 12) + " : " + value });
        }

        public void Preview(Job job, bool forExecutionLog, IApplicationCommandExecutionContext context, ISubJobRunner runner, ISubJobDetailRunner detailRunner, Dictionary<string, Login> accessCodes) {
            context.Report(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = "Job" });
            if (job.Description.Length != 0) {
                if (forExecutionLog) {
                    ExecutionLogEntry(context, Properties.Resources.Executed, job.Description);
                } else {
                    context.Report(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = job.Description });
                }
            }
            if (job.Machine.Length != 0) {
                ExecutionLogEntry(context, Properties.Resources.Machine, job.Machine);
            }
            if (job.SecondaryMachine.Length != 0) {
                ExecutionLogEntry(context, Properties.Resources.SecondaryMachine, job.SecondaryMachine);
            }
            if (IsWrongMachine(job)) {
                return;
            }

            if (job.AdjustedFolder.Length != 0) {
                ExecutionLogEntry(context, Properties.Resources.Folder, job.AdjustedFolder);
            }
            if (job.AdjustedDestinationFolder.Length != 0) {
                ExecutionLogEntry(context, Properties.Resources.Destination, job.AdjustedDestinationFolder);
            }
            if (forExecutionLog) {
                return;
            }

            if (job.Name.Length != 0 && job.Description.IndexOf(job.Name, StringComparison.Ordinal) < 0) {
                ExecutionLogEntry(context, Properties.Resources.Name, job.Name);
            }
            ExecutionLogEntry(context, Properties.Resources.Type, Enum.GetName(typeof(CargoJobType), job.JobType));
            foreach (var subJob in job.SubJobs) {
                runner.Preview(subJob, job, false, context, detailRunner, accessCodes);
            }
        }

        public bool Run(Job job, DateTime today, IApplicationCommandExecutionContext context, ISubJobRunner runner, ISubJobDetailRunner detailRunner, CrypticKey crypticKey, Dictionary<string, Login> accessCodes) {
            foreach (var nextSubJob in job.SubJobs) {
                runner.Preview(nextSubJob, job, true, context, detailRunner, accessCodes);
                if (!runner.Run(nextSubJob, today, job, context, detailRunner, crypticKey, accessCodes)) {
                    return false;
                }
            }

            return true;
        }
    }
}
