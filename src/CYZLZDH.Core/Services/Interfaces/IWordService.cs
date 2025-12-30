using System.Collections.Generic;
using CYZLZDH.Core.Models;

namespace CYZLZDH.Core.Interfaces;

public interface IWordService
{
    DocumentInfo LoadDocument(string filePath);
    List<MarkerInfo> FindMarkers(DocumentInfo doc);
    void ReplaceMarker(DocumentInfo doc, string markerId, string value);
    void ReplaceMarkers(DocumentInfo doc, Dictionary<string, string> replacements);
    void SaveAs(DocumentInfo doc, string outputPath);
    void ReplaceTitle(DocumentInfo doc, string newTitle);
    void CropImages(DocumentInfo doc, float keepRatio);
}
