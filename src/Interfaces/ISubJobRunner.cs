using System;
using System.Collections.Generic;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces {
    public interface ISubJobRunner {
        void Preview(SubJob subJob, Job job, bool forExecutionLog, IApplicationCommandExecutionContext context, ISubJobDetailRunner runner, Dictionary<string, Login> accessCodes);
        bool Run(SubJob subJob, DateTime today, Job job, IApplicationCommandExecutionContext context, ISubJobDetailRunner runner, CrypticKey crypticKey, Dictionary<string, Login> accessCodes);
    }
}