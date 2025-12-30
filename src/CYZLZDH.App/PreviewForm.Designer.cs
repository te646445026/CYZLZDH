using System;
using System.Drawing;
using System.Windows.Forms;

namespace CYZLZDH.App
{
    partial class PreviewForm
    {
        private System.ComponentModel.IContainer components = null;
        private TabControl tabControl;
        private TabPage originalTab;
        private TabPage reportTab;
        private RichTextBox rtbOriginal;
        private RichTextBox rtbReport;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            this.Text = "文档预览";
            this.StartPosition = FormStartPosition.CenterParent;
            this.WindowState = FormWindowState.Maximized;
            this.Size = new Size(1200, 800);

            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;

            originalTab = new TabPage();
            originalTab.Text = "原始记录";
            rtbOriginal = new RichTextBox();
            rtbOriginal.Dock = DockStyle.Fill;
            rtbOriginal.ReadOnly = true;
            rtbOriginal.Font = new Font("宋体", 12);
            originalTab.Controls.Add(rtbOriginal);

            reportTab = new TabPage();
            reportTab.Text = "测试报告";
            rtbReport = new RichTextBox();
            rtbReport.Dock = DockStyle.Fill;
            rtbReport.ReadOnly = true;
            rtbReport.Font = new Font("宋体", 12);
            reportTab.Controls.Add(rtbReport);

            tabControl.TabPages.Add(originalTab);
            tabControl.TabPages.Add(reportTab);

            this.Controls.Add(tabControl);
        }
    }
}
