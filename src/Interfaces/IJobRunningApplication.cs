using System.Collections.Generic;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces {
    public interface IJobRunningApplication {
        void Run(Job job, CrypticKey crypticKey, Dictionary<string, Login> accessCodes);
    }
}
