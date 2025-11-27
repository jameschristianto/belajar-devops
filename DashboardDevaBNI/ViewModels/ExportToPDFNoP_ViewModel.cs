using DashboardDevaBNI.Models;

namespace DashboardDevaBNI.ViewModels
{
    public class ExportToPDFNoP_ViewModel
    {
        public string UserLogin { get; set; }

        public string TanggalCetak { get; set; }

        public string Domain { get; set; }

        public string Logo { get; set; }

        public List<TblNoticeOfPayment> ListData { get; set; }
    }
}
