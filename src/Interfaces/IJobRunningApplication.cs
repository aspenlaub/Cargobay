using System.Collections.Generic;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces;

public interface IJobRunningApplication {
    Task RunAsync(Job job, CrypticKey crypticKey, Dictionary<string, Login> accessCodes);
}