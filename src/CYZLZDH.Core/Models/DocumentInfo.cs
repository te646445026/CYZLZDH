using System.Collections.Generic;

namespace CYZLZDH.Core.Models;

public class DocumentInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public List<MarkerInfo> Markers { get; set; } = new();
}
