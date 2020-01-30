﻿using System.IO;
using System.Linq;
using System.Text;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Extensions {
    public static class JobsExtensions {
        public static CargoJobs Load(IXmlDeserializer xmlDeserializer, IJobFolderAdjuster jobFolderAdjuster, string folder, string utf8FileName, IErrorsAndInfos errorsAndInfos) {
            var fileName = ReplaceInvalidCharacters(utf8FileName);
            if (!File.Exists(folder + fileName)) { return new CargoJobs(); }

            var jobs = xmlDeserializer.Deserialize<CargoJobs>(File.ReadAllText(folder + fileName, Encoding.UTF8));
            jobs.ForEach(job => jobFolderAdjuster.AdjustJobAndSubFolders(job, errorsAndInfos));
            return jobs;
        }

        private static string ReplaceInvalidCharacters(string utf8FileName) {
            return Path.GetInvalidFileNameChars().Aggregate(utf8FileName, (current, c) => current.Replace(c, '_'));
        }

        public static bool Save(this CargoJobs jobs, IXmlSerializer xmlSerializer, string fileName) {
            if (File.Exists(fileName)) { return false; }

            var xml = xmlSerializer.Serialize(jobs);
            File.WriteAllText(fileName, xml, Encoding.UTF8);
            return true;
        }
    }
}