using System;
using System.Collections;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using NPOI.XWPF.UserModel;
using table_OCRV41ForCsharp_net.Interfaces;
using table_OCRV41ForCsharp_net.Models;
using table_OCRV41ForCsharp_net.Services;
using table_OCRV41ForCsharp_net.Exceptions;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace table_OCRV41ForCsharp_net
{
    /// <summary>
    /// 程序入口类
    /// </summary>
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            var serviceProvider = serviceCollection.BuildServiceProvider();
            
            // 运行HolidayService测试
            if (args.Length > 0)
            {
                switch (args[0])
                {
                    case "test-holiday":
                        // 运行HolidayService测试
                        HolidayServiceTest.RunAllTests().Wait();
                        break;
                    case "test-logger":
                        // 运行Logger测试
                        LoggerTest.RunLoggerTest();
                        break;
                }
            }
            else
            {
                // 运行正常的处理流程
                Process(serviceProvider);
            }
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // 配置 NLog
            LogManager.LoadConfiguration("nlog.config");
            
            // 添加日志服务
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
                builder.AddNLog();
            });
            
            // 添加配置服务
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("default.json", optional: true, reloadOnChange: true)
                .Build();
            services.AddSingleton<IConfiguration>(configuration);
            
            // 注册业务服务
            services.AddSingleton<IPathService, PathService>();
            services.AddSingleton<IKeyService, KeyService>();
            services.AddSingleton<IGetFileContentAsBase64Service, GetFileContentAsBase64Service>();
            services.AddSingleton<IOcrParser, TencentOcrParser>();

            // 使用工厂模式注册 TencentOcrService
            services.AddSingleton<IOcrService>(provider =>
            {
                var keyService = provider.GetService<IKeyService>();
                var secretId = keyService.CheckKey().API_KEY;
                var secretKey = keyService.CheckKey().SECRET_KEY;
                return new TencentOcrService(secretId, secretKey);
            });
        }

        /// <summary>
        /// 初始化全局异常处理
        /// </summary>
        /// <param name="logger">日志记录器</param>
        private static void InitializeExceptionHandler(ILogger<Program> logger)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var exception = (Exception)args.ExceptionObject;
                logger?.LogCritical(exception, "程序遇到了未处理的异常");
                MessageBox.Show("程序遇到了未处理的异常，请查看日志文件获取详细信息。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
        }

        
        /// <summary>
        /// 使用重试机制执行操作
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="operation">要执行的操作</param>
        /// <param name="maxRetries">最大重试次数</param>
        /// <returns>操作结果</returns>
        private static async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, int maxRetries = 3)
        {
            int retryCount = 0;
            Exception lastException = null;
            
            while (retryCount < maxRetries)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    retryCount++;
                    
                    if (retryCount >= maxRetries)
                        break;
                        
                    // 指数退避策略
                    int delayMs = (int)Math.Pow(2, retryCount) * 1000;
                    await Task.Delay(delayMs);
                }
            }
            
            throw new Exception($"操作失败，已重试{maxRetries}次", lastException);
        }

        /// <summary>
        /// 主处理流程
        /// </summary>
        /// <param name="serviceProvider">服务提供者</param>
        private static void Process(IServiceProvider serviceProvider)
        {
            // 获取日志记录器
            var logger = serviceProvider.GetService<ILogger<Program>>();
            
            // 初始化异常处理
            InitializeExceptionHandler(logger);

            try
            {
                logger?.LogInformation("应用程序启动");
                
                // 显示程序启动界面
                DisplayWelcomeScreen();
                
                // 显示主菜单并获取用户选择
                var userChoice = DisplayMainMenuAndGetChoice(logger);

                string? workPath;
                string dataDir = "";
                string folderDir = "";

                var ocrService = serviceProvider.GetService<IOcrService>();
                var pathService = serviceProvider.GetService<IPathService>();
                var getFileContentAsBase64Service = serviceProvider.GetService<IGetFileContentAsBase64Service>();
                var ocrParser = serviceProvider.GetService<IOcrParser>();

                if (ocrService == null || pathService == null || getFileContentAsBase64Service == null || ocrParser == null)
                {
                    throw new Exception("无法解析所需的服务");
                }

                ArrayList resultDir = new ArrayList();
                OcrResult resultForJsonMessage = new OcrResult();

                try
                {
                    logger?.LogInformation("检查默认路径");
                    PathMessage path = pathService.CheckDefaultPath();
                    workPath = path.FolderPath;
                    logger?.LogInformation("工作路径: {WorkPath}", workPath);

                    if (userChoice == "1")
                    {
                        //从json文件中读取
                        logger?.LogInformation("从JSON文件中读取数据");
                        dataDir = path.DataFilePath + "\\";
                        folderDir = path.DataJsonFilePath + "\\";

                        logger?.LogInformation("数据目录: {DataDir}", dataDir);
                        logger?.LogInformation("JSON文件目录: {FolderDir}", folderDir);
                    }
                    else
                    {
                        try
                        {
                            logger?.LogInformation("打开文件选择对话框");
                            // 创建 OpenFileDialog 对象
                            OpenFileDialog fileDialog = new OpenFileDialog();

                            // 设置对话框的属性
                            fileDialog.Multiselect = true; // 允许多选文件
                            fileDialog.Title = "请选择文件"; // 设置对话框的标题
                            fileDialog.Filter = "json文件(*.json)|*.json"; // 设置对话框的文件过滤器
                            fileDialog.InitialDirectory = path.DataJsonFilePath; // 设置初始目录

                            // 显示对话框并获取用户选择的文件路径
                            DialogResult result = fileDialog.ShowDialog();
                            if (result == DialogResult.OK)
                            {
                                foreach (string fileName in fileDialog.FileNames)
                                {
                                    resultDir.Add(fileName); // 获取用户选择的多个文件名的数组
                                    logger?.LogInformation("选择文件: {FileName}", fileName);
                                }
                            }
                            else
                            {
                                logger?.LogWarning("用户取消了文件选择");
                            }
                        }
                        catch (Exception ex)
                        {
                            logger?.LogError(ex, "打开文件选择对话框时出错");
                            MessageBox.Show($"选择文件时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }

                    if (userChoice == "1")
                    {
                        try
                        {
                            logger?.LogInformation("开始处理图片文件");
                            int num = 0;
                            DirectoryInfo directoryInfo = new DirectoryInfo(dataDir);

                            if (!directoryInfo.Exists)
                            {
                                throw new DirectoryNotFoundException($"目录不存在: {dataDir}");
                            }

                            foreach (FileInfo file in directoryInfo.GetFiles())
                            {
                                try
                                {
                                    logger?.LogInformation("处理文件: {FileName}", file.Name);
                                    Console.WriteLine("{0}: {1} 正在处理：", num + 1, file.Name.Split('.')[0]);

                                    string imageBase64 = getFileContentAsBase64Service.GetFileContentAsBase64(file.FullName);
                                    string data_json = ocrService.RecognizeTable(imageBase64);
                                    string jsonFile_name = folderDir + file.Name.Split('.')[0] + ".json";

                                    File.WriteAllText(jsonFile_name, data_json);
                                    logger?.LogInformation("文件处理完成: {JsonFileName}", jsonFile_name);

                                    Console.WriteLine("{0}: {1} 下载完成。", num + 1, jsonFile_name);
                                    num++;
                                    Console.WriteLine("--------------------------------------");
                                    Console.WriteLine("");
                                    Thread.Sleep(1000);
                                }
                                catch (Exception ex)
                                {
                                    logger?.LogError(ex, "处理文件 {FileName} 时出错", file.Name);
                                    Console.WriteLine($"处理文件 {file.Name} 时出错: {ex.Message}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            logger?.LogError(ex, "处理图片文件时出错");
                            MessageBox.Show($"处理图片文件时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }

                    if (userChoice == "1")
                    {
                        try
                        {
                            logger?.LogInformation("从目录加载JSON文件: {FolderDir}", folderDir);
                            string[] file_dir = Directory.GetFiles(folderDir);
                            for (int i = 0; i < file_dir.Length; i++)
                            {
                                resultDir.Add(file_dir[i]);
                                logger?.LogInformation("添加JSON文件: {FilePath}", file_dir[i]);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger?.LogError(ex, "加载JSON文件时出错");
                            MessageBox.Show($"加载JSON文件时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    int fileNum = 0;
                    logger?.LogInformation("开始处理JSON文件，共 {FileCount} 个文件", resultDir.Count);

                    foreach (string jsonPath in resultDir)
                    {
                        try
                        {
                            fileNum++;
                            logger?.LogInformation("处理JSON文件 {FileNum}/{TotalCount}: {JsonPath}", fileNum, resultDir.Count, jsonPath);
                            Console.WriteLine("-----------{0}-------------", fileNum);

                            // 把识别结果的json文档信息提取出来
                            string json = File.ReadAllText(jsonPath);
                            resultForJsonMessage = ocrParser.Parse(json);
                            logger?.LogInformation("成功解析JSON文件: {FileName}", Path.GetFileName(jsonPath));

                            // 根据模板，写入对应的word文档里面
                            string recordTemplatePath = workPath + "\\限速器测试记录模板4.docx";
                            string reportTemplatePath = workPath + "\\限速器测试报告模板4.docx";

                            if (!File.Exists(recordTemplatePath) || !File.Exists(reportTemplatePath))
                            {
                                throw new FileNotFoundException("模板文件不存在", !File.Exists(recordTemplatePath) ? recordTemplatePath : reportTemplatePath);
                            }

                            logger?.LogInformation("打开Word模板文件");
                            FileStream docFlieRec = new FileStream(recordTemplatePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                            FileStream docFlieRep = new FileStream(reportTemplatePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);

                            XWPFDocument documentRec = new XWPFDocument(docFlieRec);
                            XWPFDocument documentRep = new XWPFDocument(docFlieRep);

                            IList<XWPFParagraph> paragraphsRec = documentRec.Paragraphs;
                            Console.WriteLine(paragraphsRec[2].ParagraphText + resultForJsonMessage.ReportNum);

                            IList<XWPFTable> tablesRec = documentRec.Tables;
                            XWPFTable tableRec0 = tablesRec[0];
                            XWPFTable tableRec1 = tablesRec[1];

                            
                            IList<XWPFParagraph> paragraphsRep = documentRep.Paragraphs;
                            Console.WriteLine(paragraphsRep[3].ParagraphText + resultForJsonMessage.ReportNum);

                            IList<XWPFTable> tablesRep = documentRep.Tables;
                            XWPFTable tableRep0 = tablesRep[0];
                            XWPFTable tableRep1 = tablesRep[1];

                            logger?.LogInformation("开始填充Word文档内容");

                            //写入记录for模板3
                            try
                            {
                                tableRec0.GetRow(0).GetCell(1).SetText(resultForJsonMessage.UserName);
                                //左对齐
                                tableRec0.GetRow(0).GetCell(1).Paragraphs[0].Alignment = ParagraphAlignment.LEFT;
                            }
                            catch (Exception ex)
                            {
                                logger?.LogWarning(ex, "设置委托单位时出错");
                                Console.WriteLine("userName write error");
                            }

                            try
                            {
                                tableRec0.GetRow(1).GetCell(1).SetText(resultForJsonMessage.UserName);
                                //左对齐
                                tableRec0.GetRow(1).GetCell(1).Paragraphs[0].Alignment = ParagraphAlignment.LEFT;
                            }
                            catch (Exception ex)
                            {
                                logger?.LogWarning(ex, "设置使用单位时出错");
                                Console.WriteLine("userName write error");
                            }

                            try
                            {
                                tableRec0.GetRow(2).GetCell(1).SetText(resultForJsonMessage.MaintenanceUnit);
                                //左对齐
                                tableRec0.GetRow(2).GetCell(1).Paragraphs[0].Alignment = ParagraphAlignment.LEFT;
                            }
                            catch
                            {
                                Console.WriteLine("MaintenanceUnit write error");
                            }

                            try
                            {
                                tableRec0.GetRow(3).GetCell(1).SetText(resultForJsonMessage.UsingAddress);
                                //左对齐
                                tableRec0.GetRow(3).GetCell(1).Paragraphs[0].Alignment = ParagraphAlignment.LEFT;
                            }
                            catch
                            {
                                Console.WriteLine("UsingAddress write error");
                            }

                            try
                            {
                                tableRec0.GetRow(4).GetCell(2).SetText(resultForJsonMessage.ElevatorDeviceType);
                                //左对齐
                                tableRec0.GetRow(4).GetCell(2).Paragraphs[0].Alignment = ParagraphAlignment.LEFT;
                            }
                            catch
                            {
                                Console.WriteLine("ElevatorDeviceType write error");
                            }

                            try
                            {
                                tableRec0.GetRow(4).GetCell(4).SetText(resultForJsonMessage.DeviceCode);
                                //左对齐
                                tableRec0.GetRow(4).GetCell(4).Paragraphs[0].Alignment = ParagraphAlignment.LEFT;
                            }
                            catch
                            {
                                Console.WriteLine("DeviceCode write error");
                            }

                            try
                            {
                                tableRec0.GetRow(5).GetCell(2).SetText(resultForJsonMessage.SerialNum);
                                //左对齐
                                tableRec0.GetRow(5).GetCell(2).Paragraphs[0].Alignment = ParagraphAlignment.LEFT;
                            }
                            catch
                            {
                                Console.WriteLine("SerialNum write error");
                            }

                            try
                            {
                                tableRec0.GetRow(5).GetCell(4).SetText(resultForJsonMessage.Speed + "m/s");
                                //左对齐
                                tableRec0.GetRow(5).GetCell(4).Paragraphs[0].Alignment = ParagraphAlignment.LEFT;
                            }
                            catch
                            {
                                Console.WriteLine("speed write error");
                            }

                           try
                           {
                                tableRec0.GetRow(6).GetCell(2).SetText(resultForJsonMessage.XiansuqiManufacturingUnit);
                                //左对齐
                                tableRec0.GetRow(6).GetCell(2).Paragraphs[0].Alignment = ParagraphAlignment.LEFT;
                           }
                           catch
                           {
                                Console.WriteLine("XiansuqiManufacturingUnit write error");
                           }

                           

                           try
                           {
                                tableRec0.GetRow(7).GetCell(2).SetText(resultForJsonMessage.XiansuqiModel);
                                //左对齐
                                tableRec0.GetRow(7).GetCell(2).Paragraphs[0].Alignment = ParagraphAlignment.LEFT;
                           }
                           catch
                           {
                                Console.WriteLine("XiansuqiModel write error");
                           }

                            try
                           {
                                tableRec0.GetRow(7).GetCell(4).SetText(resultForJsonMessage.XiansuqiNum);
                                //左对齐
                                tableRec0.GetRow(7).GetCell(4).Paragraphs[0].Alignment = ParagraphAlignment.LEFT;
                           }
                           catch
                           {
                                Console.WriteLine("XiansuqiNum write error");
                           }

                            try
                           {
                                tableRec0.GetRow(8).GetCell(4).SetText(resultForJsonMessage.XiansuqiDirection);
                                //左对齐
                                tableRec0.GetRow(8).GetCell(4).Paragraphs[0].Alignment = ParagraphAlignment.LEFT;
                           }
                           catch
                           {
                                Console.WriteLine("XiansuqiDirection write error");
                           }

                            try
                            {
                                tableRec0.GetRow(10).GetCell(1).Paragraphs[0].CreateRun().SetText(resultForJsonMessage.XiansuqiElectricalUpSpeed + "m/s");
                                //左对齐
                                tableRec0.GetRow(10).GetCell(1).Paragraphs[0].Alignment = ParagraphAlignment.LEFT;
                            }
                            catch
                            {
                                Console.WriteLine("XiansuqiElectricalUpSpeed write error");
                            }

                            try
                            {
                                tableRec0.GetRow(10).GetCell(2).Paragraphs[0].CreateRun().SetText(resultForJsonMessage.XiansuqiElectricalDownSpeed + "m/s");
                                //左对齐
                                tableRec0.GetRow(10).GetCell(2).Paragraphs[0].Alignment = ParagraphAlignment.LEFT;
                            }
                            catch
                            {
                                Console.WriteLine("XiansuqiElectricalDownSpeed write error");
                            }

                            try
                            {
                                tableRec0.GetRow(10).GetCell(3).Paragraphs[0].CreateRun().SetText(resultForJsonMessage.XiansuqiMechanicalUpSpeed + "m/s");
                                //左对齐
                                tableRec0.GetRow(10).GetCell(3).Paragraphs[0].Alignment = ParagraphAlignment.LEFT;
                            }
                            catch
                            {
                                Console.WriteLine("XiansuqiMechanicalUpSpeed write error");
                            }

                            try
                            {
                                tableRec0.GetRow(10).GetCell(4).Paragraphs[0].CreateRun().SetText(resultForJsonMessage.XiansuqiMechanicalDownSpeed + "m/s");
                                //左对齐
                                tableRec0.GetRow(10).GetCell(4).Paragraphs[0].Alignment = ParagraphAlignment.LEFT;
                            }
                            catch
                            {
                                Console.WriteLine("XiansuqiMechanicalDownSpeed write error");
                            }


                            try
                            {
                                tableRec0.GetRow(25).GetCell(0).Paragraphs[0].CreateRun().SetText(resultForJsonMessage.Date);
                                //右对齐
                                tableRec0.GetRow(25).GetCell(0).Paragraphs[0].Alignment = ParagraphAlignment.RIGHT;

                            }
                            catch
                            {
                                Console.WriteLine("date write error");
                            }

                            try
                            {
                                paragraphsRec[2].CreateRun().SetText(resultForJsonMessage.JianyanOrjiance.Equals("检验") ? "D" : "E");
                                paragraphsRec[2].CreateRun().SetText(resultForJsonMessage.ReportNum);
                                paragraphsRec[2].Alignment = ParagraphAlignment.RIGHT;
                            }
                            catch
                            {
                                Console.WriteLine("reportNum2 write error");
                            }


                            string outPath = string.Format(workPath + "\\{0}_{1}_{2}.docx",
                                                                resultForJsonMessage.DeviceCode,
                                                                Path.GetFileNameWithoutExtension(jsonPath),
                                                                resultForJsonMessage.NextYearFlag);
                            FileStream outFile = new FileStream(outPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                            documentRec.Write(outFile);
                            outFile.Close();
                            documentRec.Close();
                            docFlieRec.Close();

                            Console.WriteLine("{0}打印记录完成", Path.GetFileNameWithoutExtension(jsonPath));
                            Console.WriteLine("-------------------------------------------------------");
                            Console.WriteLine("");

                            //写入报告模板3
                            try
                            {
                                tableRep0.GetRow(0).GetCell(1).SetText(resultForJsonMessage.UserName);
                                //左对齐
                                tableRep0.GetRow(0).GetCell(1).Paragraphs[0].Alignment = ParagraphAlignment.LEFT;
                            }
                            catch
                            {
                                Console.WriteLine("userName write error");
                            }

                            try
                            {
                                tableRep0.GetRow(1).GetCell(1).SetText(resultForJsonMessage.UserName);
                                //左对齐
                                tableRep0.GetRow(1).GetCell(1).Paragraphs[0].Alignment = ParagraphAlignment.LEFT;
                            }
                            catch
                            {
                                Console.WriteLine("userName write error");
                            }

                            try
                            {
                                tableRep0.GetRow(2).GetCell(1).SetText(resultForJsonMessage.MaintenanceUnit);
                                //左对齐
                                tableRep0.GetRow(2).GetCell(1).Paragraphs[0].Alignment = ParagraphAlignment.LEFT;
                            }
                            catch
                            {
                                Console.WriteLine("MaintenanceUnit write error");
                            }

                            try
                            {
                                tableRep0.GetRow(3).GetCell(1).SetText(resultForJsonMessage.UsingAddress);
                                //左对齐
                                tableRep0.GetRow(3).GetCell(1).Paragraphs[0].Alignment = ParagraphAlignment.LEFT;
                            }
                            catch
                            {
                                Console.WriteLine("UsingAddress write error");
                            }

                            try
                            {
                                tableRep0.GetRow(4).GetCell(2).SetText(resultForJsonMessage.ElevatorDeviceType);
                                //左对齐
                                tableRep0.GetRow(4).GetCell(2).Paragraphs[0].Alignment = ParagraphAlignment.LEFT;
                            }
                            catch
                            {
                                Console.WriteLine("ElevatorDeviceType write error");
                            }

                            try
                            {
                                tableRep0.GetRow(4).GetCell(4).SetText(resultForJsonMessage.DeviceCode);
                                //左对齐
                                tableRep0.GetRow(4).GetCell(4).Paragraphs[0].Alignment = ParagraphAlignment.LEFT;
                            }
                            catch
                            {
                                Console.WriteLine("DeviceCode write error");
                            }

                            try
                            {
                                tableRep0.GetRow(5).GetCell(2).SetText(resultForJsonMessage.SerialNum);
                                //左对齐
                                tableRep0.GetRow(5).GetCell(2).Paragraphs[0].Alignment = ParagraphAlignment.LEFT;
                            }
                            catch
                            {
                                Console.WriteLine("SerialNum write error");
                            }

                            try
                            {
                                tableRep0.GetRow(5).GetCell(4).SetText(resultForJsonMessage.Speed+ "m/s");
                                //左对齐
                                tableRep0.GetRow(5).GetCell(4).Paragraphs[0].Alignment = ParagraphAlignment.LEFT;
                            }
                            catch
                            {
                                Console.WriteLine("Speed write error");
                            }

                            try
                            {
                                tableRep0.GetRow(6).GetCell(2).SetText(resultForJsonMessage.XiansuqiManufacturingUnit);
                                //左对齐
                                tableRep0.GetRow(6).GetCell(2).Paragraphs[0].Alignment = ParagraphAlignment.LEFT;
                            }
                            catch
                            {
                                Console.WriteLine("XiansuqiManufacturingUnit write error");
                            }

                            try
                            {
                                tableRep0.GetRow(7).GetCell(2).SetText(resultForJsonMessage.XiansuqiModel);
                                //左对齐
                                tableRep0.GetRow(7).GetCell(2).Paragraphs[0].Alignment = ParagraphAlignment.LEFT;
                            }
                            catch
                            {
                                Console.WriteLine("XiansuqiModel write error");
                            }

                            try
                            {
                                tableRep0.GetRow(7).GetCell(4).SetText(resultForJsonMessage.XiansuqiNum);
                                //左对齐
                                tableRep0.GetRow(7).GetCell(4).Paragraphs[0].Alignment = ParagraphAlignment.LEFT;
                            }
                            catch
                            {
                                Console.WriteLine("XiansuqiNum write error");
                            }

                            try
                            {
                                tableRep0.GetRow(8).GetCell(4).SetText(resultForJsonMessage.XiansuqiDirection);
                                //左对齐
                                tableRep0.GetRow(8).GetCell(4).Paragraphs[0].Alignment = ParagraphAlignment.LEFT;
                            }
                            catch
                            {
                                Console.WriteLine("XiansuqiDirection write error");
                            }

                            try
                            {
                                tableRep0.GetRow(10).GetCell(1).Paragraphs[0].CreateRun().SetText(resultForJsonMessage.XiansuqiElectricalUpSpeed + "m/s");
                                //左对齐
                                tableRep0.GetRow(10).GetCell(1).Paragraphs[0].Alignment = ParagraphAlignment.LEFT;
                            }
                            catch
                            {
                                Console.WriteLine("XiansuqiElectricalUpSpeed write error");
                            }

                            try
                            {
                                tableRep0.GetRow(10).GetCell(2).Paragraphs[0].CreateRun().SetText(resultForJsonMessage.XiansuqiElectricalDownSpeed + "m/s");
                                //左对齐
                                tableRep0.GetRow(10).GetCell(2).Paragraphs[0].Alignment = ParagraphAlignment.LEFT;
                            }
                            catch
                            {
                                Console.WriteLine("XiansuqiElectricalDownSpeed write error");
                            }

                            try
                            {
                                tableRep0.GetRow(10).GetCell(3).Paragraphs[0].CreateRun().SetText(resultForJsonMessage.XiansuqiMechanicalUpSpeed + "m/s");
                                //左对齐
                                tableRep0.GetRow(10).GetCell(3).Paragraphs[0].Alignment = ParagraphAlignment.LEFT;
                            }
                            catch
                            {
                                Console.WriteLine("XiansuqiMechanicalUpSpeed write error");
                            }

                            try
                            {
                                tableRep0.GetRow(10).GetCell(4).Paragraphs[0].CreateRun().SetText(resultForJsonMessage.XiansuqiMechanicalDownSpeed + "m/s");
                                //左对齐
                                tableRep0.GetRow(10).GetCell(4).Paragraphs[0].Alignment = ParagraphAlignment.LEFT;
                            }
                            catch
                            {
                                Console.WriteLine("XiansuqiMechanicalDownSpeed write error");
                            }



                            try
                            {
                                tableRep0.GetRow(22).GetCell(0).SetText(resultForJsonMessage.Date);
                                //右对齐
                                tableRep0.GetRow(22).GetCell(0).Paragraphs[0].Alignment = ParagraphAlignment.LEFT;
                            }
                            catch
                            {
                                Console.WriteLine("Date write error");
                            }

                            try
                            {
                                tableRep0.GetRow(23).GetCell(0).SetText(resultForJsonMessage.ShenheDate);
                                //右对齐
                                tableRep0.GetRow(23).GetCell(0).Paragraphs[0].Alignment = ParagraphAlignment.LEFT;
                            }
                            catch
                            {
                                Console.WriteLine("ShenheDate write error");
                            }

                            try
                            {
                                tableRep0.GetRow(24).GetCell(0).SetText(resultForJsonMessage.ShenheDate);
                                //右对齐
                                tableRep0.GetRow(24).GetCell(0).Paragraphs[0].Alignment = ParagraphAlignment.LEFT;
                            }
                            catch
                            {
                                Console.WriteLine("ShenheDate write error");
                            }


                            try
                            {
                                //
                                paragraphsRep[3].CreateRun().SetText(resultForJsonMessage.JianyanOrjiance.Equals("检验") ? "D" : "E");
                                paragraphsRep[3].CreateRun().SetText(resultForJsonMessage.ReportNum);
                                paragraphsRep[3].Alignment = ParagraphAlignment.RIGHT;
                                //
                                // 检查段落是否已有Run，如果有则复制格式
                                var newRun = paragraphsRep[15].CreateRun();
                                if (paragraphsRep[15].Runs.Count > 1)
                                {
                                    var existingRun = paragraphsRep[15].Runs[0];
                                    // 复制字体格式
                                    newRun.FontSize = existingRun.FontSize;
                                    newRun.FontFamily = existingRun.FontFamily;
                                    newRun.IsBold = existingRun.IsBold;
                                    newRun.IsItalic = existingRun.IsItalic;
                                    newRun.Underline = UnderlinePatterns.Single;
                                }
                                else
                                {
                                    newRun.Underline = UnderlinePatterns.Single; // 设置下划线
                                }
                                newRun.SetText(resultForJsonMessage.UserName);  
                                //
                                
                                newRun = paragraphsRep[17].CreateRun();
                                if (paragraphsRep[17].Runs.Count > 1)
                                {
                                    var existingRun = paragraphsRep[17].Runs[0];
                                    // 复制字体格式
                                    newRun.FontSize = existingRun.FontSize;
                                    newRun.FontFamily = existingRun.FontFamily;
                                    newRun.IsBold = existingRun.IsBold;
                                    newRun.IsItalic = existingRun.IsItalic;
                                    newRun.Underline = UnderlinePatterns.Single;
                                }
                                else
                                {
                                    newRun.Underline = UnderlinePatterns.Single; // 设置下划线
                                }
                                newRun.SetText(resultForJsonMessage.Date); 
                                //
                                paragraphsRep[53].CreateRun().SetText(resultForJsonMessage.JianyanOrjiance.Equals("检验") ? "D" : "E");
                                paragraphsRep[53].CreateRun().SetText(resultForJsonMessage.ReportNum);
                                paragraphsRep[53].Alignment = ParagraphAlignment.RIGHT;
                            }
                            catch
                            {
                                Console.WriteLine("reportNum2 write error");
                            }


                            // 保存报告文件
                            string outPath2 = string.Format(workPath + "\\{0}.docx", resultForJsonMessage.DeviceCode);
                            logger?.LogInformation("保存报告文件: {OutPath}", outPath2);
                            FileStream outFile2 = new FileStream(outPath2, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                            documentRep.Write(outFile2);
                            outFile2.Close();
                            documentRep.Close();
                            docFlieRep.Close();

                            Console.WriteLine("{0}打印报告完成", Path.GetFileNameWithoutExtension(jsonPath));
                            Console.WriteLine("-------------------------------------------------------");
                            Console.WriteLine("");
                        }
                        catch (Exception ex)
                        {
                            logger?.LogError(ex, "处理JSON文件 {JsonPath} 时出错", jsonPath);
                            MessageBox.Show($"处理文件时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }

                    logger?.LogInformation("所有文件处理完成");
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("🎉 处理完成！按任意键退出程序");
                    Console.ResetColor();
                    Console.ReadKey();
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "程序执行过程中发生错误");
                    MessageBox.Show($"程序执行过程中发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    logger?.LogInformation("应用程序结束");
                }
            }
            finally{}
        }
        
        /// <summary>
        /// 显示程序启动欢迎界面
        /// </summary>
        private static void DisplayWelcomeScreen()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    电梯限速器检测报告生成系统                    ║");
            Console.WriteLine("║                    Elevator Speed Limiter Report              ║");
            Console.WriteLine("║                         Generation System                     ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("📋 功能说明:");
            Console.WriteLine("   • 支持OCR图片识别，自动提取检测数据");
            Console.WriteLine("   • 支持从JSON文件读取已识别的数据");
            Console.WriteLine("   • 自动生成标准格式的Word检测报告");
            Console.ResetColor();
            Console.WriteLine();
        }
        
        /// <summary>
        /// 显示主菜单选择界面
        /// </summary>
        private static void DisplayMainMenu()
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("┌─────────────────────────────────────────┐");
            Console.WriteLine("│                 选择操作模式                │");
            Console.WriteLine("└─────────────────────────────────────────┘");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("📂 [0] 使用已生成的识别结果文件");
            Console.WriteLine("🖼️ [1] 上传图片进行OCR识别");
            Console.WriteLine();
            Console.Write("请输入您的选择 [0/1]: ");
        }
        
        /// <summary>
        /// 显示主菜单并获取用户选择
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <returns>用户选择的选项</returns>
        private static string DisplayMainMenuAndGetChoice(ILogger<Program> logger)
        {
            string userChoice;
            while (true)
            {
                DisplayMainMenu();
                userChoice = Console.ReadLine()?.Trim();
                
                if (userChoice == "0" || userChoice == "1")
                {
                    logger?.LogInformation("用户选择了选项: {UserChoice}", userChoice);
                    break;
                }
                
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ 无效的选择！请输入 0 或 1");
                Console.ResetColor();
                Console.WriteLine();
                logger?.LogWarning("用户输入了无效选择: {InvalidChoice}", userChoice);
            }
            
            return userChoice;
        }
    }
}
