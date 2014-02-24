using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using SHSchool.Data;

namespace 高中成績模組維護工具
{
    public partial class frmMain : FISCA.Presentation.Controls.BaseForm
    {
        private List<SHSchoolYearScoreRecord> StudentSchoolYearScores;
        private List<SHSemesterScoreRecord> StudentScores;
        private List<SHSemesterScoreRecord> UpdateRecords;
        private StringBuilder strLog;

        public frmMain()
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

        private void RefreshUI()
        {
            List<string> SelectStudentIDs = K12.Presentation.NLDPanels.Student.SelectedSource;

            StudentScores = SHSemesterScore.SelectByStudentIDs(SelectStudentIDs, false);

            //取得選取學生的學年科目成績
            StudentSchoolYearScores = SHSchoolYearScore.SelectByStudentIDs(SelectStudentIDs);

            //根據選取學生的學期科目成績計算出成績裡的學年度、學期、級別及科目
            List<string> SchoolYears = new List<string>();
            List<string> Semesters = new List<string>();
            List<int> Levels = new List<int>();
            List<string> Subjects = new List<string>();

            foreach (SHSemesterScoreRecord record in StudentScores)
            {
                if (!SchoolYears.Contains("" + record.SchoolYear))
                    SchoolYears.Add("" + record.SchoolYear);

                if (!Semesters.Contains("" + record.Semester))
                    Semesters.Add("" + record.Semester);

                Levels.AddRange(record.Subjects.Values.Where(x => x.Level.HasValue).Select(x => x.Level.Value).ToList());
                Subjects.AddRange(record.Subjects.Values.Where(x=>!Subjects.Contains(x.Subject)).Select(x=>x.Subject));
            }

            Levels.Sort();
            Levels.Distinct().ToList().ForEach(x => cmbLevels.Items.Add("" + x));

            Subjects.Sort();
            Subjects.Sort(new SubjectComparer() { });
            Subjects.ForEach(x => cmbSubjects.Items.Add(x));

            SchoolYears.Sort();
            SchoolYears.ForEach(x => cmbSchoolYear.Items.Add(x));

            Semesters.Sort();
            Semesters.ForEach(x => cmbSemester.Items.Add(x));
        }

        /// <summary>
        /// 判斷使用者是否有輸入學年度、學期、級別及科目
        /// </summary>
        /// <returns></returns>
        private bool IsInput()
        {
            if (!string.IsNullOrEmpty(""+cmbSchoolYear.SelectedItem) && !string.IsNullOrEmpty(""+cmbSemester.SelectedItem) && !string.IsNullOrEmpty(cmbSubjects.Text))
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
            int Semester = K12.Data.Int.Parse("" + cmbSemester.SelectedItem);
            int? Level = K12.Data.Int.ParseAllowNull("" + cmbLevels.SelectedItem);
            string Subject = "" + cmbSubjects.SelectedItem;
            strLog = new StringBuilder();

            txtChangeSubject.Text = Subject;
            txtChangeLevel.Text = K12.Data.Int.GetString(Level);

            UpdateRecords = StudentScores.Where(x => x.SchoolYear == SchoolYear && x.Semester == Semester).ToList();

            List<object> SubjectRecords = new List<object>();

            foreach (SHSemesterScoreRecord record in UpdateRecords)
                foreach (SHSubjectScore SubjectScore in record.Subjects.Values)
                {
                    if (SubjectScore.Subject.Equals(Subject) && K12.Data.Int.GetString(SubjectScore.Level).Equals(K12.Data.Int.GetString(Level)))
                        SubjectRecords.Add(new { 學生編號 = record.ID, 學號 = record.Student.StudentNumber, 姓名 = record.Student.Name, 學年度 = record.SchoolYear, 學期 = record.Semester, 科目名稱 = Subject, 科目級別 = record.Subjects[Subject].Level });
                }

            grdData.DataSource = SubjectRecords; 
        }

        private void btnSelect_Click(object sender, System.EventArgs e)
        {
            SelectByCondition();
        }

        private void btnChangeSubject_Click(object sender, System.EventArgs e)
        {
            //if (record.Subjects.ContainsKey(Subject))
            //{
            //    //strLog.AppendLine(record.ID,record.Student.StudentNumber,);
            //    SHSubjectScore Score = record.Subjects[Subject];
            //    record.Subjects.Remove(Subject);
            //    Score.Subject = ChangeSubject;
            //    record.Subjects.Add(Score.Subject, Score);
            //}

            if (!IsInput() && !string.IsNullOrEmpty(txtChangeSubject.Text))
            {
                MessageBox.Show("請完整輸入欄位資訊!");
                return;
            }

            int a;

            if (!(int.TryParse(txtChangeLevel.Text, out a) || string.IsNullOrEmpty(txtChangeLevel.Text)))
            {
                MessageBox.Show("級別必需為數字或空白！");
                return;
            }

            int SchoolYear = K12.Data.Int.Parse("" + cmbSchoolYear.SelectedItem);
            int Semester = K12.Data.Int.Parse("" + cmbSemester.SelectedItem);
            int? Level = K12.Data.Int.ParseAllowNull("" + cmbLevels.SelectedItem);
            int? ChangeLevel = K12.Data.Int.ParseAllowNull("" + txtChangeLevel.Text);
            string Subject = "" + cmbSubjects.SelectedItem;
            string ChangeSubject = txtChangeSubject.Text;
            strLog = new StringBuilder();
            strLog.AppendLine("『更改學期科目名稱』");
            int UpdateCount = 0;

            List<SHSemesterScoreRecord> FinalUpdateRecords = new List<SHSemesterScoreRecord>();

            UpdateRecords = StudentScores.Where(x => x.SchoolYear == SchoolYear && x.Semester == Semester).ToList();

            foreach (SHSemesterScoreRecord record in UpdateRecords)
            {
                List<string> UpdateSubjects = new List<string>();

                foreach (SHSubjectScore SubjectScore in record.Subjects.Values)
                {
                    if (SubjectScore.Subject.Equals(Subject) && K12.Data.Int.GetString(SubjectScore.Level) == K12.Data.Int.GetString(Level))
                    {
                        UpdateSubjects.Add(SubjectScore.Subject);
                        UpdateCount++;
                    }
                }

                if (UpdateSubjects.Count > 0)
                {
                    FinalUpdateRecords.Add(record);
                    strLog.AppendLine("系統編號:" + record.ID + ",學生編號:" + record.RefStudentID + ",學生學號:" + record.Student.StudentNumber + ",學生姓名:" + record.Student.Name + ",學年度:" + record.SchoolYear + ",學期:" + record.Semester+",更新後科目名稱:"+ChangeSubject);
                }

                foreach (string UpdateSubject in UpdateSubjects)
                {
                    strLog.AppendLine(record.Subjects[UpdateSubject].ToXML().OuterXml);

                    SHSubjectScore Score = record.Subjects[UpdateSubject];

                    record.Subjects.Remove(UpdateSubject);

                    Score.Subject = ChangeSubject;
                    Score.Level = ChangeLevel;

                    record.Subjects.Add(Score.Subject, Score);
                }
            }

            int SuccessCount = 0;

            if (K12.Data.Utility.Utility.IsNullOrEmpty(FinalUpdateRecords))
            {
                MessageBox.Show("沒有資料不需更新!!");
                return;
            }

            if (MessageBox.Show("與您確認是否要更新總共" + UpdateCount + "筆成績？提醒在更新前先備份該位學生所有學期科目成績，並在成功後備份『更新紀錄』。", "更新學期科目成績確認", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                try
                {
                    SuccessCount = SHSemesterScore.Update(FinalUpdateRecords);
                    MessageBox.Show("成功更新筆數:" + UpdateCount);
                    txtLog.Text = txtLog.Text + strLog.ToString();
                    FISCA.LogAgent.ApplicationLog.Log("成績模組.學期科目成績資料維護", strLog.ToString());
                    SelectByCondition();
                }
                catch (Exception ve)
                {
                    MessageBox.Show(ve.Message);
                    txtLog.Text = txtLog.Text + ve.Message;
                }
                finally
                {
                }
            }
        }

        private void btnDeleteSubject_Click(object sender, EventArgs e)
        {
            if (!IsInput())
            {
                MessageBox.Show("請完整輸入欄位資訊!");
                return;
            }            
    
            int SchoolYear = K12.Data.Int.Parse(""+cmbSchoolYear.SelectedItem);
            int Semester = K12.Data.Int.Parse(""+cmbSemester.SelectedItem);
            int? Level = K12.Data.Int.ParseAllowNull(""+cmbLevels.SelectedItem);
            string Subject = ""+cmbSubjects.SelectedItem;
            strLog = new StringBuilder();
            strLog.AppendLine("『刪除學期科目成績紀錄』");
            int RemoveCount = 0;

            List<SHSemesterScoreRecord> FinalUpdateRecords = new List<SHSemesterScoreRecord>();

            UpdateRecords = StudentScores.Where(x => x.SchoolYear == SchoolYear && x.Semester == Semester).ToList();

            foreach (SHSemesterScoreRecord record in UpdateRecords)
            {
                List<string> RemoveSubjects = new List<string>();

                foreach (SHSubjectScore SubjectScore in record.Subjects.Values)
                    if (SubjectScore.Subject.Equals(Subject) && SubjectScore.Level.Equals(Level))
                    {
                        RemoveSubjects.Add(SubjectScore.Subject);
                        RemoveCount++;
                    }

                if (RemoveSubjects.Count > 0)
                {
                    FinalUpdateRecords.Add(record);
                    strLog.AppendLine("系統編號:" + record.ID + ",學生編號:" + record.RefStudentID + ",學生學號:" + record.Student.StudentNumber + ",學生姓名:" + record.Student.Name + ",學年度:" + record.SchoolYear + ",學期:" + record.Semester);
                }

                foreach (string RemoveSubject in RemoveSubjects)
                {
                    strLog.AppendLine(record.Subjects[RemoveSubject].ToXML().OuterXml);
                    record.Subjects.Remove(RemoveSubject);
                }
            }

            int SuccessCount = 0;

            if (K12.Data.Utility.Utility.IsNullOrEmpty(FinalUpdateRecords))
            {
                MessageBox.Show("沒有資料不需刪除!!");
                return;
            }

            if (MessageBox.Show("與您確認是否要刪除總共" + RemoveCount + "筆成績？提醒在刪除前先備份該位學生所有學期科目成績，並在成功後備份『刪除紀錄』。","刪除學期科目成績確認", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                try
                {
                    SuccessCount = SHSemesterScore.Update(FinalUpdateRecords);
                    MessageBox.Show("成功刪除筆數:" + RemoveCount);
                    txtLog.Text = txtLog.Text + strLog.ToString();
                    FISCA.LogAgent.ApplicationLog.Log("成績模組.學期科目成績資料維護", strLog.ToString());
                    SelectByCondition();
                }
                catch (Exception ve)
                {
                    MessageBox.Show(ve.Message);
                    txtLog.Text = txtLog.Text + ve.Message;
                }
                finally
                {
                }
            }
        }

        private void btnQuery_Click(object sender, EventArgs e)
        {
            List<object> Result = new List<object>();

            //將學期科目成績用學生編號做群組
            foreach (var StudentGroup in StudentScores.GroupBy(x => x.RefStudentID))
            {
                //將個別學生學期科目成績用學年度做群組
                foreach (var StudentSchoolYearGroup in StudentGroup.GroupBy(x => x.SchoolYear))
                {
                    //假學生的學年有上下兩筆學期科目成績才進行處理
                    if (StudentSchoolYearGroup.ToList().Count >= 2)
                    {
                        //將資料根據學期加以排序
                        List<SHSemesterScoreRecord> records = StudentSchoolYearGroup.OrderBy(x => x.Semester).ToList();

                        //掃描上學期的每筆學期科目成績
                        foreach (string strSubject in records[0].Subjects.Keys)
                        {
                            //判斷下學期是否有相同的科目名稱
                            if (records[1].Subjects.ContainsKey(strSubject))
                            {
                                //上學期科目成績
                                decimal? UpScore = records[0].Subjects[strSubject].Score;
                                //下學期科目成績
                                decimal? DownScore = records[1].Subjects[strSubject].Score;

                                //判斷是否上學期科目成績大於50小於60，並且下學期科目成績大於60
                                if ((UpScore >= 50 && UpScore < 60) && DownScore > 60)
                                {
                                    decimal? SchoolYearScore = null;

                                    List<SHSchoolYearScoreRecord> SchoolYearScoreRecords = StudentSchoolYearScores.Where(x => x.RefStudentID.Equals(StudentGroup.Key) && x.SchoolYear.Equals(StudentSchoolYearGroup.Key)).ToList();

                                    if (SchoolYearScoreRecords.Count > 0)
                                    {
                                        List<SHSchoolYearScoreSubject> Subjects = SchoolYearScoreRecords[0].Subjects.Where(x => x.Subject.Equals(strSubject)).ToList();

                                        if (Subjects.Count > 0) 
                                            SchoolYearScore = Subjects[0].Score;
                                    }

                                    Result.Add(new { 學號 = records[0].Student.StudentNumber, 姓名 = records[0].Student.Name, 學年度 = records[0].SchoolYear, 上學期科目 = (strSubject + records[0].Subjects[strSubject].Level), 上學期成績 = records[0].Subjects[strSubject].Score, 下學期科目 = (strSubject + records[1].Subjects[strSubject].Level), 下學期成績 = records[1].Subjects[strSubject].Score,學年科目= strSubject,學年成績= SchoolYearScore});
                                }
                            }
                        }
                    }
                }       
            }

            grdScore.DataSource = Result;
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            ExportUtil.Export(grdScore, "學期及學年科目成績查詢");
        }
    }
}