using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Jobz;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;
using Resources = Aspenlaub.Net.GitHub.CSharp.Cargobay.Properties.Resources;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Application {
    public class PreviewCommand : IApplicationCommand {
        protected IJobPreviewingApplication JobPreviewingApplication;
        protected IJobSelector JobSelector;
        protected IPasswordProvider PasswordProvider;

        public bool MakeLogEntries => false;
        public string Name => Resources.PreviewCommandName;
        public bool CanExecute() { return true; }

        public PreviewCommand(IJobPreviewingApplication jobPreviewingApplication, IJobSelector jobSelector, IPasswordProvider passwordProvider) {
            JobPreviewingApplication = jobPreviewingApplication;
            JobSelector = jobSelector;
            PasswordProvider = passwordProvider;
        }

        public Task Execute(IApplicationCommandExecutionContext context) {
            var job = JobSelector.SelectedJob;
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

            return JobPreviewingApplication.PreviewAsync(job, accessCodes);
        }
    }
}
