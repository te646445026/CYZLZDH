using System;
using System.Drawing;
using System.Windows.Forms;

namespace CYZLZDH.App
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private TableLayoutPanel mainLayout;
        private GroupBox docManagementGroup;
        private TableLayoutPanel docLayout;
        private Label lblOriginal;
        private TextBox txtOriginalDoc;
        private Button btnOriginalDoc;
        private Label lblReport;
        private TextBox txtReportDoc;
        private Button btnReportDoc;
        private GroupBox imageGroup;
        private TableLayoutPanel imageLayout;
        private Button btnSelectImage;
        private Label lblCurrentImage;
        private Button btnRecognize;
        private Label spacer;
        private GroupBox resultGroup;
        private TextBox txtOcrResult;
        private GroupBox mappingGroup;
        private TableLayoutPanel mappingLayout;
        private DataGridView gridMapping;
        private DataGridViewTextBoxColumn colMarker;
        private DataGridViewTextBoxColumn colFieldName;
        private DataGridViewTextBoxColumn colValue;
        private FlowLayoutPanel buttonPanel;
        private Button btnProcess;
        private Button btnClear;
        private Button btnViewLogs;
        private Panel statusPanel;
        private Label lblStatus;
        private Panel headerPanel;
        private Label lblTitle;

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
            this.mainLayout = new System.Windows.Forms.TableLayoutPanel();
            this.headerPanel = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.docManagementGroup = new System.Windows.Forms.GroupBox();
            this.docLayout = new System.Windows.Forms.TableLayoutPanel();
            this.lblOriginal = new System.Windows.Forms.Label();
            this.txtOriginalDoc = new System.Windows.Forms.TextBox();
            this.btnOriginalDoc = new System.Windows.Forms.Button();
            this.lblReport = new System.Windows.Forms.Label();
            this.txtReportDoc = new System.Windows.Forms.TextBox();
            this.btnReportDoc = new System.Windows.Forms.Button();
            this.imageGroup = new System.Windows.Forms.GroupBox();
            this.imageLayout = new System.Windows.Forms.TableLayoutPanel();
            this.btnSelectImage = new System.Windows.Forms.Button();
            this.lblCurrentImage = new System.Windows.Forms.Label();
            this.btnRecognize = new System.Windows.Forms.Button();
            this.spacer = new System.Windows.Forms.Label();
            this.resultGroup = new System.Windows.Forms.GroupBox();
            this.txtOcrResult = new System.Windows.Forms.TextBox();
            this.mappingGroup = new System.Windows.Forms.GroupBox();
            this.mappingLayout = new System.Windows.Forms.TableLayoutPanel();
            this.gridMapping = new System.Windows.Forms.DataGridView();
            this.colMarker = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colFieldName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.buttonPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.btnProcess = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.btnViewLogs = new System.Windows.Forms.Button();
            this.statusPanel = new System.Windows.Forms.Panel();
            this.lblStatus = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.gridMapping)).BeginInit();
            this.mainLayout.SuspendLayout();
            this.headerPanel.SuspendLayout();
            this.docManagementGroup.SuspendLayout();
            this.docLayout.SuspendLayout();
            this.imageGroup.SuspendLayout();
            this.imageLayout.SuspendLayout();
            this.resultGroup.SuspendLayout();
            this.mappingGroup.SuspendLayout();
            this.mappingLayout.SuspendLayout();
            this.buttonPanel.SuspendLayout();
            this.statusPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainLayout
            // 
            this.mainLayout.ColumnCount = 1;
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.Controls.Add(this.headerPanel, 0, 0);
            this.mainLayout.Controls.Add(this.docManagementGroup, 0, 1);
            this.mainLayout.Controls.Add(this.imageGroup, 0, 2);
            this.mainLayout.Controls.Add(this.resultGroup, 0, 3);
            this.mainLayout.Controls.Add(this.mappingGroup, 0, 4);
            this.mainLayout.Controls.Add(this.statusPanel, 0, 5);
            this.mainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainLayout.Location = new System.Drawing.Point(0, 0);
            this.mainLayout.Name = "mainLayout";
            this.mainLayout.Padding = new System.Windows.Forms.Padding(10);
            this.mainLayout.RowCount = 6;
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 95F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 75F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 180F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.mainLayout.Size = new System.Drawing.Size(1000, 700);
            this.mainLayout.TabIndex = 0;
            // 
            // headerPanel
            // 
            this.headerPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
            this.headerPanel.Controls.Add(this.lblTitle);
            this.headerPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.headerPanel.Location = new System.Drawing.Point(18, 18);
            this.headerPanel.Name = "headerPanel";
            this.headerPanel.Size = new System.Drawing.Size(248, 54);
            this.headerPanel.TabIndex = 0;
            // 
            // lblTitle
            // 
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTitle.Font = new System.Drawing.Font("微软雅黑", 14F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(0, 0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(248, 54);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "电梯乘运质量文档处理系统";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // docManagementGroup
            // 
            this.docManagementGroup.BackColor = System.Drawing.Color.White;
            this.docManagementGroup.Controls.Add(this.docLayout);
            this.docManagementGroup.Dock = System.Windows.Forms.DockStyle.Fill;
            this.docManagementGroup.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold);
            this.docManagementGroup.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
            this.docManagementGroup.Location = new System.Drawing.Point(10, 60);
            this.docManagementGroup.Name = "docManagementGroup";
            this.docManagementGroup.Padding = new System.Windows.Forms.Padding(10);
            this.docManagementGroup.Size = new System.Drawing.Size(900, 95);
            this.docManagementGroup.TabIndex = 1;
            this.docManagementGroup.TabStop = false;
            this.docManagementGroup.Text = "文档管理";
            // 
            // docLayout
            // 
            this.docLayout.ColumnCount = 3;
            this.docLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.docLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.docLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.docLayout.Controls.Add(this.lblOriginal, 0, 0);
            this.docLayout.Controls.Add(this.txtOriginalDoc, 1, 0);
            this.docLayout.Controls.Add(this.btnOriginalDoc, 2, 0);
            this.docLayout.Controls.Add(this.lblReport, 0, 1);
            this.docLayout.Controls.Add(this.txtReportDoc, 1, 1);
            this.docLayout.Controls.Add(this.btnReportDoc, 2, 1);
            this.docLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.docLayout.Location = new System.Drawing.Point(10, 24);
            this.docLayout.Name = "docLayout";
            this.docLayout.RowCount = 2;
            this.docLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.docLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.docLayout.Size = new System.Drawing.Size(880, 61);
            this.docLayout.TabIndex = 0;
            // 
            // lblOriginal
            // 
            this.lblOriginal.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblOriginal.AutoSize = true;
            this.lblOriginal.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.lblOriginal.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
            this.lblOriginal.Location = new System.Drawing.Point(28, 10);
            this.lblOriginal.Name = "lblOriginal";
            this.lblOriginal.Size = new System.Drawing.Size(59, 17);
            this.lblOriginal.TabIndex = 0;
            this.lblOriginal.Text = "原始记录:";
            // 
            // txtOriginalDoc
            // 
            this.txtOriginalDoc.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtOriginalDoc.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.txtOriginalDoc.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtOriginalDoc.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.txtOriginalDoc.Location = new System.Drawing.Point(95, 7);
            this.txtOriginalDoc.Name = "txtOriginalDoc";
            this.txtOriginalDoc.ReadOnly = true;
            this.txtOriginalDoc.Size = new System.Drawing.Size(36, 23);
            this.txtOriginalDoc.TabIndex = 1;
            // 
            // btnOriginalDoc
            // 
            this.btnOriginalDoc.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.btnOriginalDoc.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(152)))), ((int)(((byte)(219)))));
            this.btnOriginalDoc.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnOriginalDoc.FlatAppearance.BorderSize = 0;
            this.btnOriginalDoc.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOriginalDoc.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.btnOriginalDoc.ForeColor = System.Drawing.Color.White;
            this.btnOriginalDoc.Location = new System.Drawing.Point(139, 4);
            this.btnOriginalDoc.Name = "btnOriginalDoc";
            this.btnOriginalDoc.Size = new System.Drawing.Size(80, 29);
            this.btnOriginalDoc.TabIndex = 2;
            this.btnOriginalDoc.Text = "浏览...";
            this.btnOriginalDoc.UseVisualStyleBackColor = false;
            this.btnOriginalDoc.Click += new System.EventHandler(this.BtnOriginalDoc_Click);
            // 
            // lblReport
            // 
            this.lblReport.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.lblReport.AutoSize = true;
            this.lblReport.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.lblReport.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
            this.lblReport.Location = new System.Drawing.Point(28, 47);
            this.lblReport.Name = "lblReport";
            this.lblReport.Size = new System.Drawing.Size(59, 17);
            this.lblReport.TabIndex = 3;
            this.lblReport.Text = "测试报告:";
            // 
            // txtReportDoc
            // 
            this.txtReportDoc.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtReportDoc.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.txtReportDoc.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtReportDoc.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.txtReportDoc.Location = new System.Drawing.Point(95, 44);
            this.txtReportDoc.Name = "txtReportDoc";
            this.txtReportDoc.ReadOnly = true;
            this.txtReportDoc.Size = new System.Drawing.Size(36, 23);
            this.txtReportDoc.TabIndex = 4;
            // 
            // btnReportDoc
            // 
            this.btnReportDoc.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.btnReportDoc.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(152)))), ((int)(((byte)(219)))));
            this.btnReportDoc.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnReportDoc.FlatAppearance.BorderSize = 0;
            this.btnReportDoc.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReportDoc.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.btnReportDoc.ForeColor = System.Drawing.Color.White;
            this.btnReportDoc.Location = new System.Drawing.Point(139, 41);
            this.btnReportDoc.Name = "btnReportDoc";
            this.btnReportDoc.Size = new System.Drawing.Size(80, 29);
            this.btnReportDoc.TabIndex = 5;
            this.btnReportDoc.Text = "浏览...";
            this.btnReportDoc.UseVisualStyleBackColor = false;
            this.btnReportDoc.Click += new System.EventHandler(this.BtnReportDoc_Click);
            // 
            // imageGroup
            // 
            this.imageGroup.BackColor = System.Drawing.Color.White;
            this.imageGroup.Controls.Add(this.imageLayout);
            this.imageGroup.Dock = System.Windows.Forms.DockStyle.Fill;
            this.imageGroup.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold);
            this.imageGroup.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
            this.imageGroup.Location = new System.Drawing.Point(10, 165);
            this.imageGroup.Name = "imageGroup";
            this.imageGroup.Padding = new System.Windows.Forms.Padding(10);
            this.imageGroup.Size = new System.Drawing.Size(900, 75);
            this.imageGroup.TabIndex = 2;
            this.imageGroup.TabStop = false;
            this.imageGroup.Text = "图片处理";
            // 
            // imageLayout
            // 
            this.imageLayout.ColumnCount = 4;
            this.imageLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.imageLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.imageLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.imageLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.imageLayout.Controls.Add(this.btnSelectImage, 0, 0);
            this.imageLayout.Controls.Add(this.lblCurrentImage, 1, 0);
            this.imageLayout.Controls.Add(this.btnRecognize, 2, 0);
            this.imageLayout.Controls.Add(this.spacer, 3, 0);
            this.imageLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.imageLayout.Location = new System.Drawing.Point(10, 24);
            this.imageLayout.Name = "imageLayout";
            this.imageLayout.RowCount = 1;
            this.imageLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            this.imageLayout.Size = new System.Drawing.Size(880, 41);
            this.imageLayout.TabIndex = 0;
            // 
            // btnSelectImage
            // 
            this.btnSelectImage.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(204)))), ((int)(((byte)(113)))));
            this.btnSelectImage.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSelectImage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSelectImage.FlatAppearance.BorderSize = 0;
            this.btnSelectImage.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSelectImage.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.btnSelectImage.ForeColor = System.Drawing.Color.White;
            this.btnSelectImage.Location = new System.Drawing.Point(3, 3);
            this.btnSelectImage.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
            this.btnSelectImage.Name = "btnSelectImage";
            this.btnSelectImage.Size = new System.Drawing.Size(94, 29);
            this.btnSelectImage.TabIndex = 0;
            this.btnSelectImage.Text = "选择图片";
            this.btnSelectImage.UseVisualStyleBackColor = false;
            this.btnSelectImage.Click += new System.EventHandler(this.BtnSelectImage_Click);
            // 
            // lblCurrentImage
            // 
            this.lblCurrentImage.AutoSize = false;
            this.lblCurrentImage.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.lblCurrentImage.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
            this.lblCurrentImage.Location = new System.Drawing.Point(100, 8);
            this.lblCurrentImage.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.lblCurrentImage.Name = "lblCurrentImage";
            this.lblCurrentImage.Size = new System.Drawing.Size(600, 23);
            this.lblCurrentImage.TabIndex = 1;
            this.lblCurrentImage.Text = "当前图片: 未选择";
            this.lblCurrentImage.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnRecognize
            // 
            this.btnRecognize.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(126)))), ((int)(((byte)(34)))));
            this.btnRecognize.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRecognize.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRecognize.Enabled = false;
            this.btnRecognize.FlatAppearance.BorderSize = 0;
            this.btnRecognize.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRecognize.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.btnRecognize.ForeColor = System.Drawing.Color.White;
            this.btnRecognize.Location = new System.Drawing.Point(706, 3);
            this.btnRecognize.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
            this.btnRecognize.Name = "btnRecognize";
            this.btnRecognize.Size = new System.Drawing.Size(94, 29);
            this.btnRecognize.TabIndex = 2;
            this.btnRecognize.Text = "开始识别";
            this.btnRecognize.UseVisualStyleBackColor = false;
            this.btnRecognize.Click += new System.EventHandler(this.BtnRecognize_Click);
            // 
            // spacer
            // 
            this.spacer.Location = new System.Drawing.Point(87, 0);
            this.spacer.Name = "spacer";
            this.spacer.Size = new System.Drawing.Size(100, 20);
            this.spacer.TabIndex = 3;
            // 
            // resultGroup
            // 
            this.resultGroup.BackColor = System.Drawing.Color.White;
            this.resultGroup.Controls.Add(this.txtOcrResult);
            this.resultGroup.Dock = System.Windows.Forms.DockStyle.Fill;
            this.resultGroup.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold);
            this.resultGroup.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
            this.resultGroup.Location = new System.Drawing.Point(10, 250);
            this.resultGroup.Name = "resultGroup";
            this.resultGroup.Padding = new System.Windows.Forms.Padding(10);
            this.resultGroup.Size = new System.Drawing.Size(900, 250);
            this.resultGroup.TabIndex = 3;
            this.resultGroup.TabStop = false;
            this.resultGroup.Text = "识别结果";
            // 
            // txtOcrResult
            // 
            this.txtOcrResult.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.txtOcrResult.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtOcrResult.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtOcrResult.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.txtOcrResult.Location = new System.Drawing.Point(10, 24);
            this.txtOcrResult.Multiline = true;
            this.txtOcrResult.Name = "txtOcrResult";
            this.txtOcrResult.ReadOnly = true;
            this.txtOcrResult.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtOcrResult.Size = new System.Drawing.Size(880, 216);
            this.txtOcrResult.TabIndex = 0;
            // 
            // mappingGroup
            // 
            this.mappingGroup.BackColor = System.Drawing.Color.White;
            this.mappingGroup.Controls.Add(this.mappingLayout);
            this.mappingGroup.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mappingGroup.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold);
            this.mappingGroup.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
            this.mappingGroup.Location = new System.Drawing.Point(10, 510);
            this.mappingGroup.Name = "mappingGroup";
            this.mappingGroup.Padding = new System.Windows.Forms.Padding(10);
            this.mappingGroup.Size = new System.Drawing.Size(900, 180);
            this.mappingGroup.TabIndex = 4;
            this.mappingGroup.TabStop = false;
            this.mappingGroup.Text = "标记符映射配置";
            // 
            // mappingLayout
            // 
            this.mappingLayout.ColumnCount = 2;
            this.mappingLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mappingLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 170F));
            this.mappingLayout.Controls.Add(this.gridMapping, 0, 0);
            this.mappingLayout.Controls.Add(this.buttonPanel, 1, 0);
            this.mappingLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mappingLayout.Location = new System.Drawing.Point(10, 24);
            this.mappingLayout.Name = "mappingLayout";
            this.mappingLayout.RowCount = 1;
            this.mappingLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mappingLayout.Size = new System.Drawing.Size(880, 146);
            this.mappingLayout.TabIndex = 0;
            // 
            // gridMapping
            // 
            this.gridMapping.AllowUserToAddRows = false;
            this.gridMapping.AllowUserToDeleteRows = false;
            this.gridMapping.BackgroundColor = System.Drawing.Color.White;
            this.gridMapping.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.gridMapping.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridMapping.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colMarker,
            this.colFieldName,
            this.colValue});
            this.gridMapping.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridMapping.Location = new System.Drawing.Point(3, 3);
            this.gridMapping.Name = "gridMapping";
            this.gridMapping.RowHeadersVisible = false;
            this.gridMapping.RowTemplate.Height = 20;
            this.gridMapping.Size = new System.Drawing.Size(705, 140);
            this.gridMapping.TabIndex = 0;
            // 
            // colMarker
            // 
            this.colMarker.HeaderText = "标记";
            this.colMarker.Name = "colMarker";
            this.colMarker.ReadOnly = true;
            this.colMarker.Width = 50;
            // 
            // colFieldName
            // 
            this.colFieldName.HeaderText = "字段名称";
            this.colFieldName.Name = "colFieldName";
            this.colFieldName.ReadOnly = true;
            this.colFieldName.Width = 100;
            // 
            // colValue
            // 
            this.colValue.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colValue.HeaderText = "识别值";
            this.colValue.Name = "colValue";
            // 
            // buttonPanel
            // 
            this.buttonPanel.Controls.Add(this.btnProcess);
            this.buttonPanel.Controls.Add(this.btnClear);
            this.buttonPanel.Controls.Add(this.btnViewLogs);
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.buttonPanel.Location = new System.Drawing.Point(708, 3);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.Padding = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.buttonPanel.Size = new System.Drawing.Size(170, 140);
            this.buttonPanel.TabIndex = 1;
            // 
            // btnProcess
            // 
            this.btnProcess.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(152)))), ((int)(((byte)(219)))));
            this.btnProcess.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnProcess.FlatAppearance.BorderSize = 0;
            this.btnProcess.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnProcess.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.btnProcess.ForeColor = System.Drawing.Color.White;
            this.btnProcess.Location = new System.Drawing.Point(3, 5);
            this.btnProcess.Margin = new System.Windows.Forms.Padding(0, 0, 0, 5);
            this.btnProcess.Name = "btnProcess";
            this.btnProcess.Size = new System.Drawing.Size(164, 35);
            this.btnProcess.TabIndex = 1;
            this.btnProcess.Text = "填入并保存";
            this.btnProcess.UseVisualStyleBackColor = false;
            this.btnProcess.Click += new System.EventHandler(this.BtnProcess_Click);
            // 
            // btnClear
            // 
            this.btnClear.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(231)))), ((int)(((byte)(76)))), ((int)(((byte)(60)))));
            this.btnClear.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnClear.FlatAppearance.BorderSize = 0;
            this.btnClear.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClear.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.btnClear.ForeColor = System.Drawing.Color.White;
            this.btnClear.Location = new System.Drawing.Point(3, 45);
            this.btnClear.Margin = new System.Windows.Forms.Padding(0, 0, 0, 5);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(164, 35);
            this.btnClear.TabIndex = 3;
            this.btnClear.Text = "全部清空";
            this.btnClear.UseVisualStyleBackColor = false;
            this.btnClear.Click += new System.EventHandler(this.BtnClear_Click);
            // 
            // btnViewLogs
            // 
            this.btnViewLogs.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.btnViewLogs.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnViewLogs.FlatAppearance.BorderSize = 0;
            this.btnViewLogs.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnViewLogs.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.btnViewLogs.ForeColor = System.Drawing.Color.White;
            this.btnViewLogs.Location = new System.Drawing.Point(3, 85);
            this.btnViewLogs.Margin = new System.Windows.Forms.Padding(0, 0, 0, 5);
            this.btnViewLogs.Name = "btnViewLogs";
            this.btnViewLogs.Size = new System.Drawing.Size(164, 35);
            this.btnViewLogs.TabIndex = 4;
            this.btnViewLogs.Text = "查看日志";
            this.btnViewLogs.UseVisualStyleBackColor = false;
            this.btnViewLogs.Click += new System.EventHandler(this.BtnViewLogs_Click);
            // 
            // statusPanel
            // 
            this.statusPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(236)))), ((int)(((byte)(240)))), ((int)(((byte)(241)))));
            this.statusPanel.Controls.Add(this.lblStatus);
            this.statusPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.statusPanel.Location = new System.Drawing.Point(10, 700);
            this.statusPanel.Name = "statusPanel";
            this.statusPanel.Padding = new System.Windows.Forms.Padding(10, 5, 10, 5);
            this.statusPanel.Size = new System.Drawing.Size(900, 30);
            this.statusPanel.TabIndex = 5;
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.lblStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
            this.lblStatus.Location = new System.Drawing.Point(0, 0);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(32, 17);
            this.lblStatus.TabIndex = 0;
            this.lblStatus.Text = "就绪";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(247)))), ((int)(((byte)(250)))));
            this.ClientSize = new System.Drawing.Size(1020, 760);
            this.Controls.Add(this.mainLayout);
            this.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "电梯乘运质量文档处理系统";
            this.MinimumSize = new System.Drawing.Size(1020, 760);
            this.mainLayout.ResumeLayout(false);
            this.headerPanel.ResumeLayout(false);
            this.docManagementGroup.ResumeLayout(false);
            this.docLayout.ResumeLayout(false);
            this.docLayout.PerformLayout();
            this.imageGroup.ResumeLayout(false);
            this.imageLayout.ResumeLayout(false);
            this.imageLayout.PerformLayout();
            this.resultGroup.ResumeLayout(false);
            this.resultGroup.PerformLayout();
            this.mappingGroup.ResumeLayout(false);
            this.mappingLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridMapping)).EndInit();
            this.buttonPanel.ResumeLayout(false);
            this.statusPanel.ResumeLayout(false);
            this.statusPanel.PerformLayout();
            this.ResumeLayout(false);

        }
    }
}
