using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces;

public interface ISubJobDetailRunner {
    Task PreviewAsync(SubJobDetail subJobDetail, IApplicationCommandExecutionContext context);
    Task<bool> RunAsync(SubJobDetail subJobDetail, DateTime today, Job job, SubJob subJob, IApplicationCommandExecutionContext context, CrypticKey crypticKey, Dictionary<string, Login> accessCodes);
}