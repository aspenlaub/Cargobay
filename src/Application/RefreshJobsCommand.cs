using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Properties;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Application {
    public class RefreshJobsCommand : IApplicationCommand {
        protected IJobsRefreshingApplication JobsRefreshingApplication;

        public bool MakeLogEntries => false;
        public string Name => Resources.RefreshJobsCommandName;
        public async Task<bool> CanExecuteAsync() { return await Task.FromResult(true); }

        public RefreshJobsCommand(IJobsRefreshingApplication jobsRefreshingApplication) {
            JobsRefreshingApplication = jobsRefreshingApplication;
        }

        public async Task ExecuteAsync(IApplicationCommandExecutionContext context) {
            await JobsRefreshingApplication.RefreshJobsAsync();
        }
    }
}
