using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Jobz;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;
using Resources = Aspenlaub.Net.GitHub.CSharp.Cargobay.Properties.Resources;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Application;

public class ExecuteCommand : IApplicationCommand {
    protected IJobRunningApplication JobRunningApplication;
    protected IJobSelector JobSelector;
    protected ICrypticKeyProvider CrypticKeyProvider;
    protected IPasswordProvider PasswordProvider;

    public bool MakeLogEntries => false;
    public string Name => Resources.ExecuteCommandName;
    public async Task<bool> CanExecuteAsync() { return await Task.FromResult(true); }

    public ExecuteCommand(IJobRunningApplication jobRunningApplication, IJobSelector jobSelector, ICrypticKeyProvider crypticKeyProvider, IPasswordProvider passwordProvider) {
        JobRunningApplication = jobRunningApplication;
        JobSelector = jobSelector;
        CrypticKeyProvider = crypticKeyProvider;
        PasswordProvider = passwordProvider;
    }

    public async Task ExecuteAsync(IApplicationCommandExecutionContext context) {
        var job = JobSelector.SelectedJob;
        var crypticKey = job.JobType == CargoJobType.Zip ? CrypticKeyProvider.GetCrypticKey(CargoHelper.Clue, CargoHelper.Sha1) : null;
        var sites = new HashSet<string>();
        var accessCodes = new Dictionary<string, Login>();
        foreach (var subJob in job.SubJobs.Where(subJob => subJob.Url.Length != 0)) {
            CargoHelper.Site(subJob.Url, out var site, out var validUr);
            if (!validUr) { continue; }
            if (sites.Contains(site)) { continue; }

            var login = PasswordProvider.GetAccessCodes(site);
            if (login == null) { continue; }

            accessCodes[site] = login;
            sites.Add(site);
        }

        await JobRunningApplication.RunAsync(job, crypticKey, accessCodes);
    }
}