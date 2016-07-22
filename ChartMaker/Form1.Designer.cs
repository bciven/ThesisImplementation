namespace ChartMaker
{
    partial class Charts
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea3 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend3 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea4 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend4 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            this.textBoxFolder = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.buttonBrowse = new System.Windows.Forms.Button();
            this.welfareChart = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.buttonAdd = new System.Windows.Forms.Button();
            this.buttonRemove = new System.Windows.Forms.Button();
            this.listBox = new System.Windows.Forms.ListBox();
            this.regChart = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.buttonOutput = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.welfareChart)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.regChart)).BeginInit();
            this.SuspendLayout();
            // 
            // textBoxFolder
            // 
            this.textBoxFolder.Location = new System.Drawing.Point(53, 594);
            this.textBoxFolder.Name = "textBoxFolder";
            this.textBoxFolder.Size = new System.Drawing.Size(272, 20);
            this.textBoxFolder.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 598);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(39, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Folder:";
            // 
            // buttonBrowse
            // 
            this.buttonBrowse.Location = new System.Drawing.Point(331, 593);
            this.buttonBrowse.Name = "buttonBrowse";
            this.buttonBrowse.Size = new System.Drawing.Size(75, 23);
            this.buttonBrowse.TabIndex = 2;
            this.buttonBrowse.Text = "Browse";
            this.buttonBrowse.UseVisualStyleBackColor = true;
            this.buttonBrowse.Click += new System.EventHandler(this.buttonBrowse_Click);
            // 
            // welfareChart
            // 
            chartArea3.BackColor = System.Drawing.Color.White;
            chartArea3.Name = "ChartArea1";
            this.welfareChart.ChartAreas.Add(chartArea3);
            legend3.Name = "Legend1";
            legend3.Title = "Welfare";
            this.welfareChart.Legends.Add(legend3);
            this.welfareChart.Location = new System.Drawing.Point(14, 12);
            this.welfareChart.Name = "welfareChart";
            this.welfareChart.Size = new System.Drawing.Size(467, 260);
            this.welfareChart.TabIndex = 3;
            this.welfareChart.Text = "Welfare";
            // 
            // buttonAdd
            // 
            this.buttonAdd.Location = new System.Drawing.Point(487, 593);
            this.buttonAdd.Name = "buttonAdd";
            this.buttonAdd.Size = new System.Drawing.Size(75, 23);
            this.buttonAdd.TabIndex = 4;
            this.buttonAdd.Text = "Add";
            this.buttonAdd.UseVisualStyleBackColor = true;
            this.buttonAdd.Click += new System.EventHandler(this.buttonDraw_Click);
            // 
            // buttonRemove
            // 
            this.buttonRemove.Location = new System.Drawing.Point(577, 593);
            this.buttonRemove.Name = "buttonRemove";
            this.buttonRemove.Size = new System.Drawing.Size(75, 23);
            this.buttonRemove.TabIndex = 6;
            this.buttonRemove.Text = "Remove";
            this.buttonRemove.UseVisualStyleBackColor = true;
            this.buttonRemove.Click += new System.EventHandler(this.buttonRemove_Click);
            // 
            // listBox
            // 
            this.listBox.FormattingEnabled = true;
            this.listBox.Location = new System.Drawing.Point(487, 12);
            this.listBox.Name = "listBox";
            this.listBox.Size = new System.Drawing.Size(165, 576);
            this.listBox.TabIndex = 7;
            // 
            // regChart
            // 
            chartArea4.BackColor = System.Drawing.Color.White;
            chartArea4.Name = "ChartArea1";
            this.regChart.ChartAreas.Add(chartArea4);
            legend4.Name = "Legend1";
            legend4.Title = "Welfare";
            this.regChart.Legends.Add(legend4);
            this.regChart.Location = new System.Drawing.Point(15, 327);
            this.regChart.Name = "regChart";
            this.regChart.Size = new System.Drawing.Size(467, 260);
            this.regChart.TabIndex = 8;
            this.regChart.Text = "Regret Ratio";
            // 
            // buttonOutput
            // 
            this.buttonOutput.Location = new System.Drawing.Point(407, 593);
            this.buttonOutput.Name = "buttonOutput";
            this.buttonOutput.Size = new System.Drawing.Size(75, 23);
            this.buttonOutput.TabIndex = 9;
            this.buttonOutput.Text = "Write Output";
            this.buttonOutput.UseVisualStyleBackColor = true;
            this.buttonOutput.Click += new System.EventHandler(this.buttonOutput_Click);
            // 
            // Charts
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(664, 620);
            this.Controls.Add(this.buttonOutput);
            this.Controls.Add(this.regChart);
            this.Controls.Add(this.listBox);
            this.Controls.Add(this.buttonRemove);
            this.Controls.Add(this.buttonAdd);
            this.Controls.Add(this.welfareChart);
            this.Controls.Add(this.buttonBrowse);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBoxFolder);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "Charts";
            this.Text = "Form";
            ((System.ComponentModel.ISupportInitialize)(this.welfareChart)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.regChart)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxFolder;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button buttonBrowse;
        private System.Windows.Forms.DataVisualization.Charting.Chart welfareChart;
        private System.Windows.Forms.Button buttonAdd;
        private System.Windows.Forms.Button buttonRemove;
        private System.Windows.Forms.ListBox listBox;
        private System.Windows.Forms.DataVisualization.Charting.Chart regChart;
        private System.Windows.Forms.Button buttonOutput;
    }
}

