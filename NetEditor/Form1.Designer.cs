using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using System.Reflection;
using System.IO;
using System.Windows.Forms;


namespace NetEditor
{
    partial class Form1
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.txt_Status = new System.Windows.Forms.TextBox();
            this.grp_Changes = new System.Windows.Forms.GroupBox();
            this.radio151 = new System.Windows.Forms.RadioButton();
            this.radio160 = new System.Windows.Forms.RadioButton();
            this.btn_Disconnect = new System.Windows.Forms.Button();
            this.btn_Connect = new System.Windows.Forms.Button();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.deviceTable = new System.Windows.Forms.DataGridView();
            this.dgv_Id = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgv_Mode = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgv_DeviceName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgv_autoGeneratePnName = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.dgv_PnDeviceName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgv_PnNumber = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgv_IpAddress = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgv_Mask = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgv_RouterAddress = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgv_PnSubnet = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgv_IoSystem = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btn_CommitChanges = new System.Windows.Forms.Button();
            this.btn_ClearChanges = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.txt_Search = new System.Windows.Forms.TextBox();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.btn_ExportToCSV = new System.Windows.Forms.Button();
            this.txt_StatusBar = new System.Windows.Forms.Label();
            this.grp_Changes.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.deviceTable)).BeginInit();
            this.tableLayoutPanel2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // txt_Status
            // 
            this.txt_Status.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txt_Status.BackColor = System.Drawing.SystemColors.Menu;
            this.txt_Status.Location = new System.Drawing.Point(178, 376);
            this.txt_Status.Margin = new System.Windows.Forms.Padding(6, 5, 6, 6);
            this.txt_Status.MinimumSize = new System.Drawing.Size(4, 90);
            this.txt_Status.Multiline = true;
            this.txt_Status.Name = "txt_Status";
            this.txt_Status.ReadOnly = true;
            this.txt_Status.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txt_Status.Size = new System.Drawing.Size(919, 112);
            this.txt_Status.TabIndex = 117;
            this.txt_Status.Text = "Status box\r\n";
            // 
            // grp_Changes
            // 
            this.grp_Changes.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grp_Changes.Controls.Add(this.radio151);
            this.grp_Changes.Controls.Add(this.radio160);
            this.grp_Changes.Controls.Add(this.btn_Disconnect);
            this.grp_Changes.Controls.Add(this.btn_Connect);
            this.grp_Changes.Location = new System.Drawing.Point(6, 5);
            this.grp_Changes.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.grp_Changes.MaximumSize = new System.Drawing.Size(160, 180);
            this.grp_Changes.MinimumSize = new System.Drawing.Size(159, 126);
            this.grp_Changes.Name = "grp_Changes";
            this.grp_Changes.Padding = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.grp_Changes.Size = new System.Drawing.Size(160, 126);
            this.grp_Changes.TabIndex = 18;
            this.grp_Changes.TabStop = false;
            this.grp_Changes.Text = "TIA Portal Project";
            // 
            // radio151
            // 
            this.radio151.AutoSize = true;
            this.radio151.Location = new System.Drawing.Point(12, 148);
            this.radio151.Name = "radio151";
            this.radio151.Size = new System.Drawing.Size(121, 20);
            this.radio151.TabIndex = 20;
            this.radio151.Text = "TIA Portal v15.1";
            this.radio151.UseVisualStyleBackColor = true;
            this.radio151.Visible = false;
            // 
            // radio160
            // 
            this.radio160.AutoSize = true;
            this.radio160.Checked = true;
            this.radio160.Location = new System.Drawing.Point(12, 122);
            this.radio160.Name = "radio160";
            this.radio160.Size = new System.Drawing.Size(122, 20);
            this.radio160.TabIndex = 19;
            this.radio160.TabStop = true;
            this.radio160.Text = "TIA Portal v16.0";
            this.radio160.UseVisualStyleBackColor = true;
            this.radio160.Visible = false;
            // 
            // btn_Disconnect
            // 
            this.btn_Disconnect.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_Disconnect.AutoSize = true;
            this.btn_Disconnect.Enabled = false;
            this.btn_Disconnect.Location = new System.Drawing.Point(12, 74);
            this.btn_Disconnect.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.btn_Disconnect.Name = "btn_Disconnect";
            this.btn_Disconnect.Size = new System.Drawing.Size(136, 40);
            this.btn_Disconnect.TabIndex = 18;
            this.btn_Disconnect.Text = "Disconnect";
            this.btn_Disconnect.UseVisualStyleBackColor = true;
            this.btn_Disconnect.Click += new System.EventHandler(this.DisconnectFromProject);
            // 
            // btn_Connect
            // 
            this.btn_Connect.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_Connect.AutoSize = true;
            this.btn_Connect.BackColor = System.Drawing.Color.Transparent;
            this.btn_Connect.Location = new System.Drawing.Point(12, 27);
            this.btn_Connect.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.btn_Connect.Name = "btn_Connect";
            this.btn_Connect.Size = new System.Drawing.Size(136, 40);
            this.btn_Connect.TabIndex = 5;
            this.btn_Connect.Text = "Connect";
            this.btn_Connect.UseVisualStyleBackColor = false;
            this.btn_Connect.Click += new System.EventHandler(this.btn_ConnectTIA);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 172F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.deviceTable, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.progressBar, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.btn_ExportToCSV, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.txt_StatusBar, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.txt_Status, 1, 1);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(10, 10);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 75F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1103, 515);
            this.tableLayoutPanel1.TabIndex = 119;
            // 
            // deviceTable
            // 
            this.deviceTable.AllowUserToAddRows = false;
            this.deviceTable.AllowUserToDeleteRows = false;
            this.deviceTable.AllowUserToOrderColumns = true;
            this.deviceTable.AllowUserToResizeRows = false;
            this.deviceTable.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.deviceTable.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.deviceTable.BackgroundColor = System.Drawing.SystemColors.ControlLight;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.ControlLight;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Siemens Sans", 8F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.deviceTable.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.deviceTable.ColumnHeadersHeight = 29;
            this.deviceTable.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.deviceTable.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dgv_Id,
            this.dgv_Mode,
            this.dgv_DeviceName,
            this.dgv_autoGeneratePnName,
            this.dgv_PnDeviceName,
            this.dgv_PnNumber,
            this.dgv_IpAddress,
            this.dgv_Mask,
            this.dgv_RouterAddress,
            this.dgv_PnSubnet,
            this.dgv_IoSystem});
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle4.Font = new System.Drawing.Font("Siemens Sans", 8F);
            dataGridViewCellStyle4.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle4.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.deviceTable.DefaultCellStyle = dataGridViewCellStyle4;
            this.deviceTable.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            this.deviceTable.GridColor = System.Drawing.SystemColors.ControlLightLight;
            this.deviceTable.Location = new System.Drawing.Point(178, 5);
            this.deviceTable.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.deviceTable.MultiSelect = false;
            this.deviceTable.Name = "deviceTable";
            this.deviceTable.RowHeadersVisible = false;
            this.deviceTable.RowHeadersWidth = 11;
            this.deviceTable.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this.deviceTable.RowTemplate.Height = 24;
            this.deviceTable.Size = new System.Drawing.Size(919, 361);
            this.deviceTable.TabIndex = 118;
            this.deviceTable.CellMouseUp += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.DeviceTable_OnCellMouseUp);
            this.deviceTable.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.deviceTable_CellEndEdit);
            this.deviceTable.ColumnHeaderMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.DeviceTable_Sort);
            // 
            // dgv_Id
            // 
            this.dgv_Id.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.ControlLight;
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.GrayText;
            this.dgv_Id.DefaultCellStyle = dataGridViewCellStyle2;
            this.dgv_Id.HeaderText = "Id";
            this.dgv_Id.MinimumWidth = 6;
            this.dgv_Id.Name = "dgv_Id";
            this.dgv_Id.ReadOnly = true;
            this.dgv_Id.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.dgv_Id.Width = 50;
            // 
            // dgv_Mode
            // 
            this.dgv_Mode.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.ControlLight;
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.GrayText;
            this.dgv_Mode.DefaultCellStyle = dataGridViewCellStyle3;
            this.dgv_Mode.FillWeight = 80F;
            this.dgv_Mode.HeaderText = "Mode";
            this.dgv_Mode.MinimumWidth = 6;
            this.dgv_Mode.Name = "dgv_Mode";
            this.dgv_Mode.ReadOnly = true;
            this.dgv_Mode.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.dgv_Mode.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.dgv_Mode.Width = 110;
            // 
            // dgv_DeviceName
            // 
            this.dgv_DeviceName.FillWeight = 120F;
            this.dgv_DeviceName.HeaderText = "Name";
            this.dgv_DeviceName.MinimumWidth = 100;
            this.dgv_DeviceName.Name = "dgv_DeviceName";
            this.dgv_DeviceName.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
            // 
            // dgv_autoGeneratePnName
            // 
            this.dgv_autoGeneratePnName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.dgv_autoGeneratePnName.FillWeight = 50F;
            this.dgv_autoGeneratePnName.HeaderText = "Auto";
            this.dgv_autoGeneratePnName.MinimumWidth = 6;
            this.dgv_autoGeneratePnName.Name = "dgv_autoGeneratePnName";
            this.dgv_autoGeneratePnName.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.dgv_autoGeneratePnName.Width = 56;
            // 
            // dgv_PnDeviceName
            // 
            this.dgv_PnDeviceName.HeaderText = "Profinet Name";
            this.dgv_PnDeviceName.MinimumWidth = 70;
            this.dgv_PnDeviceName.Name = "dgv_PnDeviceName";
            this.dgv_PnDeviceName.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // dgv_PnNumber
            // 
            this.dgv_PnNumber.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.dgv_PnNumber.FillWeight = 50F;
            this.dgv_PnNumber.HeaderText = "Number";
            this.dgv_PnNumber.MinimumWidth = 6;
            this.dgv_PnNumber.Name = "dgv_PnNumber";
            this.dgv_PnNumber.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.dgv_PnNumber.ToolTipText = "ProfiNet device number";
            this.dgv_PnNumber.Width = 57;
            // 
            // dgv_IpAddress
            // 
            this.dgv_IpAddress.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.dgv_IpAddress.FillWeight = 80F;
            this.dgv_IpAddress.HeaderText = "IP Address";
            this.dgv_IpAddress.MinimumWidth = 6;
            this.dgv_IpAddress.Name = "dgv_IpAddress";
            this.dgv_IpAddress.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
            this.dgv_IpAddress.Width = 120;
            // 
            // dgv_Mask
            // 
            this.dgv_Mask.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.dgv_Mask.FillWeight = 80F;
            this.dgv_Mask.HeaderText = "Subnet Mask";
            this.dgv_Mask.MinimumWidth = 6;
            this.dgv_Mask.Name = "dgv_Mask";
            this.dgv_Mask.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.dgv_Mask.Width = 120;
            // 
            // dgv_RouterAddress
            // 
            this.dgv_RouterAddress.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.dgv_RouterAddress.FillWeight = 80F;
            this.dgv_RouterAddress.HeaderText = "Router Address";
            this.dgv_RouterAddress.MinimumWidth = 6;
            this.dgv_RouterAddress.Name = "dgv_RouterAddress";
            this.dgv_RouterAddress.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.dgv_RouterAddress.Width = 120;
            // 
            // dgv_PnSubnet
            // 
            this.dgv_PnSubnet.FillWeight = 50F;
            this.dgv_PnSubnet.HeaderText = "PN Subnet";
            this.dgv_PnSubnet.MinimumWidth = 6;
            this.dgv_PnSubnet.Name = "dgv_PnSubnet";
            this.dgv_PnSubnet.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
            // 
            // dgv_IoSystem
            // 
            this.dgv_IoSystem.FillWeight = 80F;
            this.dgv_IoSystem.HeaderText = "IO System";
            this.dgv_IoSystem.MinimumWidth = 6;
            this.dgv_IoSystem.Name = "dgv_IoSystem";
            this.dgv_IoSystem.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel2.ColumnCount = 1;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Controls.Add(this.grp_Changes, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.groupBox1, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.groupBox2, 0, 2);
            this.tableLayoutPanel2.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel2.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel2.MinimumSize = new System.Drawing.Size(171, 300);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 3;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 46.93877F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 53.06123F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 83F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(172, 366);
            this.tableLayoutPanel2.TabIndex = 119;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.btn_CommitChanges);
            this.groupBox1.Controls.Add(this.btn_ClearChanges);
            this.groupBox1.Location = new System.Drawing.Point(6, 137);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.groupBox1.MaximumSize = new System.Drawing.Size(160, 145);
            this.groupBox1.MinimumSize = new System.Drawing.Size(159, 100);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.groupBox1.Size = new System.Drawing.Size(160, 127);
            this.groupBox1.TabIndex = 19;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Editing";
            // 
            // btn_CommitChanges
            // 
            this.btn_CommitChanges.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_CommitChanges.AutoSize = true;
            this.btn_CommitChanges.Enabled = false;
            this.btn_CommitChanges.Location = new System.Drawing.Point(12, 27);
            this.btn_CommitChanges.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.btn_CommitChanges.Name = "btn_CommitChanges";
            this.btn_CommitChanges.Size = new System.Drawing.Size(136, 40);
            this.btn_CommitChanges.TabIndex = 15;
            this.btn_CommitChanges.Text = "Commit Changes";
            this.btn_CommitChanges.UseVisualStyleBackColor = true;
            this.btn_CommitChanges.Click += new System.EventHandler(this.CommitChanges);
            // 
            // btn_ClearChanges
            // 
            this.btn_ClearChanges.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_ClearChanges.AutoSize = true;
            this.btn_ClearChanges.Enabled = false;
            this.btn_ClearChanges.Location = new System.Drawing.Point(12, 77);
            this.btn_ClearChanges.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.btn_ClearChanges.Name = "btn_ClearChanges";
            this.btn_ClearChanges.Size = new System.Drawing.Size(136, 40);
            this.btn_ClearChanges.TabIndex = 16;
            this.btn_ClearChanges.Text = "Clear Changes";
            this.btn_ClearChanges.UseVisualStyleBackColor = true;
            this.btn_ClearChanges.Click += new System.EventHandler(this.ClearChanges);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.txt_Search);
            this.groupBox2.Location = new System.Drawing.Point(3, 285);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(166, 58);
            this.groupBox2.TabIndex = 20;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Search box";
            this.groupBox2.Enter += new System.EventHandler(this.groupBox2_Enter);
            // 
            // txt_Search
            // 
            this.txt_Search.Enabled = false;
            this.txt_Search.Location = new System.Drawing.Point(15, 24);
            this.txt_Search.Name = "txt_Search";
            this.txt_Search.Size = new System.Drawing.Size(136, 24);
            this.txt_Search.TabIndex = 0;
            this.txt_Search.TextChanged += new System.EventHandler(this.DisplayOnlyFiltered);
            // 
            // progressBar
            // 
            this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar.Location = new System.Drawing.Point(3, 497);
            this.progressBar.Maximum = 2;
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(166, 15);
            this.progressBar.Step = 1;
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressBar.TabIndex = 120;
            // 
            // btn_ExportToCSV
            // 
            this.btn_ExportToCSV.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_ExportToCSV.AutoSize = true;
            this.btn_ExportToCSV.BackColor = System.Drawing.Color.Transparent;
            this.btn_ExportToCSV.Enabled = false;
            this.btn_ExportToCSV.Location = new System.Drawing.Point(10, 444);
            this.btn_ExportToCSV.Margin = new System.Windows.Forms.Padding(10, 20, 10, 10);
            this.btn_ExportToCSV.Name = "btn_ExportToCSV";
            this.btn_ExportToCSV.Size = new System.Drawing.Size(152, 40);
            this.btn_ExportToCSV.TabIndex = 18;
            this.btn_ExportToCSV.Text = "Export to CSV";
            this.btn_ExportToCSV.UseVisualStyleBackColor = false;
            this.btn_ExportToCSV.Click += new System.EventHandler(this.ExportToCSV);
            // 
            // txt_StatusBar
            // 
            this.txt_StatusBar.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txt_StatusBar.AutoSize = true;
            this.txt_StatusBar.Location = new System.Drawing.Point(175, 497);
            this.txt_StatusBar.Margin = new System.Windows.Forms.Padding(3);
            this.txt_StatusBar.Name = "txt_StatusBar";
            this.txt_StatusBar.Size = new System.Drawing.Size(925, 15);
            this.txt_StatusBar.TabIndex = 121;
            this.txt_StatusBar.Click += new System.EventHandler(this.txt_StatusBar_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Gainsboro;
            this.ClientSize = new System.Drawing.Size(1122, 537);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Font = new System.Drawing.Font("Siemens Sans", 8F);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(6, 5, 6, 5);
            this.MinimumSize = new System.Drawing.Size(1100, 450);
            this.Name = "Form1";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "Net Editor ";
            this.grp_Changes.ResumeLayout(false);
            this.grp_Changes.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.deviceTable)).EndInit();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }



        #endregion
        private System.Windows.Forms.TextBox txt_Status;
        private System.Windows.Forms.GroupBox grp_Changes;
        private System.Windows.Forms.Button btn_Connect;
        private TableLayoutPanel tableLayoutPanel1;
        private DataGridView deviceTable;
        private Button btn_Disconnect;
        private Button btn_ClearChanges;
        private Button btn_CommitChanges;
        private TableLayoutPanel tableLayoutPanel2;
        private GroupBox groupBox1;
        private Button btn_ExportToCSV;
        private ProgressBar progressBar;
        private Label txt_StatusBar;
        private RadioButton radio151;
        private RadioButton radio160;
        private DataGridViewTextBoxColumn dgv_Id;
        private DataGridViewTextBoxColumn dgv_Mode;
        private DataGridViewTextBoxColumn dgv_DeviceName;
        private DataGridViewCheckBoxColumn dgv_autoGeneratePnName;
        private DataGridViewTextBoxColumn dgv_PnDeviceName;
        private DataGridViewTextBoxColumn dgv_PnNumber;
        private DataGridViewTextBoxColumn dgv_IpAddress;
        private DataGridViewTextBoxColumn dgv_Mask;
        private DataGridViewTextBoxColumn dgv_RouterAddress;
        private DataGridViewTextBoxColumn dgv_PnSubnet;
        private DataGridViewTextBoxColumn dgv_IoSystem;
        private GroupBox groupBox2;
        private TextBox txt_Search;
    }
}

