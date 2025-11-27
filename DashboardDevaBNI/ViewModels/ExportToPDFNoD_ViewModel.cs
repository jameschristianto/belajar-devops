using DashboardDevaBNI.Models;

namespace DashboardDevaBNI.ViewModels
{
    public class ExportToPDFNoD_ViewModel
    {
        public string UserLogin { get; set; }

        public string TanggalCetak { get; set; }

        public string Domain { get; set; }

        public string Logo { get; set; }

        public List<TblNoticeOfDisbursement> ListData {  get; set; }
    }

}
