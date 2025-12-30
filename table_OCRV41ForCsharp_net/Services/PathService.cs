using Newtonsoft.Json;
using table_OCRV41ForCsharp_net.Interfaces;
using table_OCRV41ForCsharp_net.Models;

namespace table_OCRV41ForCsharp_net.Services;

// 定义路径服务
/**
* 检查默认配置文件，如果存在，则读取对应部分作为默认路径，如果不存在，则新建并要求用户选择路径作为默认路径
* @param 可选参数defaultPath 默认为调试用的路径
* @return 返回一个对象，FolderPath为工作路径，DefaultJsonFilePath为默认参数的json文件路径，
*                DataFilePath为截图保存文件的路径,DataJsonFilePath为识别结果保存的文件路径
*/
public class PathService : IPathService
{
    public PathMessage CheckDefaultPath()
    {
        PathMessage path = new PathMessage();


        //检查是否有配置文件，没有就生成，并选择路径
        path.DefaultJsonFilePath = System.Environment.CurrentDirectory + @"\default.json";


        if (!File.Exists(path.DefaultJsonFilePath))
        {
            //选择默认路径

            MessageBox.Show("请选择需要识别的图片所在文件夹");
            FolderBrowserDialog folder1 = new FolderBrowserDialog();
            folder1.Description = "请选择需要识别的图片所在文件夹";

            if (folder1.ShowDialog() == DialogResult.OK)
            {
                path.DataFilePath = folder1.SelectedPath;
                Console.WriteLine("------------------------------------------------");
                Console.WriteLine($"已选择需要识别的图片所在文件夹：{path.DataFilePath}");
                Console.WriteLine("");
            }
            //增加消息框弹出提醒

            MessageBox.Show("请选择识别结果存放的文件夹");
            FolderBrowserDialog folder2 = new FolderBrowserDialog();
            folder2.Description = "请选择识别结果存放的文件夹";

            if (folder2.ShowDialog() == DialogResult.OK)
            {
                path.DataJsonFilePath = folder2.SelectedPath;
                Console.WriteLine("------------------------------------------------");
                Console.WriteLine($"已选择识别结果存放的文件夹：{path.DataJsonFilePath}");
                Console.WriteLine("");
            }
            //确定工作路径

            path.FolderPath = Path.GetDirectoryName(folder1.SelectedPath);

            Console.WriteLine($"当前工作文件夹目录为：{path.FolderPath}");

            //把默认路径写入json文件中
            string defaultStr = JsonConvert.SerializeObject(path);
            File.WriteAllText(path.DefaultJsonFilePath, defaultStr);
        }
        else
        {
            //读取DataFilePath和DataJsonFilePath
            string defaultStr = File.ReadAllText(path.DefaultJsonFilePath);
            PathMessage? defaultStrPath = JsonConvert.DeserializeObject<PathMessage>(defaultStr);
            path.DataFilePath = defaultStrPath.DataFilePath;
            path.DataJsonFilePath = defaultStrPath.DataJsonFilePath;
            //确定工作路径
            path.FolderPath = Path.GetDirectoryName(path.DataFilePath);
            Console.WriteLine($"当前工作文件夹目录为：{path.FolderPath}");
        }

        return path;
    }
}