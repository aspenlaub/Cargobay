using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Application;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Jobz;

public class JobRunner : IJobRunner {
    protected static bool IsPrimaryMachine(Job job) {
        return Environment.MachineName.ToUpper() == job.Machine.ToUpper();
    }

    protected static bool IsSecondaryMachine(Job job) {
        return Environment.MachineName.ToUpper() == job.SecondaryMachine.ToUpper() || job.Machine == "";
    }

    public bool IsWrongMachine(Job job) {
        return !(IsPrimaryMachine(job) || IsSecondaryMachine(job));
    }

    public bool IsRightMachine(Job job) {
        return IsPrimaryMachine(job) || IsSecondaryMachine(job);
    }

    private static async Task ExecutionLogEntryAsync(IApplicationCommandExecutionContext context, string caption, string value) {
        await context.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = (caption + "            ").Substring(0, 12) + " : " + value });
    }

    public async Task PreviewAsync(Job job, bool forExecutionLog, IApplicationCommandExecutionContext context, ISubJobRunner runner, ISubJobDetailRunner detailRunner, Dictionary<string, Login> accessCodes) {
        await context.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = "Job" });
        if (job.Description.Length != 0) {
            if (forExecutionLog) {
                await ExecutionLogEntryAsync(context, Properties.Resources.Executed, job.Description);
            } else {
                await context.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = job.Description });
            }
        }
        if (job.Machine.Length != 0) {
            await ExecutionLogEntryAsync(context, Properties.Resources.Machine, job.Machine);
        }
        if (job.SecondaryMachine.Length != 0) {
            await ExecutionLogEntryAsync(context, Properties.Resources.SecondaryMachine, job.SecondaryMachine);
        }
        if (IsWrongMachine(job)) {
            return;
        }

        if (job.AdjustedFolder.Length != 0) {
            await ExecutionLogEntryAsync(context, Properties.Resources.Folder, job.AdjustedFolder);
        }
        if (job.AdjustedDestinationFolder.Length != 0) {
            await ExecutionLogEntryAsync(context, Properties.Resources.Destination, job.AdjustedDestinationFolder);
        }
        if (forExecutionLog) {
            return;
        }

        if (job.Name.Length != 0 && job.Description.IndexOf(job.Name, StringComparison.Ordinal) < 0) {
            await ExecutionLogEntryAsync(context, Properties.Resources.Name, job.Name);
        }
        await ExecutionLogEntryAsync(context, Properties.Resources.Type, Enum.GetName(typeof(CargoJobType), job.JobType));
        foreach (SubJob subJob in job.SubJobs) {
            await runner.PreviewAsync(subJob, job, false, context, detailRunner, accessCodes);
        }
    }

    public async Task<bool> RunAsync(Job job, DateTime today, IApplicationCommandExecutionContext context,
            ISubJobRunner runner, ISubJobDetailRunner detailRunner,
            CrypticKey crypticKey, Dictionary<string, Login> accessCodes) {
        if (!await RunCleanUpUrlAsync(job, context)) { return false; }

        foreach (SubJob nextSubJob in job.SubJobs) {
            await runner.PreviewAsync(nextSubJob, job, true, context, detailRunner, accessCodes);
            if (!await runner.RunAsync(nextSubJob, today, job, context, detailRunner, crypticKey, accessCodes)) {
                return false;
            }
        }

        return true;
    }

    private static async Task<bool> RunCleanUpUrlAsync(Job job, IApplicationCommandExecutionContext context) {
        if (job.JobType != CargoJobType.CleanUp || string.IsNullOrEmpty(job.Url)) {
            return true;
        }

        string url = job.Url;

        await context.ReportAsync(new FeedbackToApplication { Type = FeedbackType.LogInformation, Message = "    " });
        await context.ReportAsync(new FeedbackToApplication {
            Type = FeedbackType.LogInformation,
            Message = "    " + url
        });

        const string urlStart = "http://localhost/";
        if (!url.StartsWith(urlStart)) {
            await context.ReportAsync(new FeedbackToApplication {
                Type = FeedbackType.LogError,
                Message = "    " + string.Format(Properties.Resources.UrlMustStartWith, urlStart)
            });
            return false;
        }

        var client = new HttpClient();
        HttpResponseMessage result = await client.GetAsync(url);
        if (result.IsSuccessStatusCode) {
            await LogInformationFromUrlResultAsync(await result.Content.ReadAsStringAsync(), context);
            return true;
        }

        await context.ReportAsync(new FeedbackToApplication {
            Type = FeedbackType.LogError,
            Message = $"    {(int)result.StatusCode} {result.ReasonPhrase}"
        });
        return false;
    }

    private static async Task LogInformationFromUrlResultAsync(string contents,
            IApplicationCommandExecutionContext context) {

        int pos = -1;
        do {
            pos = contents.IndexOf("<div", pos + 1, StringComparison.InvariantCultureIgnoreCase);
            if (pos < 0) {
                return;
            }

            int pos2 = contents.IndexOf(">", pos + 1, StringComparison.InvariantCultureIgnoreCase);
            if (pos2 < 0) {
                continue;
            }

            int pos3 = contents.IndexOf("</div", pos2 + 1, StringComparison.InvariantCultureIgnoreCase);
            if (pos3 < 0) {
                continue;
            }

            await context.ReportAsync(new FeedbackToApplication {
                Type = FeedbackType.LogInformation,
                Message = "    " + contents.Substring(pos2 + 1, pos3 - pos2 - 1)
            });
        } while (true);
    }
}