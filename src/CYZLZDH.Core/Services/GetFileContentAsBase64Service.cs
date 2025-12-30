using System;
using System.IO;
using CYZLZDH.Core.Interfaces;

namespace CYZLZDH.Core.Services;

public class GetFileContentAsBase64Service : IGetFileContentAsBase64Service
{
    public string GetFileContentAsBase64(string path)
    {
        using (FileStream filestream = new FileStream(path, FileMode.Open))
        {
            byte[] arr = new byte[filestream.Length];
            filestream.Read(arr, 0, (int)filestream.Length);
            string base64 = Convert.ToBase64String(arr);
            base64 = "{\"ImageBase64\":\"data:image/png;base64," + base64 + "\"}";
            return base64;
        }
    }
}
