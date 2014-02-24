using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using SHSchool.Data;

namespace 高中成績模組維護工具
{
    public partial class frmSchoolYear : FISCA.Presentation.Controls.BaseForm
    {
        private List<SHSchoolYearScoreRecord> StudentScores;
        private List<SHSchoolYearScoreRecord> UpdateRecords;
        private List<SHStudentRecord> StudentRecords;
        private StringBuilder strLog;

        public frmSchoolYear()
        {
            InitializeComponent();

            if (K12.Presentation.NLDPanels.Student.SelectedSource.Count > 0)
                RefreshUI();
            else
                MessageBox.Show("請選擇學生!");

            string DALMessage = "『"; 

            foreach(Assembly Assembly in AppDomain.CurrentDomain.GetAssemblies().Where(x=>x.GetName().Name.Equals("K12.Data")))
                DALMessage += "K12.Data " + Assembly.GetName().Version + " ";

            foreach (Assembly Assembly in AppDomain.CurrentDomain.GetAssemblies().Where(x => x.GetName().Name.Equals("SHSchool.Data")))
                DALMessage += " SHSchool.Data " + Assembly.GetName().Version;

            DALMessage += "』";

            this.Text += DALMessage;
        }

        private void frmSchoolYear_Load(object sender, EventArgs e)
        {
            //取得選取學生
            StudentRecords = SHStudent.SelectByIDs(K12.Presentation.NLDPanels.Student.SelectedSource);
        }

        /// <summary>
        /// 重新整理介面
        /// </summary>
        private void RefreshUI()
        {
            //取得學年科目成績
            StudentScores = SHSchoolYearScore.SelectByStudentIDs(K12.Presentation.NLDPanels.Student.SelectedSource);

            #region 取得選取學生的學年度及科目
            List<string> SchoolYears = new List<string>();
            List<string> Subjects = new List<string>();

            foreach (SHSchoolYearScoreRecord record in StudentScores)
            {
                if (!SchoolYears.Contains("" + record.SchoolYear))
                    SchoolYears.Add("" + record.SchoolYear);

                Subjects.AddRange(record.Subjects.Where(x=>!Subjects.Contains(x.Subject)).Select(x=>x.Subject));
            }

            cmbSubjects.SelectedItem = null;
            cmbSubjects.Items.Clear();
            cmbSchoolYear.SelectedItem = null;
            cmbSchoolYear.Items.Clear();

            Subjects.Sort();
            Subjects.Sort(new SubjectComparer() { });

            Subjects.ForEach(x => cmbSubjects.Items.Add(x));           

            SchoolYears.Sort();
            SchoolYears.ForEach(x => cmbSchoolYear.Items.Add(x));

            grdData.DataSource = null;
            #endregion
        }

        /// <summary>
        /// 判斷使用者是否有輸入學年度及科目
        /// </summary>
        /// <returns></returns>
        private bool IsInput()
        {
            if (!string.IsNullOrEmpty(""+cmbSchoolYear.SelectedItem))
                return true;

            return false;
        }

        /// <summary>
        /// 根據使用者所選擇的條件取得學期科目成績
        /// </summary>
        private void SelectByCondition()
        {
            if (!IsInput())
            {
                MessageBox.Show("請完整輸入欄位資訊!");
                return;
            }

            int SchoolYear = K12.Data.Int.Parse("" + cmbSchoolYear.SelectedItem);
            string Subject = "" + cmbSubjects.SelectedItem;
            strLog = new StringBuilder();

            UpdateRecords = StudentScores.Where(x => x.SchoolYear == SchoolYear).ToList();

            List<object> SubjectRecords = new List<object>();

            foreach (SHSchoolYearScoreRecord record in UpdateRecords)
            {
                SHStudentRecord s = StudentRecords.Find(x => x.ID.Equals(record.RefStudentID));

                foreach (SHSchoolYearScoreSubject SubjectScore in record.Subjects)
                    if (SubjectScore.Subject.Equals(Subject) || string.IsNullOrEmpty(Subject))
                        SubjectRecords.Add(new { 學生編號 = s.ID, 學號 = s.StudentNumber, 姓名 = s.Name, 學年度 = record.SchoolYear, 科目名稱 = SubjectScore.Subject });
            }

            grdData.DataSource = SubjectRecords; 
        }

        private void btnSelect_Click(object sender, System.EventArgs e)
        {
            SelectByCondition();
        }

        /// <summary>
        /// 更新學年科目名稱
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnChangeSubject_Click(object sender, System.EventArgs e)
        {
            if (!IsInput() && string.IsNullOrEmpty("" + cmbSubjects.SelectedItem) && !string.IsNullOrEmpty(txtChangeSubject.Text))
            {
                MessageBox.Show("請完整輸入欄位資訊!");
                return;
            }

            int SchoolYear = K12.Data.Int.Parse("" + cmbSchoolYear.SelectedItem);
            string Subject = "" + cmbSubjects.SelectedItem;
            string ChangeSubject = txtChangeSubject.Text;
            strLog = new StringBuilder();
            strLog.AppendLine("『更改學年科目名稱』");
            int UpdateCount = 0;

            List<SHSchoolYearScoreRecord> FinalUpdateRecords = new List<SHSchoolYearScoreRecord>();

            UpdateRecords = StudentScores.Where(x => x.SchoolYear == SchoolYear).ToList();

            foreach (SHSchoolYearScoreRecord record in UpdateRecords)
            {
                SHStudentRecord s = StudentRecords.Find(x => x.ID.Equals(record.RefStudentID));

                List<string> UpdateSubjects = new List<string>();

                foreach (SHSchoolYearScoreSubject SubjectScore in record.Subjects)
                    if (SubjectScore.Subject.Equals(Subject))
                    {
                        UpdateSubjects.Add(SubjectScore.Subject);
                        UpdateCount++;
                    }

                if (UpdateSubjects.Count > 0)
                {
                    FinalUpdateRecords.Add(record);
                    strLog.AppendLine("系統編號:" + record.ID + ",學生編號:" + s.ID + ",學生學號:" + s.StudentNumber + ",學生姓名:" + s.Name + ",學年度:" + record.SchoolYear +",更新後科目名稱:"+ChangeSubject);
                }

                foreach (string UpdateSubject in UpdateSubjects)
                {
                    SHSchoolYearScoreSubject Score = record.Subjects.Find(x => x.Subject.Equals(UpdateSubject));

                    record.Subjects.Remove(Score);
                    Score.Subject = ChangeSubject;
                    record.Subjects.Add(Score);
                }
            }

            int SuccessCount = 0;

            if (K12.Data.Utility.Utility.IsNullOrEmpty(FinalUpdateRecords))
            {
                MessageBox.Show("沒有資料不需更新!!");
                return;
            }

            try
            {
                if (MessageBox.Show("與您確認是否要更新總共" + UpdateCount + "筆成績？提醒在更新前先備份該位學生所有學年科目成績，並在成功後備份『更新紀錄』。", "更新學年科目成績確認", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    SuccessCount = SHSchoolYearScore.Update(FinalUpdateRecords);
                    MessageBox.Show("成功更新筆數:" + UpdateCount);
                    txtLog.Text = txtLog.Text + strLog.ToString();
                    FISCA.LogAgent.ApplicationLog.Log("成績模組.學年科目成績資料維護", strLog.ToString());
                }
            }
            catch (Exception ve)
            {
                MessageBox.Show(ve.Message);
                txtLog.Text = txtLog.Text + ve.Message;
            }
            finally
            {
                RefreshUI();
            }
        }

        /// <summary>
        /// 刪除單筆學年科目成績
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnDeleteSubject_Click(object sender, EventArgs e)
        {
            if (!IsInput())
            {
                MessageBox.Show("請完整輸入欄位資訊!");
                return;
            }            
    
            int SchoolYear = K12.Data.Int.Parse(""+cmbSchoolYear.SelectedItem);
            string Subject = ""+cmbSubjects.SelectedItem;
            strLog = new StringBuilder();
            strLog.AppendLine("『刪除學年科目成績紀錄』");
            int RemoveCount = 0;

            List<SHSchoolYearScoreRecord> FinalUpdateRecords = new List<SHSchoolYearScoreRecord>();
            List<SHSchoolYearScoreRecord> FinalDeleteRecords = new List<SHSchoolYearScoreRecord>();

            UpdateRecords = StudentScores.Where(x => x.SchoolYear == SchoolYear).ToList();

            foreach (SHSchoolYearScoreRecord record in UpdateRecords)
            {
                SHStudentRecord s = StudentRecords.Find(x => x.ID.Equals(record.RefStudentID));

                List<string> RemoveSubjects = new List<string>();

                foreach (SHSchoolYearScoreSubject SubjectScore in record.Subjects)
                    if (SubjectScore.Subject.Equals(Subject) || string.IsNullOrEmpty(Subject))
                    {
                        RemoveSubjects.Add(SubjectScore.Subject);
                        RemoveCount++;
                    }

                if (RemoveSubjects.Count > 0)
                {
                    FinalUpdateRecords.Add(record);
                    strLog.AppendLine("系統編號:" + record.ID + ",學生編號:" + s.ID + ",學生學號:" + s.StudentNumber + ",學生姓名:" + s.Name + ",學年度:" + record.SchoolYear);
                }

                foreach (string RemoveSubject in RemoveSubjects)
                {
                    SHSchoolYearScoreSubject removerecord = record.Subjects.Find(x => x.Subject.Equals(RemoveSubject));

                    record.Subjects.Remove(removerecord);
                }

                #region 移除時判斷是否科目已清空，若是則加到刪除清單中
                if (record.Subjects.Count == 0)
                    FinalDeleteRecords.Add(record);
                #endregion
            }

            int SuccessCount = 0;

            if (K12.Data.Utility.Utility.IsNullOrEmpty(FinalUpdateRecords))
            {
                MessageBox.Show("沒有資料不需刪除!!");
                return;
            }

            try
            {
                if (MessageBox.Show("與您確認是否要刪除總共" + RemoveCount + "筆成績？提醒在刪除前先備份該位學生所有學年科目成績，並在成功後備份『刪除紀錄』。","刪除學年科目成績確認", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    SuccessCount = SHSchoolYearScore.Update(FinalUpdateRecords);

                    if (FinalDeleteRecords.Count > 0)
                        SHSchoolYearScore.Delete(FinalDeleteRecords);

                    MessageBox.Show("成功刪除筆數:" + RemoveCount);
                    txtLog.Text = txtLog.Text + strLog.ToString();
                    FISCA.LogAgent.ApplicationLog.Log("成績模組.學年科目成績資料維護", strLog.ToString());
                }
            }
            catch (Exception ve)
            {
                MessageBox.Show(ve.Message);
                txtLog.Text = txtLog.Text + ve.Message;
            }
            finally
            {
                RefreshUI();
            }
        }

        /// <summary>
        /// 完整刪除單筆學年科目成績
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCompleteDelete_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(""+cmbSchoolYear.SelectedItem))
            {
                MessageBox.Show("請選擇學年度!");
                return;
            }

            int SchoolYear = K12.Data.Int.Parse("" + cmbSchoolYear.SelectedItem);
            strLog = new StringBuilder();
            strLog.AppendLine("『完整刪除學年科目成績紀錄』");

            List<SHSchoolYearScoreRecord> DeleteRecords = StudentScores.Where(x => x.SchoolYear == SchoolYear).ToList();

            foreach (SHSchoolYearScoreRecord record in DeleteRecords)
            {
                SHStudentRecord s = StudentRecords.Find(x => x.ID.Equals(record.RefStudentID));
                strLog.AppendLine("系統編號:" + record.ID + ",學生編號:" + s.ID + ",學生學號:" + s.StudentNumber + ",學生姓名:" + s.Name + ",學年度:" + record.SchoolYear);
            }

            int SuccessCount = 0;

            if (K12.Data.Utility.Utility.IsNullOrEmpty(DeleteRecords))
            {
                MessageBox.Show("沒有資料不需刪除!!");
                return;
            }

            try
            {
                if (MessageBox.Show("與您確認是否要刪除總共" + DeleteRecords.Count + "筆成績？提醒在刪除前先備份該位學生所有學年科目成績，並在成功後備份『刪除紀錄』。", "刪除學年科目成績確認", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    SuccessCount = SHSchoolYearScore.Delete(DeleteRecords);
                    MessageBox.Show("成功刪除筆數:" + SuccessCount);
                    txtLog.Text = txtLog.Text + strLog.ToString();
                    FISCA.LogAgent.ApplicationLog.Log("成績模組.學年科目成績資料維護", strLog.ToString());
                }
            }
            catch (Exception ve)
            {
                MessageBox.Show(ve.Message);
                txtLog.Text = txtLog.Text + ve.Message;
            }
            finally
            {
                RefreshUI();
            }
        }

        private void tabControlPanel1_DoubleClick(object sender, EventArgs e)
        {
            btnCompleteDelete.Visible = true;
        }
    }
}