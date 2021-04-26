using System.Collections.Generic;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces {
    public interface IJobPreviewingApplication {
        Task PreviewAsync(Job job, Dictionary<string, Login> accessCodes);
    }
}
