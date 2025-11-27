namespace DashboardDevaBNI.ViewModels
{
    public class MasterMenu_ViewModel
    {
        public Int64 Number { get; set; }
        public long? Id { get; set; }
        public string TypeName { get; set; }
        public string ParentName { get; set; }
        public string Nama { get; set; }
        public string Tipe { get; set; }
        public int? TipeId { get; set; }
        public string Route { get; set; }
        public string Icon { get; set; }
        public string Role { get; set; }
        public long? ParentId { get; set; }
        public int OrderBy { get; set; }
        public bool IsActive { get; set; }
        public string CreatedTime { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedTime { get; set; }
        public string UpdatedBy { get; set; }
    }
}
