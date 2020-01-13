using System;
using System.Collections.Generic;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces {
    public interface IJobRunner {
        bool IsRightMachine(Job job);
        bool IsWrongMachine(Job job);

        void Preview(Job job, bool forExecutionLog, IApplicationCommandExecutionContext context, ISubJobRunner runner, ISubJobDetailRunner detailRunner, Dictionary<string, Login> accessCodes);
        bool Run(Job job, DateTime today, IApplicationCommandExecutionContext context, ISubJobRunner runner, ISubJobDetailRunner detailRunner, CrypticKey crypticKey, Dictionary<string, Login> accessCodes);
    }
}
