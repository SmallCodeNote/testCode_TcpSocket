﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;

using FluentScheduler;

using WinFormStringCnvClass;
using PanelScroll;

namespace tcpClient
{
    public partial class Form1 : Form
    {
        string thisExeDirPath;
        TcpSocketClient tcp;

        PanelScrollControl panelScrollControl_OnceJobList;
        PanelScrollControl panelScrollControl_StatusList;

        public Form1()
        {
            InitializeComponent();
            thisExeDirPath = Path.GetDirectoryName(Application.ExecutablePath);
            tcp = new TcpSocketClient();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            panelScrollControl_OnceJobList = new PanelScrollControl(panel_OnceJobListFrame, panel_OnceJobList, vScrollBar_OnceJobList);
            panelScrollControl_StatusList = new PanelScrollControl(panel_JobViewListFrame, panel_JobViewList, vScrollBar_StatusList);

            string paramFilename = Path.Combine(thisExeDirPath, "_param.txt");
            if (File.Exists(paramFilename))
            {
                WinFormStringCnv.setControlFromString(this, File.ReadAllText(paramFilename));
            }


            if (checkBox_LoadFromStoreAuto.Checked) { LoadFromStore(); };


        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            string FormContents = WinFormStringCnv.ToString(this);
            string paramFilename = Path.Combine(thisExeDirPath, "_param.txt");
            File.WriteAllText(paramFilename, FormContents);
        }

        private void button_SchedulerList_Click(object sender, EventArgs e)
        {
            SchedulerInitialize();
        }


        private async void button_SendMessage_Click(object sender, EventArgs e)
        {

            //JobManager.AddJob();
            string sendMessage = textBox_ClientName.Text + "\t" + comboBox_Status.Text + "\t" + textBox_Message.Text + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "\t" + textBox_Parameter.Text + "\t" + comboBox_checkStyle.Text;

            var responce = await tcp.StartClient(textBox_Address.Text, int.Parse(textBox_PortNumber.Text), sendMessage);
            label_Return.Text = responce;

        }

        private void button_AddSchedule_Click(object sender, EventArgs e)
        {
            SchedulerInitialize();

        }



        public void tabPage_View_Enter(object sender = null, EventArgs e = null)
        {
            if (sender != null) timer_ClientViewUpdate.Start();

            var allSchedules = JobManager.AllSchedules;

            List<string> scheduleList = new List<string>();

            foreach (var schedule in allSchedules)
            {
                List<string> Cols = new List<string>();

                Cols.Add(schedule.Name);
                Cols.Add(schedule.NextRun.Second.ToString());
                Cols.Add(schedule.ToString());

                scheduleList.Add(string.Join("\t", Cols.ToArray()));

            }

            updateJobViewList();

        }


        List<JobItemView> jobItemViews = new List<JobItemView>();

        private void updateJobViewList()
        {
            try
            {
                panel_JobViewList.Controls.Clear();

                List<Schedule> sortedSchedules = JobManager.AllSchedules.OrderBy(schedule => schedule.NextRun).ToList();

                int topLocation = 0;

                foreach (var schedule in sortedSchedules)
                {

                    List<string> Cols = new List<string>();

                    var jobItemView = new JobItemView();
                    jobItemView.Top = topLocation;
                    jobItemView.Left = 0;

                    jobItemView.setLabel(schedule, this);

                    panel_JobViewList.Controls.Add(jobItemView);

                    topLocation += jobItemView.Height;

                }

                panel_JobViewList.Height = topLocation;

            }
            catch (Exception ex)
            {
                Debug.WriteLine(GetType().Name + "::" + System.Reflection.MethodBase.GetCurrentMethod().Name + " EX:" + ex.ToString());
            }


        }


        int JobManager_AllSchedules_Count_Buff = 0;

        private void timer_ClientViewUpdate_Tick(object sender, EventArgs e)
        {
            int jobCount = JobManager.AllSchedules.Count();
            if (JobManager_AllSchedules_Count_Buff != jobCount)
            {
                JobManager_AllSchedules_Count_Buff = jobCount;
                tabPage_View_Enter();
            }
        }

        private void tabPage_View_Leave(object sender, EventArgs e)
        {
            timer_ClientViewUpdate.Stop();
        }

        private void LoadFromStore()
        {

            string[] Lines = textBox_Store.Text.Replace("\r\n", "\n").Trim('\n').Split('\n');

            var job = new FluentSchedulerRegistry(tcp, Lines);

            JobManager.RemoveAllJobs();
            JobManager.Initialize(job);

        }

        private void button_LoadFromStore_Click(object sender, EventArgs e)
        {

            LoadFromStore();

        }

        private void button_EditContentsFromClipboard_Click(object sender, EventArgs e)
        {
            string Line = Clipboard.GetText();

            string[] Cols = Line.Split('\t');

            if (Cols.Length == 10)
            {

                int colIndex = 0;
                textBox_Address.Text = Cols[colIndex]; colIndex++;
                textBox_PortNumber.Text = Cols[colIndex]; colIndex++;

                textBox_JobName.Text = Cols[colIndex]; colIndex++;
                comboBox_ScheduleUnit.Text = Cols[colIndex]; colIndex++;
                textBox_ScheduleUnitParam.Text = Cols[colIndex]; colIndex++;
                textBox_ClientName.Text = Cols[colIndex]; colIndex++;
                comboBox_Status.Text = Cols[colIndex]; colIndex++;
                textBox_Message.Text = Cols[colIndex]; colIndex++;
                textBox_Parameter.Text = Cols[colIndex]; colIndex++;
                comboBox_checkStyle.Text = Cols[colIndex];

            }

        }

        private void comboBox_ScheduleUnit_SelectedIndexChanged(object sender, EventArgs e)
        {
            button_AddOnceJobPanel.Enabled = comboBox_ScheduleUnit.Text.IndexOf("Once") >= 0;

            //toolTip Update
            if (comboBox_ScheduleUnit.Text == "EveryDays") { toolTip_textBox_ScheduleUnitParam.SetToolTip(textBox_ScheduleUnitParam, "Hour and Minute value(ex. 8:15 )"); }
            else if (comboBox_ScheduleUnit.Text == "EveryHours") { toolTip_textBox_ScheduleUnitParam.SetToolTip(textBox_ScheduleUnitParam, "Minute value(ex. 10)"); }
            else if (comboBox_ScheduleUnit.Text == "EverySeconds") { toolTip_textBox_ScheduleUnitParam.SetToolTip(textBox_ScheduleUnitParam, "Interval in Seconds"); }
            else if (comboBox_ScheduleUnit.Text == "OnceAtSeconds") { toolTip_textBox_ScheduleUnitParam.SetToolTip(textBox_ScheduleUnitParam, "Delay time value in Seconds"); }
            else if (comboBox_ScheduleUnit.Text == "OnceAtMinutes") { toolTip_textBox_ScheduleUnitParam.SetToolTip(textBox_ScheduleUnitParam, "Delay time value in Minutes"); }
            else if (comboBox_ScheduleUnit.Text == "OnceAtHours") { toolTip_textBox_ScheduleUnitParam.SetToolTip(textBox_ScheduleUnitParam, "Delay time value in Hours"); }

            textBox_ScheduleUnitParam_TextChanged();

        }

        private void button_AddOnceJobPanel_Click(object sender, EventArgs e)
        {
            textBox_OnceJobPanelStore.Text += getJobStringFromEditControls() + "\r\n";
        }


        public string getJobStringFromEditControls()
        {


            string Address = textBox_Address.Text;
            string PortNumber = textBox_PortNumber.Text;

            string JobName = textBox_JobName.Text;
            string ScheduleUnit = comboBox_ScheduleUnit.Text;
            string ScheduleUnitParam = textBox_ScheduleUnitParam.Text;
            string ClientName = textBox_ClientName.Text;
            string Status = comboBox_Status.Text;
            string Message = textBox_Message.Text;
            string Parameter = textBox_Parameter.Text;
            string CheckStyle = comboBox_checkStyle.Text;

            List<string> ColList = new List<string>();
            ColList.Add(Address);
            ColList.Add(PortNumber);

            ColList.Add(JobName);
            ColList.Add(ScheduleUnit);
            ColList.Add(ScheduleUnitParam);
            ColList.Add(ClientName);
            ColList.Add(Status);
            ColList.Add(Message);
            ColList.Add(Parameter);
            ColList.Add(CheckStyle.ToString());

            return String.Join("\t", ColList.ToArray());

        }
        private void SchedulerInitialize()
        {
            List<string> Lines = new List<string>();
            Lines.Add(getJobStringFromEditControls());

            var job = new FluentSchedulerRegistry(tcp, Lines.ToArray());
            JobManager.Initialize(job);

            string ScheduleUnit = comboBox_ScheduleUnit.Text;

            if (ScheduleUnit.IndexOf("Once") < 0)
            {
                textBox_Store.Text += string.Join("\r\n", Lines.ToArray()) + "\r\n";
            }

        }

        private void update_OnceJobPanel()
        {

            string[] Lines = textBox_OnceJobPanelStore.Text.Replace("\r\n", "\n").Trim('\n').Split('\n');

            panel_OnceJobList.Controls.Clear();

            int viewTop = 0;
            foreach (var Line in Lines)
            {
                string[] Cols = Line.Split('\t');
                if (Cols.Length == 10)
                {
                    var onceJobView = new OnceJobView(tcp, Cols, this);
                    onceJobView.Top = viewTop;
                    panel_OnceJobList.Controls.Add(onceJobView);
                    panel_OnceJobList.Width = onceJobView.Width;

                    viewTop += onceJobView.Height;
                }
            }

            panel_OnceJobList.Height = viewTop;

        }

        private void textBox_OnceJobPanelStore_TextChanged(object sender, EventArgs e)
        {
            update_OnceJobPanel();

        }

        private void button_StatusListFrame_Sort_Click(object sender, EventArgs e)
        {
            updateJobViewList();
        }

        private void textBox_ScheduleUnitParam_TextChanged(object sender = null, EventArgs e = null)
        {
            TextBox textBox = textBox_ScheduleUnitParam;

            if (comboBox_ScheduleUnit.Text.IndexOf("Day") >= 0)
            {
                // 正規表現パターン
                string pattern = @"^([01]?[0-9]|2[0-3]):[0-5][0-9]$";

                // Textboxの内容をカンマで分割
                string[] times = new string[] { textBox.Text };// textBox.Text.Split(',');

                // 各時刻についてチェック
                foreach (string time in times)
                {
                    if (!Regex.IsMatch(time, pattern))
                    {
                        // パターンと一致しない場合は文字色を赤に設定
                        textBox.ForeColor = Color.Red;

                        button_AddOnceJobPanel.Enabled = false;
                        button_AddSchedule.Enabled = false;

                        return;
                    }
                }

                // 全ての時刻が正常であれば文字色を黒に設定
                textBox.ForeColor = Color.Black;
            }
            else
            {

                // 正規表現パターン
                string pattern = @"^\d+$";

                // Textboxの内容をカンマで分割
                string[] times = new string[] { textBox.Text };// textBox.Text.Split(',');

                // 各時刻についてチェック
                foreach (string time in times)
                {
                    if (!Regex.IsMatch(time, pattern))
                    {
                        // パターンと一致しない場合は文字色を赤に設定
                        textBox.ForeColor = Color.Red;

                        button_AddOnceJobPanel.Enabled = false;
                        button_AddSchedule.Enabled = false;

                        return;
                    }
                }

                // 全ての時刻が正常であれば文字色を黒に設定
                textBox.ForeColor = Color.Black;


            }

            button_AddOnceJobPanel.Enabled = comboBox_ScheduleUnit.Text.IndexOf("Once") >= 0;
            button_AddSchedule.Enabled = true;

        }

        private void textBox_Message_TextChanged(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;

           
        }
    }
}
