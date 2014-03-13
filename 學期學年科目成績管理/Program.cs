using FISCA;
using FISCA.Permission;

namespace SpecialManagementTools
{    
    public static class Program 
    {
        [MainMethod]
        public static void Main()
        {
            K12.Presentation.NLDPanels.Student.SelectedSourceChanged += (sender, e) =>
            {
                if (K12.Presentation.NLDPanels.Student.SelectedSource.Count > 0)
                    K12.Presentation.NLDPanels.Student.RibbonBarItems["成績"]["學期科目成績資料維護"].Enable = FISCA.Permission.UserAcl.Current["SocreManagement.Ribbon0010"].Executable;
            };

            K12.Presentation.NLDPanels.Student.RibbonBarItems["成績"]["學期科目成績資料維護"].Click += (sender, e) => (new frmMain()).ShowDialog();
            K12.Presentation.NLDPanels.Student.RibbonBarItems["成績"]["學期科目成績資料維護"].Enable = false;


            
            K12.Presentation.NLDPanels.Student.SelectedSourceChanged += (sender, e) =>
            {
                if (K12.Presentation.NLDPanels.Student.SelectedSource.Count > 0)
                    K12.Presentation.NLDPanels.Student.RibbonBarItems["成績"]["學年科目成績資料維護"].Enable = FISCA.Permission.UserAcl.Current["SocreManagement.Ribbon0010"].Executable;
            };

            K12.Presentation.NLDPanels.Student.RibbonBarItems["成績"]["學年科目成績資料維護"].Click += (sender, e) => (new frmSchoolYear()).ShowDialog();
            K12.Presentation.NLDPanels.Student.RibbonBarItems["成績"]["學年科目成績資料維護"].Enable = false;

            
            FISCA.Permission.Catalog CourseCatalog = FISCA.Permission.RoleAclSource.Instance["學生"]["功能按鈕"];
            CourseCatalog.Add(new RibbonFeature("SocreManagement.Ribbon0010", "成績資料維護"));
        }
    }
}