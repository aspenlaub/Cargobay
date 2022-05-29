using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces;

public interface ISubJobRunner {
    Task PreviewAsync(SubJob subJob, Job job, bool forExecutionLog, IApplicationCommandExecutionContext context, ISubJobDetailRunner runner, Dictionary<string, Login> accessCodes);
    Task<bool> RunAsync(SubJob subJob, DateTime today, Job job, IApplicationCommandExecutionContext context, ISubJobDetailRunner runner, CrypticKey crypticKey, Dictionary<string, Login> accessCodes);
}