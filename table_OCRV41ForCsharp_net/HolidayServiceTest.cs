using System;
using System.Threading.Tasks;
using table_OCRV41ForCsharp_net.Services;

namespace table_OCRV41ForCsharp_net
{
    /// <summary>
    /// 测试HolidayService类的功能
    /// </summary>
    public class HolidayServiceTest
    {
        /// <summary>
        /// 运行所有测试
        /// </summary>
        public static async Task RunAllTests()
        {
            Console.WriteLine("======= 开始测试HolidayService类 =======");
            Console.WriteLine();

            // 测试周末检测
            await TestWeekendDetection();
            Console.WriteLine();

            // 测试节假日API
            await TestHolidayApi();
            Console.WriteLine();

            // 测试下一个工作日计算
            await TestNextWorkingDay();
            Console.WriteLine();

            // 测试同步版本的下一个工作日计算
            TestSyncNextWorkingDay();
            Console.WriteLine();

            Console.WriteLine("======= 测试完成 =======");
        }

        /// <summary>
        /// 测试周末检测功能
        /// </summary>
        private static async Task TestWeekendDetection()
        {
            Console.WriteLine("测试周末检测功能:");
            
            // 创建一个周六的日期
            DateTime saturday = new DateTime(2023, 7, 1); // 2023年7月1日是周六
            bool isSaturdayHoliday = await HolidayService.IsHolidayOrWeekendAsync(saturday);
            Console.WriteLine($"2023年7月1日(周六)是否为节假日或周末: {isSaturdayHoliday}");
            
            // 创建一个周日的日期
            DateTime sunday = new DateTime(2023, 7, 2); // 2023年7月2日是周日
            bool isSundayHoliday = await HolidayService.IsHolidayOrWeekendAsync(sunday);
            Console.WriteLine($"2023年7月2日(周日)是否为节假日或周末: {isSundayHoliday}");
            
            // 创建一个工作日的日期
            DateTime workday = new DateTime(2023, 7, 3); // 2023年7月3日是周一
            bool isWorkdayHoliday = await HolidayService.IsHolidayOrWeekendAsync(workday);
            Console.WriteLine($"2023年7月3日(周一)是否为节假日或周末: {isWorkdayHoliday}");
        }

        /// <summary>
        /// 测试节假日API功能
        /// </summary>
        private static async Task TestHolidayApi()
        {
            Console.WriteLine("测试节假日API功能:");
            
            // 测试一个可能的法定节假日（以2023年国庆节为例）
            DateTime nationalDay = new DateTime(2023, 10, 1); // 2023年10月1日是国庆节
            bool isNationalDayHoliday = await HolidayService.IsHolidayOrWeekendAsync(nationalDay);
            Console.WriteLine($"2023年10月1日(国庆节)是否为节假日或周末: {isNationalDayHoliday}");
            
            // 测试当前日期
            DateTime today = DateTime.Today;
            bool isTodayHoliday = await HolidayService.IsHolidayOrWeekendAsync(today);
            Console.WriteLine($"今天({today:yyyy年MM月dd日})是否为节假日或周末: {isTodayHoliday}");
        }

        /// <summary>
        /// 测试下一个工作日计算功能
        /// </summary>
        private static async Task TestNextWorkingDay()
        {
            Console.WriteLine("测试下一个工作日计算功能(异步):");
            
            // 从周五开始，计算下一个工作日（应该是下周一）
            DateTime friday = new DateTime(2023, 7, 7); // 2023年7月7日是周五
            DateTime nextWorkDayAfterFriday = await HolidayService.GetNextWorkingDayAsync(friday);
            Console.WriteLine($"2023年7月7日(周五)之后的下一个工作日是: {nextWorkDayAfterFriday:yyyy年MM月dd日}");
            
            // 从工作日开始，计算下一个工作日
            DateTime wednesday = new DateTime(2023, 7, 5); // 2023年7月5日是周三
            DateTime nextWorkDayAfterWednesday = await HolidayService.GetNextWorkingDayAsync(wednesday);
            Console.WriteLine($"2023年7月5日(周三)之后的下一个工作日是: {nextWorkDayAfterWednesday:yyyy年MM月dd日}");
            
            // 计算多个工作日之后的日期
            DateTime monday = new DateTime(2023, 7, 3); // 2023年7月3日是周一
            DateTime fiveWorkDaysAfterMonday = await HolidayService.GetNextWorkingDayAsync(monday, 5);
            Console.WriteLine($"2023年7月3日(周一)之后的第5个工作日是: {fiveWorkDaysAfterMonday:yyyy年MM月dd日}");
        }

        /// <summary>
        /// 测试同步版本的下一个工作日计算功能
        /// </summary>
        private static void TestSyncNextWorkingDay()
        {
            Console.WriteLine("测试下一个工作日计算功能(同步):");
            
            // 从周五开始，计算下一个工作日（应该是下周一，因为同步版本只跳过周末）
            DateTime friday = new DateTime(2023, 7, 7); // 2023年7月7日是周五
            DateTime nextWorkDayAfterFriday = HolidayService.GetNextWorkingDay(friday);
            Console.WriteLine($"2023年7月7日(周五)之后的下一个工作日是: {nextWorkDayAfterFriday:yyyy年MM月dd日}");
            
            // 从工作日开始，计算下一个工作日
            DateTime wednesday = new DateTime(2023, 7, 5); // 2023年7月5日是周三
            DateTime nextWorkDayAfterWednesday = HolidayService.GetNextWorkingDay(wednesday);
            Console.WriteLine($"2023年7月5日(周三)之后的下一个工作日是: {nextWorkDayAfterWednesday:yyyy年MM月dd日}");
            
            // 计算多个工作日之后的日期
            DateTime monday = new DateTime(2023, 7, 3); // 2023年7月3日是周一
            DateTime fiveWorkDaysAfterMonday = HolidayService.GetNextWorkingDay(monday, 5);
            Console.WriteLine($"2023年7月3日(周一)之后的第5个工作日是: {fiveWorkDaysAfterMonday:yyyy年MM月dd日}");
        }
    }
}