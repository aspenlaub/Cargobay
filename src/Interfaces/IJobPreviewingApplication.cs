using System.Collections.Generic;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces {
    public interface IJobPreviewingApplication {
        void Preview(Job job, Dictionary<string, Login> accessCodes);
    }
}
