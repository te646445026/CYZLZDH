namespace table_OCRV41ForCsharp_net.Interfaces;

public interface IOcrService
{
    string RecognizeTable(string imageBase64);
}