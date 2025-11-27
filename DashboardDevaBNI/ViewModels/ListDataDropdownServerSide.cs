using System.ComponentModel.DataAnnotations;

namespace DashboardDevaBNI.ViewModels
{
	public class ListDataDropdownServerSide
	{
		public List<DataDropdownServerSide> items { get; set; }
		public int total_count { get; set; }
	}
	public class ListDropdownServerSideVM
	{
		public List<DropdownServerSideVM> items { get; set; }
		public int total_count { get; set; }
	}
    public class ListDataDropdownServerSideDeva
    {
        public List<DataDropdownServerSideDeva> items { get; set; }
        public int total_count { get; set; }
    }
    public class DataDropdownServerSideDeva
    {
        public string id { get; set; }
        public string text { get; set; }
        public string format_selected { get; set; }
        public string nama_text { get; set; }
        public string LoanId { get; set; }
        public int RekId { get; set; }
        public string Rekening { get; set; }
        public string CreditorRef { get; set; }
        public DateTime? DSign { get; set; }
        public string Amount { get; set; }
        public string acc { get; set; }
        public string Cur { get; set; }
    }
    public class DataDropdownServerSide
	{
		public long id { get; set; }
		public string text { get; set; }
		public string format_selected { get; set; }
		public string nama_text { get; set; }
		public string LoanId { get; set; }
        public string RekId { get; set; }
        public string Rekening { get; set; }
        public string CreditorRef { get; set; }
		public DateTime? DSign { get; set; }
		public string Amount { get; set; }
		public string Cur { get; set; }
	}
	public class DropdownServerSideVM
	{
		public string id { get; set; }
		public string text { get; set; }
		public string format_selected { get; set; }
		public string nama_text { get; set; }
	}
	public class DropdownServerSideIntVM
	{
		public int? id { get; set; }
		public string text { get; set; }
		public string format_selected { get; set; }
		public string nama_text { get; set; }
	}
	public class DataDropdownParent
    {
        public long Number { get; set; }
        public long Id { get; set; }
        public string text { get; set; }
    }
    public class DataDropdownParentList
    {
        public List<DataDropdownParent> items { get; set; }

		public int total_count { get; set;}

    }

	public class DropDownRole
	{
		public long id { set; get; }
		public string text { set; get; }
	}

    public class DropDownParent
    {
		public long Number { get; set; }
        public long id { get; set; }
        public string text { get; set; }
    }

	public class DropDownParentList
	{
        public List<DropDownParent> items { get; set; }
        public int total_count { get; set; }
    }

}
