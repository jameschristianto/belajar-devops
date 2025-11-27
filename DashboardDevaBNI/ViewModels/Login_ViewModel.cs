namespace DashboardDevaBNI.ViewModels
{
    public class Login_ViewModel
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? HDUsername { get; set; }
        public string? HDPassword { get; set; }
        public string? TokenCapcha { get; set; }
        public string? OtpValid { get; set; }
        public string? KodeOtp { get; set; }
        public string? ObjectSend { get; set; }
        public string? TypeSend { get; set; }

    }

    public class DetailLogin_ViewModels
    {
        public long? UserId { get; set; }
        public string? Username { get; set; }
        public string? Fullname { get; set; }
        public string? Password { get; set; }
        public long? PegawaiId { get; set; }
        public string? PegawaiName { get; set; }
        public int?  RoleId { get; set; }
        public string? KodeRole { get; set; }
        public string? RoleName { get; set; }
        public string? Email { get; set; }
        public string? NoHP { get; set; }
		public string? IsOTP { get; set; }
		public string? IsVerifEmail { get; set; }
		public string? IsVerifNoTelp { get; set; }
		public bool? IsActive { get; set; }
        public bool? IsDeleted { get; set; }
        public bool? LDAPLogin { get; set; }
        public DateTime? LastLogin { get; set; }
        public DateTime? LastActive { get; set; }
        public bool? isLogout { get; set; }
        public long? UnitId { get; set; }
        public int? Counter { get; set; }
    }

	public class DetailLoginOTP_ViewModels
	{
		public int Kode { get; set; }
		public string OTPInput { get; set; }
		public string OTP { get; set; }
		public string UserId { get; set; }
		public string ipAddress { get; set; }
		public string Username { get; set; }
		public string ReqEncrypt { get; set; }
		public string PasswordLama { get; set; }
		public string PasswordInput { get; set; }
		public string PasswordKonfirmasi { get; set; }
		public string Password { get; set; }
		public string ConfirmPassword { get; set; }

	}

	public class SliderLogin_ViewModel
	{
		public int Kode { get; set; }
		public string Validate { get; set; }
        public string Pengumuman { get; set; }
		public string TypeSent { get; set; }
		public string ObjectSent { get; set; }
		public string Username { get; set; }
		public string ShowOtp { get; set; }
		public string ShowOtpEmail { get; set; }
		public string ShowOtpWa { get; set; }
		public List<SliderImage_ViewModels> ListSlider { get; set; }
	}


    public class Tutorial_ViewModels
    {
        public List<SliderImage_ViewModels> ListPdf { get; set; }
        public List<SliderImage_ViewModels> ListVideo { get; set; }
    }

    public class SliderImage_ViewModels
	{
		public int Id { get; set; }
		public string NameFile { get; set; }
		public string ImagePath { get; set; }

	}

	public class TokenVM
	{
		public string Username { get; set; }
		public DateTime DateTime { get; set; }
	}

	public class WhatsappResponse
	{
		public bool sent { get; set; }
		public string message { get; set; }
		public string description { get; set; }
		public string id { get; set; }
	}
}

