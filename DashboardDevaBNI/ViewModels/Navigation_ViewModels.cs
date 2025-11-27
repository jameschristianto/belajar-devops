namespace DashboardDevaBNI.ViewModels
{
    public class Navigation_ViewModels
    {
        public Int64 Number { get; set; }
        public long Id { get; set; }
        public long? ParentNavigationId { get; set; }
        public string ParentName { get; set; }
        public int? Type { get; set; }
        public string TypeName { get; set; }
        public string Name { get; set; }
        public string Route { get; set; }
        public int? RoleId { get; set; }
        public int? Orderby { get; set; }
        public string Icon { get; set; }
        public DateTime? Createdtime { get; set; }
        public long? Createdbyid { get; set; }
        public DateTime? Updatedtime { get; set; }
        public long? Updatedbyid { get; set; }
        public DateTime? Deletedtime { get; set; }
        public long? Deletedbyid { get; set; }
        public bool? Isactive { get; set; }
        public bool? Isdeleted { get; set; }
    }
}
