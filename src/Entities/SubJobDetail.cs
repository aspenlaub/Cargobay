using System.Xml.Serialization;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities {
    public class SubJobDetail {
        [XmlIgnore]
        public string Description { get; set; }

        [XmlIgnore]
        public string FileName { get; set; }

        public SubJobDetail() {
            Description = string.Empty;
            FileName = string.Empty;
        }

    }
}