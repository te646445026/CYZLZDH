using System;
using System.IO;
using Newtonsoft.Json;
using CYZLZDH.Core.Interfaces;
using CYZLZDH.Core.Models;

namespace CYZLZDH.Core.Services;

public class KeyService : IKeyService
{
    public KEY CheckKey()
    {
        KEY myKey = new KEY();
        string keyPath = System.Environment.CurrentDirectory + @"\key.json";
        if (!File.Exists(keyPath))
        {
            throw new FileNotFoundException($"密钥文件缺失: {keyPath}");
        }
        else
        {
            string keyJson = File.ReadAllText(keyPath);
            myKey = JsonConvert.DeserializeObject<KEY>(keyJson) ?? throw new InvalidOperationException("密钥文件格式错误");
        }

        return myKey;
    }
}
