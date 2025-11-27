namespace DashboardDevaBNI.ViewModels
{
    public class Downloadlist_LoadDataVM
    {
        public Int64 Number { get; set; }
        public int Id { get; set; }
        public DateTime? GeneratedDate { get; set; }
        public DateTime? ExpiredDate { get; set; }
        public string? FileName { get; set; }
        public string? Extension { get; set; }
        public int? Status { get; set; }
        public string? Path { get; set; }
    }
}
