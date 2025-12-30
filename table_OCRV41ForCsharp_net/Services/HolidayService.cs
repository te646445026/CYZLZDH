using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace table_OCRV41ForCsharp_net.Services
{
    public class HolidayService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string ApiUrl = "http://timor.tech/api/holiday/info/";

        /// <summary>
        /// 检查指定日期是否为节假日或周末
        /// </summary>
        /// <param name="date">要检查的日期</param>
        /// <returns>如果是节假日或周末返回true，否则返回false</returns>
        public static async Task<bool> IsHolidayOrWeekendAsync(DateTime date)
        {
            try
            {
                // 首先检查是否为周末（周六或周日）
                if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                {
                    return true;
                }

                // 调用API检查是否为法定节假日
                string formattedDate = date.ToString("yyyy-MM-dd");
                string requestUrl = $"{HolidayService.ApiUrl}{formattedDate}";

                HttpResponseMessage response = await _httpClient.GetAsync(requestUrl);
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                    
                    // 解析API返回的JSON数据
                    JsonElement root = doc.RootElement;
                    if (root.TryGetProperty("holiday", out JsonElement holiday) && !holiday.ValueKind.Equals(JsonValueKind.Null))
                    {
                        // 如果holiday字段存在且不为null，则为节假日
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检查节假日时发生错误: {ex.Message}");
                // 发生错误时，默认为非节假日
                return false;
            }
        }

        /// <summary>
        /// 获取下一个工作日（非节假日且非周末）
        /// </summary>
        /// <param name="startDate">起始日期</param>
        /// <param name="daysToAdd">要添加的工作日天数</param>
        /// <returns>下一个工作日</returns>
        public static async Task<DateTime> GetNextWorkingDayAsync(DateTime startDate, int daysToAdd = 1)
        {
            DateTime nextDate = startDate;
            int workDaysAdded = 0;

            while (workDaysAdded < daysToAdd)
            {
                nextDate = nextDate.AddDays(1);
                
                // 检查是否为节假日或周末
                bool isHoliday = await IsHolidayOrWeekendAsync(nextDate);
                
                if (!isHoliday)
                {
                    workDaysAdded++;
                }
            }

            return nextDate;
        }

        /// <summary>
        /// 获取下一个工作日的同步版本（用于不支持异步的场景）
        /// </summary>
        /// <param name="startDate">起始日期</param>
        /// <param name="daysToAdd">要添加的工作日天数</param>
        /// <returns>下一个工作日</returns>
        public static DateTime GetNextWorkingDay(DateTime startDate, int daysToAdd = 1)
        {
            // 如果无法使用API，至少跳过周末
            DateTime nextDate = startDate;
            int workDaysAdded = 0;

            while (workDaysAdded < daysToAdd)
            {
                nextDate = nextDate.AddDays(1);
                
                // 只检查是否为周末
                if (nextDate.DayOfWeek != DayOfWeek.Saturday && nextDate.DayOfWeek != DayOfWeek.Sunday)
                {
                    workDaysAdded++;
                }
            }

            return nextDate;
        }
    }
}