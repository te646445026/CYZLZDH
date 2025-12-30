namespace CYZLZDH.Core.Models;

public class OcrResult
{
    public string DeviceCode { get; set; } = string.Empty;      // 1. 设备代码
    public string Model { get; set; } = string.Empty;           // 2. 型号
    public string SerialNum { get; set; } = string.Empty;       // 3. 编号
    public string ManufacturingUnit { get; set; } = string.Empty; // 4. 制造单位名称
    public string UserName { get; set; } = string.Empty;        // 5. 使用单位名称
    public string UsingAddress { get; set; } = string.Empty;    // 6. 安装地点
    public string MaintenanceUnit { get; set; } = string.Empty; // 7. 维护保养单位名称
    public string Speed { get; set; } = string.Empty;           // 8. 额定速度
    public string RatedLoad { get; set; } = string.Empty;       // 9. 额定载重量
    public string ReportNum { get; set; } = string.Empty;       // 10. 报告编号
    public string ConstructionUnit { get; set; } = string.Empty; // 施工单位/委托单位
    public string Date { get; set; } = string.Empty;            // 11. 检验日期
    public string JianyanOrjiance { get; set; } = string.Empty; // 12. 检验类型
    public string LayerStationDoor { get; set; } = string.Empty; // 层站门数
    public string DeviceType { get; set; } = string.Empty;       // 设备品种
}