using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;

public class SubJob {
    [XmlAttribute("guid")]
    public string Guid { get; set; }

    [XmlIgnore]
    public List<SubJobDetail> SubJobDetails { get; }

    [XmlAttribute("folder"), DefaultValue("")]
    public string LogicalFolder { get; set; }

    [XmlAttribute("destinationfolder"), DefaultValue("")]
    public string LogicalDestinationFolder { get; set; }

    [XmlAttribute("wildcard"), DefaultValue("")]
    public string Wildcard { get; set; }

    [XmlAttribute("url"), DefaultValue("")]
    public string Url { get; set; }

    [XmlIgnore]
    public FolderAdjustmentState FolderAdjustmentState { get; set; }

    private string PrivateAdjustedFolder;
    [XmlIgnore]
    public string AdjustedFolder {
        get {
            if (FolderAdjustmentState == FolderAdjustmentState.NotAdjusted) {
                throw new Exception("Sub job folder has not been adjusted");
            }
            return PrivateAdjustedFolder;
        }
        set => PrivateAdjustedFolder = value;
    }

    private string PrivateAdjustedDestinationFolder;
    [XmlIgnore]
    public string AdjustedDestinationFolder {
        get {
            if (FolderAdjustmentState == FolderAdjustmentState.NotAdjusted) {
                throw new Exception("Sub job destination folder has not been adjusted");
            }
            return PrivateAdjustedDestinationFolder;
        }
        set => PrivateAdjustedDestinationFolder = value;
    }

    public SubJob() {
        Guid = System.Guid.NewGuid().ToString();
        LogicalFolder = string.Empty;
        Wildcard = string.Empty;
        LogicalDestinationFolder = string.Empty;
        SubJobDetails = new List<SubJobDetail>();
        Url = string.Empty;
        FolderAdjustmentState = FolderAdjustmentState.NotAdjusted;
        AdjustedFolder = string.Empty;
        AdjustedDestinationFolder = string.Empty;
    }
}