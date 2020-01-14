using System.Threading.Tasks;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces {
    public interface IJobsRefreshingApplication {
        Task RefreshJobsAsync();
    }
}