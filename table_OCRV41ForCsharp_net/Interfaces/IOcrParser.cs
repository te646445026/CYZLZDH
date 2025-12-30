using table_OCRV41ForCsharp_net.Models;

namespace table_OCRV41ForCsharp_net.Interfaces;

public interface IOcrParser
{
    OcrResult Parse(string json);
}