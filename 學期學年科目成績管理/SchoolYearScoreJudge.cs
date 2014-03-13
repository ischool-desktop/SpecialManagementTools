using System.Collections.Generic;
using System.Linq;
using iAccess;
using SHSchool.Data;

namespace SpecialManagementTools
{
    public class SchoolYearScoreJudge
    {
        /// <summary>
        /// 學年科目成績判斷物件
        /// </summary>
        public class SchoolYearScoreJudgeRecord
        {
            /// <summary>
            /// 學生學號
            /// </summary>
            [Field(Caption = "學號")]
            public string StudentNumber { get; set; }
            /// <summary>
            /// 學生姓名
            /// </summary>
            [Field(Caption = "姓名")]
            public string Name { get; set; }
            /// <summary>
            /// 學年度
            /// </summary>
            [Field(Caption = "學年度")]
            public int SchoolYear { get; set; }
            /// <summary>
            /// 上學期科目
            /// </summary>
            [Field(Caption = "上學期科目")]
            public string UPSemesterSubject { get; set; }
            /// <summary>
            /// 上學期成績
            /// </summary>
            [Field(Caption = "上學期成績")]
            public decimal? UpScore { get; set; }
            /// <summary>
            /// 下學期科目
            /// </summary>
            [Field(Caption = "下學期科目")]
            public string DownSemesterSubject { get; set; }
            /// <summary>
            /// 下學期成績
            /// </summary>
            [Field(Caption = "下學期成績")]
            public decimal? DownSemesterScore { get; set; }
            /// <summary>
            /// 學年科目
            /// </summary>
            [Field(Caption = "學年科目")]
            public string SchoolYearSubject { get; set; }
            /// <summary>
            /// 學年成績
            /// </summary>
            [Field(Caption = "學期成績")]
            public decimal? SchoolYearScore { get; set; }


            public SchoolYearScoreJudgeRecord()
            {

            }
        }

        //[SelectMethod("SchoolYearScoreJudge", "取得選取學生學年科目不及格進校判斷")]
        public static List<SchoolYearScoreJudgeRecord> Select()
        {
            List<SchoolYearScoreJudgeRecord> Result = new List<SchoolYearScoreJudgeRecord>();

            List<string> StudentIDs = K12.Presentation.NLDPanels.Student.SelectedSource;

            if (K12.Data.Utility.Utility.IsNullOrEmpty(StudentIDs))
                return Result;

            List<SHSchoolYearScoreRecord> StudentSchoolYearScores = SHSchoolYearScore.SelectByStudentIDs(StudentIDs);
            List<SHSemesterScoreRecord> StudentScores = SHSemesterScore.SelectByStudentIDs(StudentIDs);

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

                                    SchoolYearScoreJudgeRecord record = new SchoolYearScoreJudgeRecord();

                                    record.StudentNumber = records[0].Student.StudentNumber;
                                    record.Name = records[0].Student.Name;
                                    record.SchoolYear = records[0].SchoolYear;
                                    record.UPSemesterSubject = (strSubject + records[0].Subjects[strSubject].Level);
                                    record.UpScore = records[0].Subjects[strSubject].Score;
                                    record.DownSemesterSubject = (strSubject + records[1].Subjects[strSubject].Level);
                                    record.DownSemesterScore = records[1].Subjects[strSubject].Score;
                                    record.SchoolYearSubject = strSubject;
                                    record.SchoolYearScore = SchoolYearScore;

                                    Result.Add(record);
                                }
                            }
                        }
                    }
                }
            }

            return Result;
        }
    }
}