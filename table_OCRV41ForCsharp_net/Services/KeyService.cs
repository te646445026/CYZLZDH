using Newtonsoft.Json;
using table_OCRV41ForCsharp_net.Interfaces;
using table_OCRV41ForCsharp_net.Models;

namespace table_OCRV41ForCsharp_net.Services;

// 定义 API 密钥服务
public class KeyService : IKeyService
{
    public KEY CheckKey()
    {
        KEY? myKey = new KEY();
        string keyPath = System.Environment.CurrentDirectory + @"\key.json";
        if (!File.Exists(keyPath))
        {
            MessageBox.Show("密钥文件缺失,点击确认后手动输入");

            Console.WriteLine("请输入API_KEY");
            do
            {
                myKey.API_KEY = Console.ReadLine();
            } while (myKey.API_KEY == null);

            Console.WriteLine("请输入SECRET_KEY");
            do
            {
                myKey.SECRET_KEY = Console.ReadLine();
            } while (myKey.SECRET_KEY == null);

            string keyJson = JsonConvert.SerializeObject(myKey);

            File.WriteAllText(keyPath, keyJson);

        }
        else
        {
            string keyJson = File.ReadAllText(keyPath);
            myKey = JsonConvert.DeserializeObject<KEY>(keyJson);
        }

        return myKey;
    }
}