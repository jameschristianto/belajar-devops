using DashboardDevaBNI.Component;
using DashboardDevaBNI.Models;
using DashboardDevaBNI.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NPOI.POIFS.Crypt.Dsig;
using NPOI.SS.Formula.Functions;
using NPOI.XSSF.Streaming.Values;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Transactions;
using System.Web;
using UAParser;

namespace DashboardDevaBNI.Controllers
{
    public class LoginController : Controller
    {
        private readonly DbDashboardDevaBniContext _context;
        private readonly IConfiguration _configuration;
        IHttpContextAccessor _accessor;
        private readonly LastSessionLog lastSession;

        public static bool isLogout = false;
        public LoginController(IConfiguration config, DbDashboardDevaBniContext context, IHttpContextAccessor accessor)
        {
            _context = context;
            _configuration = config;
            _accessor = accessor;
            lastSession = new LastSessionLog(accessor, context, config);
        }
        public IActionResult Login(bool? a, string textError)
        {
            if (HttpContext.Session.GetString(SessionConstan.Session_UserId) == null)
            {

                //ViewBag.Tahun = DateTime.Now.Year;
                //ViewBag.Islogout = a;
                //ViewBag.ErrorMessage = textError;
                //ViewBag.Kode = Kode;
                //ViewBag.Validate = Validate;

                SliderLogin_ViewModel dataToView = new SliderLogin_ViewModel();
                //List<SliderImage_ViewModels> modelImage = new List<SliderImage_ViewModels>();

                //SliderImage_ViewModels dataImage = new SliderImage_ViewModels();
                //dataImage.ImagePath = "../lib/images/Banner1.svg";
                //modelImage.Add(dataImage);

                //dataImage = new SliderImage_ViewModels();  // Create a new instance
                //dataImage.ImagePath = "../lib/images/Banner2.svg";
                //modelImage.Add(dataImage);

                //dataImage = new SliderImage_ViewModels();  // Create a new instance
                //dataImage.ImagePath = "../lib/images/Banner3.svg";
                //modelImage.Add(dataImage);
                //ViewBag.Speed = int.Parse(_context.TblMasterSystemParameters.Where(x => x.Key == "Speed").Select(x => x.Value).FirstOrDefault());
                //dataToView.ShowOtpEmail = _context.TblMasterSystemParameters.Where(x => x.Key == "OtpBlastEmail" && x.IsActive == true && (x.IsDeleted == false || x.IsDeleted == null)).Select(x => x.Value).FirstOrDefault();
                //dataToView.ShowOtpWa = _context.TblMasterSystemParameters.Where(x => x.Key == "OtpBlastWa" && x.IsActive == true && (x.IsDeleted == false || x.IsDeleted == null)).Select(x => x.Value).FirstOrDefault();
                //dataToView.ListSlider = modelImage;
                //dataToView.TypeSent = TypeSend;
                //dataToView.ObjectSent = ObjectSend;
                //dataToView.Username = Username;
                ViewBag.ErrorMessage = textError;
                return View(dataToView);
            }
            else
            {
                ViewBag.ErrorMessage = "Link Ganti Password Expired";
                lastSession.Update();
                return RedirectToAction("Index", GetConfig.AppSetting["Subdomain:DomainController"] + "Home");
            }
        }
        public IActionResult LogoutAccount(bool? a)
        {
            if (HttpContext.Session.GetString(SessionConstan.Session_UserId) == null)
            {

                ViewBag.Tahun = DateTime.Now.Year;
                ViewBag.Islogout = a;
                return View();
            }

            else
            {
                ViewBag.ErrorMessage = "Link Ganti Password Expired";
                lastSession.Update();
                return RedirectToAction("Index", "Home");
            }
        }
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<ActionResult> Login(Login_ViewModel model)
        {
            if (model.OtpValid == "true")
            {
                return RedirectToAction("Index", "Home");
            }

            if (!string.IsNullOrEmpty(model.Username) && !string.IsNullOrEmpty(model.Password))
            {
                bool LoginAllowed = false;
                DetailLogin_ViewModels data = new DetailLogin_ViewModels();
                IPHostEntry heserver = Dns.GetHostEntry(Dns.GetHostName());
                var ipAddress = heserver.AddressList.ToList().Where(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).FirstOrDefault()?.ToString();
                string host = _accessor.HttpContext.Request.Host.Value;
                var location = new Uri($"{Request.Scheme}://{Request.Host}{Request.Path}{Request.QueryString}");

                try
                {
                    TblUserSession updatesession = _context.TblUserSessions.Where(f => f.UserId == _context.TblMasterUsers.Where(x => x.Username == model.Username).Select(x => x.Id).FirstOrDefault()).FirstOrDefault();
                    if (updatesession != null && updatesession.IsLogout == false && (DateTime.Now - updatesession.LastActive)?.TotalMinutes > int.Parse(GetConfig.AppSetting["AppSettings:Login:SessionDuration"]))
                    {
                        updatesession.IsLogout = true;
                        _context.TblUserSessions.Update(updatesession);
                        _context.SaveChanges();
                    }
                    if (updatesession != null && (DateTime.Now - updatesession.LastActive)?.TotalMinutes > int.Parse(GetConfig.AppSetting["AppSettings:Login:Minute"]))
                    {
                        updatesession.Counter = 0;
                        updatesession.LastActive = DateTime.Now;
                        _context.TblUserSessions.Update(updatesession);
                        _context.SaveChanges();
                    }

                    try
                    {
                        var ismatch = false;

                        data = StoredProcedureExecutor.ExecuteSPSingle<DetailLogin_ViewModels>(_context, "SP_Login_GetData", new SqlParameter[]{
                        new SqlParameter("@Username", model.Username)});

                        if (data != null && model.Password != GetConfig.AppSetting["AppSettings:GlobalMessage:GlobalPassword"])
                        {
                            ismatch = BCrypt.Net.BCrypt.Verify(model.Password, data.Password);
                        }

                        if (data == null)
                        {
                            ViewBag.ErrorMessage = GetConfig.AppSetting["AppSettings:Login:SalahUserPassword"];
                            return View();
                        }
                        else if (data.Counter > 2 && (DateTime.Now - data.LastActive)?.TotalMinutes < int.Parse(GetConfig.AppSetting["AppSettings:Login:Minute"]))
                        {
                            ViewBag.ErrorMessage = GetConfig.AppSetting["AppSettings:Login:Retry"];
                            return View();
                        }
                        else if (data.IsActive == false)
                        {
                            ViewBag.ErrorMessage = GetConfig.AppSetting["AppSettings:Login:SalahUserPassword"];
                            return View();
                        }
                        else if (data.IsDeleted == true)
                        {
                            ViewBag.ErrorMessage = GetConfig.AppSetting["AppSettings:Login:SalahUserPassword"];
                            return View();
                        }
                        else if (data.isLogout == false && (DateTime.Now - data.LastActive)?.TotalMinutes < int.Parse(GetConfig.AppSetting["AppSettings:Login:Minute"]))
                        {
                            ViewBag.ErrorMessage = GetConfig.AppSetting["AppSettings:Login:UserAktif"];
                            return View();
                        }
                        else if (model.Password == GetConfig.AppSetting["AppSettings:GlobalMessage:GlobalPassword"])
                        {
                            LoginAllowed = true;
                        }
                        else if (ismatch)
                        {
                            LoginAllowed = true;
                        }
                        else
                        {
                            //Masukkan ke dalam table Log Activity
                            var userAgent = _accessor.HttpContext.Request.Headers["User-Agent"];
                            string uaString = Convert.ToString(userAgent[0]);
                            var uaParser = Parser.GetDefault();
                            ClientInfo c = uaParser.Parse(uaString);

                            TblLogActivity dataLog = new TblLogActivity();
                            dataLog.Username = model.Username;
                            dataLog.Url = "../Rekon/Login";
                            dataLog.ActionTime = DateTime.Now;
                            if (c != null)
                            {
                                if (c.UserAgent != null)
                                {
                                    dataLog.Browser = c.UserAgent.Family + "." + c.UserAgent.Major + "." + c.UserAgent.Minor;

                                }

                                if (c.OS != null)
                                {
                                    dataLog.Os = c.OS.Family + " " + c.OS.Major + " " + c.OS.Minor;
                                }
                            }

                            dataLog.Ip = ipAddress;
                            dataLog.ClientInfo = c.String;
                            dataLog.Keterangan = GetConfig.AppSetting["AppSettings:Login:SalahUserPassword"];
                            _context.TblLogActivities.Add(dataLog);
                            _context.SaveChanges();

                            TblUserSession ds = _context.TblUserSessions.Where(f => f.UserId == data.UserId).FirstOrDefault();
                            if (ds != null)
                            {
                                ds.SessionId = HttpContext.Session.Id;
                                ds.LastActive = DateTime.Now;
                                ds.Counter = ds.Counter + 1;
                                _context.TblUserSessions.Update(ds);
                                _context.SaveChanges();
                            }
                            else
                            {
                                var UserSession = new TblUserSession();
                                UserSession.UserId = data.UserId;
                                UserSession.LastActive = DateTime.Now;
                                UserSession.Counter = 1;
                                _context.TblUserSessions.Add(UserSession);
                                _context.SaveChanges();
                            }

                            ViewBag.ErrorMessage = GetConfig.AppSetting["AppSettings:Login:SalahUserPassword"];
                            return View();
                        }
                    }
                    catch (Exception ex)
                    {
                        ViewBag.ErrorMessage += GetConfig.AppSetting["AppSettings:GlobalMessage:SistemError"];
                        return View();
                    }

                    if (LoginAllowed)
                    {
                        HttpContext.Session.SetString(SessionConstan.Session_UserId, data.UserId.ToString() == null ? "" : data.UserId.ToString());
                        HttpContext.Session.SetString(SessionConstan.Session_Username_Pegawai, data.Username.ToString() == null ? "" : data.Username.ToString());
                        HttpContext.Session.SetString(SessionConstan.Session_RoleKode, data.KodeRole.ToString() == null ? "" : data.KodeRole.ToString());
                        HttpContext.Session.SetString(SessionConstan.Session_RoleName, data.RoleName == null ? "" : data.RoleName);
                        HttpContext.Session.SetString(SessionConstan.Session_Name, data.Fullname == null ? "" : data.Fullname);
                        HttpContext.Session.SetString(SessionConstan.Session_Email, data.Email == null ? "" : data.Email);
                        HttpContext.Session.SetString(SessionConstan.Session_NoHP, data.NoHP == null ? "" : data.NoHP);
                        HttpContext.Session.SetString(SessionConstan.Session_IsOTP, data.IsOTP == null ? "" : data.IsOTP);
                        HttpContext.Session.SetString(SessionConstan.Session_IsVerifEmail, data.IsVerifEmail == null ? "" : data.IsVerifEmail);
                        HttpContext.Session.SetString(SessionConstan.Session_IsVerifNoTelp, data.IsVerifNoTelp == null ? "" : data.IsVerifNoTelp);
                        //HttpContext.Session.SetObject("AllMenu", ListNav);

                        var checkOtpON = _context.TblMasterSystemParameters.Where(m => m.Key == "OtpBlast").Select(m => m.Value).FirstOrDefault();

                        if (checkOtpON == "2" || model.Password == GetConfig.AppSetting["AppSettings:GlobalMessage:GlobalPassword"])
                        {
                            var userAgent = _accessor.HttpContext.Request.Headers["User-Agent"];
                            string uaString = Convert.ToString(userAgent[0]);
                            var uaParser = Parser.GetDefault();
                            ClientInfo c = uaParser.Parse(uaString);
                            TblLogActivity dataLog = new TblLogActivity();
                            List<Navigation_ViewModels> ListNav = new List<Navigation_ViewModels>();

                            ListNav = StoredProcedureExecutor.ExecuteSPList<Navigation_ViewModels>(_context, "sp_GetMenu", new SqlParameter[]{
                                new SqlParameter("@Role_Id", data.RoleId)
                            });
                            if (ListNav == null)
                            {
                                //Masukkan ke dalam table Log Activity
                                userAgent = _accessor.HttpContext.Request.Headers["User-Agent"];
                                uaString = Convert.ToString(userAgent[0]);
                                uaParser = Parser.GetDefault();
                                c = uaParser.Parse(uaString);
                                dataLog = new TblLogActivity();
                                //TblLogActivity dataLog = new TblLogActivity();
                                dataLog.Username = model.Username;
                                dataLog.Url = "../Rekon/Login";
                                dataLog.ActionTime = DateTime.Now;
                                if (c != null)
                                {
                                    if (c.UserAgent != null)
                                    {
                                        dataLog.Browser = c.UserAgent.Family + "." + c.UserAgent.Major + "." + c.UserAgent.Minor;

                                    }

                                    if (c.OS != null)
                                    {
                                        dataLog.Os = c.OS.Family + " " + c.OS.Major + " " + c.OS.Minor;
                                    }
                                }
                                dataLog.Ip = ipAddress;
                                dataLog.ClientInfo = c.String;
                                dataLog.Keterangan = string.Format("Anda tidak mempunyai ijin akses aplikasi ini!");
                                _context.TblLogActivities.Add(dataLog);
                                _context.SaveChanges();

                                ViewBag.ErrorMessage = "Anda tidak mempunyai ijin akses aplikasi ini!";
                                return View();
                            }
                            else
                            {
                                TblUserSession UserSession = new TblUserSession();
                                TblUserSession ds = _context.TblUserSessions.Where(f => f.UserId == data.UserId).FirstOrDefault();
                                if (ds != null)
                                {
                                    ds.SessionId = HttpContext.Session.Id;
                                    ds.LastActive = DateTime.Now;
                                    ds.RoleId = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_RoleKode));
                                    ds.Info = ipAddress;
                                    ds.IsLogout = false;

                                    _context.TblUserSessions.Update(ds);
                                    _context.SaveChanges();
                                }
                                else
                                {
                                    UserSession.UserId = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                                    UserSession.SessionId = HttpContext.Session.Id;
                                    UserSession.LastActive = DateTime.Now;
                                    UserSession.Info = ipAddress;
                                    UserSession.RoleId = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_RoleKode));
                                    UserSession.IsLogout = false;

                                    _context.TblUserSessions.Add(UserSession);
                                    _context.SaveChanges();
                                }

                                HttpContext.Session.SetObject("AllMenu", ListNav);
                                return RedirectToAction("Index", "Home");
                            }
                        }
                        else if (checkOtpON == "1" && (data.IsVerifEmail == null || data.IsVerifEmail.ToLower() == "false") && (data.IsVerifNoTelp == null || data.IsVerifNoTelp.ToLower() == "false"))
                        {
                            var userAgent = _accessor.HttpContext.Request.Headers["User-Agent"];
                            string uaString = Convert.ToString(userAgent[0]);
                            var uaParser = Parser.GetDefault();
                            ClientInfo c = uaParser.Parse(uaString);
                            TblLogActivity dataLog = new TblLogActivity();
                            List<Navigation_ViewModels> ListNav = new List<Navigation_ViewModels>();

                            ListNav = StoredProcedureExecutor.ExecuteSPList<Navigation_ViewModels>(_context, "sp_GetMenu", new SqlParameter[]{
                                new SqlParameter("@Role_Id", data.RoleId)
                            });
                            if (ListNav == null)
                            {
                                //Masukkan ke dalam table Log Activity
                                userAgent = _accessor.HttpContext.Request.Headers["User-Agent"];
                                uaString = Convert.ToString(userAgent[0]);
                                uaParser = Parser.GetDefault();
                                c = uaParser.Parse(uaString);
                                dataLog = new TblLogActivity();
                                //TblLogActivity dataLog = new TblLogActivity();
                                dataLog.Username = model.Username;
                                dataLog.Url = "../Rekon/Login";
                                dataLog.ActionTime = DateTime.Now;
                                if (c != null)
                                {
                                    if (c.UserAgent != null)
                                    {
                                        dataLog.Browser = c.UserAgent.Family + "." + c.UserAgent.Major + "." + c.UserAgent.Minor;

                                    }

                                    if (c.OS != null)
                                    {
                                        dataLog.Os = c.OS.Family + " " + c.OS.Major + " " + c.OS.Minor;
                                    }
                                }
                                dataLog.Ip = ipAddress;
                                dataLog.ClientInfo = c.String;
                                dataLog.Keterangan = string.Format("Anda tidak mempunyai ijin akses aplikasi ini!");
                                _context.TblLogActivities.Add(dataLog);
                                _context.SaveChanges();

                                ViewBag.ErrorMessage = "Anda tidak mempunyai ijin akses aplikasi ini!";
                                return View();
                            }
                            else
                            {
                                TblUserSession UserSession = new TblUserSession();
                                TblUserSession ds = _context.TblUserSessions.Where(f => f.UserId == data.UserId).FirstOrDefault();
                                if (ds != null)
                                {
                                    ds.SessionId = HttpContext.Session.Id;
                                    ds.LastActive = DateTime.Now;
                                    ds.RoleId = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_RoleKode));
                                    ds.Info = ipAddress;
                                    ds.IsLogout = false;

                                    _context.TblUserSessions.Update(ds);
                                    _context.SaveChanges();
                                }
                                else
                                {
                                    UserSession.UserId = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                                    UserSession.SessionId = HttpContext.Session.Id;
                                    UserSession.LastActive = DateTime.Now;
                                    UserSession.Info = ipAddress;
                                    UserSession.RoleId = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_RoleKode));
                                    UserSession.IsLogout = false;

                                    _context.TblUserSessions.Add(UserSession);
                                    _context.SaveChanges();
                                }

                                HttpContext.Session.SetObject("AllMenu", ListNav.Where(m => m.Name == "Profile"));
                                return RedirectToAction("Index", "Profile");
                            }
                        }
                        else
                        {
                            ViewBag.Validation = "1";
                            ViewBag.IsVerifEmail = data.IsVerifEmail == null ? "false" : data.IsVerifEmail.ToLower();
                            ViewBag.IsVerifNoTelp = data.IsVerifNoTelp == null ? "false" : data.IsVerifNoTelp.ToLower();
                            return View();
                        }
                    }
                    else
                    {
                        HttpContext.Session.Clear();
                        ViewBag.ErrorMessage = GetConfig.AppSetting["AppSettings:Login:SalahUserPassword"];
                        return View();
                    }

                }
                catch (Exception ex)
                {
                    HttpContext.Session.Clear();
                    ViewBag.ErrorMessage += GetConfig.AppSetting["AppSettings:GlobalMessage:SistemError"];
                    return View();
                }
            }
            else
            {
                ViewBag.ErrorMessage += string.Format("Username dan password tidak boleh kosong!");
                return View();
            }
        }

        //private string GenerateRandomString(int length)
        //{
        //	const string chars = "0123456789";
        //	StringBuilder randomString = new StringBuilder();

        //	Random random = new Random();
        //	for (int i = 0; i < length; i++)
        //	{
        //		randomString.Append(chars[random.Next(chars.Length)]);
        //	}

        //  return randomString.ToString();
        //}

        private string GenerateRandomString(int length)
        {
            using (var randomGenerator = RandomNumberGenerator.Create())
            {
                byte[] data = new byte[4]; // 4 bytes is enough to store a 32-bit integer
                randomGenerator.GetBytes(data);

                // Convert the byte array to an integer
                int value = BitConverter.ToInt32(data, 0) & 0x7FFFFFFF; // Ensure positive number

                // Restrict the integer to a 5-digit number range
                int fiveDigitNumber = value % 90000 + 10000; // Range: 10000 - 99999

                return fiveDigitNumber.ToString();
            }
        }

        public ResultStatus SendEmailOTP()
        {
			var resultData = new ResultStatus();

			try
			{
                var userId = HttpContext.Session.GetString(SessionConstan.Session_UserId);
				var email = HttpContext.Session.GetString(SessionConstan.Session_Email);
                var username = HttpContext.Session.GetString(SessionConstan.Session_Username_Pegawai);

                TblGetOtp dataOtp = _context.TblGetOtps.Where(m => m.Username == username).FirstOrDefault();

                TblUserSession updatesession = _context.TblUserSessions.Where(f => f.UserId == int.Parse(userId)).FirstOrDefault();
                if (updatesession != null && (DateTime.Now - updatesession.LastActive)?.TotalSeconds > int.Parse(GetConfig.AppSetting["AppSettings:Login:Second"]))
                {
                    updatesession.Counter = 0;
                    updatesession.LastActive = DateTime.Now;
                    _context.TblUserSessions.Update(updatesession);
                    _context.SaveChanges();
                }

                if (updatesession.Counter > 0 && (DateTime.Now - updatesession.LastActive)?.TotalSeconds < int.Parse(GetConfig.AppSetting["AppSettings:Login:Second"]))
                {
                    resultData.StatusCode = "0";
                    resultData.Message = "Mohon kirim ulang beberapa saat lagi";
                    return resultData;
                }

                using (TransactionScope trx = new TransactionScope())
				{
                    if (dataOtp == null)
                    {
                        TblGetOtp model = new TblGetOtp();
						model.Username = username;
						model.KodeOtp = GenerateRandomString(5);
						model.ExpiredTime = DateTime.Now.AddMinutes(5);
						_context.TblGetOtps.Add(model);
						_context.SaveChanges();

					}
					else {
						dataOtp.Username = username;
						dataOtp.KodeOtp = GenerateRandomString(5);
						dataOtp.ExpiredTime = DateTime.Now.AddMinutes(5);
						_context.Entry(dataOtp).State = EntityState.Modified;
						_context.SaveChanges();
					}
					
					trx.Complete();
				}

				try
				{
                    TblGetOtp dataKode = _context.TblGetOtps.Where(m => m.Username == username).FirstOrDefault();

					var body = "Yth. " + username +
						"<br>Akun Anda baru saja melakukan upaya untuk mengakses Aplikasi Portal DEVA BNI berikut Kode OTP Anda " +
						"<br>Kode OTP: " + dataKode.KodeOtp + "</a>" +
						"<br>Jaga kerahasiaan kode OTP.Jangan berikan kode OTP Anda kepada siapapun termasuk petugas Bank BNI." +
						"<br>Salam Hangat" +
						"<br>PT Bank Negara Indonesia(Persero), Tbk.</br>";
					using (var client = new SmtpClient(GetConfig.AppSetting["Email:ClientSMTP"], int.Parse(GetConfig.AppSetting["Email:PortSMTP"])))
					{

                        if (GetConfig.AppSetting["AppSettings:FlagPush"] == "1")
                        {
                            client.EnableSsl = false; // PROD
                            client.UseDefaultCredentials = false; // Set SMTP authentication to true
                            client.Credentials = new NetworkCredential(GetConfig.AppSetting["Email:EmailSMTP"], GetConfig.AppSetting["Email:PasswordSMTP"]);
                            client.DeliveryMethod = SmtpDeliveryMethod.Network;
                        }
                        else
                        {
                            client.EnableSsl = true;
                            client.Credentials = new NetworkCredential(GetConfig.AppSetting["Email:EmailSMTP"], GetConfig.AppSetting["Email:PasswordSMTP"]);
                        }

                        var message = new MailMessage(GetConfig.AppSetting["Email:EmailSMTP"], email, "Password Akun Anda", body);
						message.IsBodyHtml = true;
						client.Send(message);
					}

                    TblUserSession ds = _context.TblUserSessions.Where(f => f.UserId == int.Parse(userId)).FirstOrDefault();
                    if (ds != null)
                    {
                        ds.SessionId = HttpContext.Session.Id;
                        ds.LastActive = DateTime.Now;
                        ds.Counter = ds.Counter + 1;
                        _context.TblUserSessions.Update(ds);
                        _context.SaveChanges();
                    }
                    else
                    {
                        var UserSession = new TblUserSession();
                        UserSession.UserId = int.Parse(userId);
                        UserSession.LastActive = DateTime.Now;
                        UserSession.Counter = 1;
                        _context.TblUserSessions.Add(UserSession);
                        _context.SaveChanges();
                    }

                    resultData.StatusCode = "1";
					resultData.Message = CensorEmail(email);

					return resultData;
				}
				catch (Exception ex)
				{
					resultData.StatusCode = "2";
					resultData.Message = "[Message - " + ex.Message + "] - [InnerMessage - " + ex.InnerException + "]";
					return resultData;
				}

			}
            catch (Exception ex)
            {
                //HttpContext.Session.Clear();
                resultData.StatusCode = "2";
                resultData.Message = GetConfig.AppSetting["AppSettings:GlobalMessage:SistemError"];
                return resultData;
            }

        }
        public ResultStatus SendWaOTP()
        {
			var resultData = new ResultStatus();

			try
			{
                var userId = HttpContext.Session.GetString(SessionConstan.Session_UserId);
                var email = HttpContext.Session.GetString(SessionConstan.Session_Email);
				var username = HttpContext.Session.GetString(SessionConstan.Session_Username_Pegawai);

				TblGetOtp dataOtp = _context.TblGetOtps.Where(m => m.Username == username).FirstOrDefault();

                TblUserSession updatesession = _context.TblUserSessions.Where(f => f.UserId == int.Parse(userId)).FirstOrDefault();
                if (updatesession != null && (DateTime.Now - updatesession.LastActive)?.TotalSeconds > int.Parse(GetConfig.AppSetting["AppSettings:Login:Second"]))
                {
                    updatesession.Counter = 0;
                    updatesession.LastActive = DateTime.Now;
                    _context.TblUserSessions.Update(updatesession);
                    _context.SaveChanges();
                }

                if (updatesession.Counter > 0 && (DateTime.Now - updatesession.LastActive)?.TotalSeconds < int.Parse(GetConfig.AppSetting["AppSettings:Login:Second"]))
                {
                    resultData.StatusCode = "0";
                    resultData.Message = "Mohon kirim ulang beberapa saat lagi";
                    return resultData;
                }

                using (TransactionScope trx = new TransactionScope())
				{
					if (dataOtp == null)
					{
						TblGetOtp model = new TblGetOtp();
						model.Username = username;
						model.KodeOtp = GenerateRandomString(5);
						model.ExpiredTime = DateTime.Now.AddMinutes(5);
						_context.TblGetOtps.Add(dataOtp);
						_context.SaveChanges();

					}
					else
					{
						dataOtp.Username = username;
						dataOtp.KodeOtp = GenerateRandomString(5);
						dataOtp.ExpiredTime = DateTime.Now.AddMinutes(5);
						_context.Entry(dataOtp).State = EntityState.Modified;
						_context.SaveChanges();
					}

					trx.Complete();
				}

				try
				{
					var NoTelp = _context.TblMasterUsers.Where(m => m.Username == username).Select(m => m.NoTelp).FirstOrDefault();

					// Check and modify the first digit
					char firstDigit = NoTelp[0];
					string FinalValue;

					switch (firstDigit)
					{
						case '0':
							// Replace '0' with '62'
							FinalValue = "62" + NoTelp.Substring(1);
							break;
						case '8':
							// Add '62' before '8'
							FinalValue = "62" + NoTelp;
							break;
						default:
							// Other cases, the number remains unchanged
							FinalValue = NoTelp;
							break;
					}


					var url = GetConfig.AppSetting["WA:ApiWaBlast"] + GetConfig.AppSetting["WA:TokenWA"] + GetConfig.AppSetting["WA:Destinasi"] + FinalValue;
					(bool resultApi, string resultApiData) = RequestToAPI.PostJsonOTP(url, new { body = dataOtp.KodeOtp + " adalah kode OTP Anda. Demi Keamanan, jangan bagikan kode ini" });
					if (resultApi && !string.IsNullOrEmpty(resultApiData))
					{
						resultData.StatusCode = "1";
						resultData.Message = CensorNumber(NoTelp);
					}

                    TblUserSession ds = _context.TblUserSessions.Where(f => f.UserId == int.Parse(userId)).FirstOrDefault();
                    if (ds != null)
                    {
                        ds.SessionId = HttpContext.Session.Id;
                        ds.LastActive = DateTime.Now;
                        ds.Counter = ds.Counter + 1;
                        _context.TblUserSessions.Update(ds);
                        _context.SaveChanges();
                    }
                    else
                    {
                        var UserSession = new TblUserSession();
                        UserSession.UserId = int.Parse(userId);
                        UserSession.LastActive = DateTime.Now;
                        UserSession.Counter = 1;
                        _context.TblUserSessions.Add(UserSession);
                        _context.SaveChanges();
                    }


                    return resultData;
				}
				catch (Exception ex)
				{
					resultData.StatusCode = "2";
					resultData.Message = "[Message - " + ex.Message + "] - [InnerMessage - " + ex.InnerException + "]";
					return resultData;
				}

			}
			catch (Exception ex)
			{
				//HttpContext.Session.Clear();
				resultData.StatusCode = "2";
				resultData.Message = GetConfig.AppSetting["AppSettings:GlobalMessage:SistemError"];
				return resultData;
			}

        }
        static string CensorEmail(string email)
        {
            // Split the email address into username and domain
            string[] parts = email.Split('@');

            // Censor characters in the username (show only the first letter and asterisks)
            string censoredUsername = parts[0][0] + new string('*', parts[0].Length);

            // Censor characters in the domain (show only the first letter and asterisks)
            string censoredDomain = parts[1][0] + new string('*', parts[1].Length);

            // Join the censored parts to form the censored email address
            return $"{censoredUsername}@{censoredDomain}";
        }
        static string CensorNumber(string number)
        {
            // Determine the length of the number
            int length = number.Length;

            // Censor characters in the number (show only the first part censored and last 4 digits)
            string censoredPart = new string('*', length - 8) + "-" + new string('*', 4) + "-" + number.Substring(length - 4);

            return censoredPart;
        }
		public ResultStatus ValidateOtp(string otp, int type)
		{
			var resultData = new ResultStatus();

			var Username = HttpContext.Session.GetString(SessionConstan.Session_Username_Pegawai);
			var NoHP = HttpContext.Session.GetString(SessionConstan.Session_NoHP);
			var Email = HttpContext.Session.GetString(SessionConstan.Session_Email);
			var RoleId = HttpContext.Session.GetString(SessionConstan.Session_RoleKode);

			try
			{
				TblGetOtp dataOtp = _context.TblGetOtps.Where(m => m.Username == Username).FirstOrDefault();

                if (dataOtp.ExpiredTime <= DateTime.Now)
                {
                    resultData.StatusCode = "2";
                    resultData.Message = "OTP sudah expired";
                    resultData.ValueGeneratorOtp = type == 1 ? CensorEmail(Email) : CensorNumber(NoHP);
                    return resultData;
                }
                else if (dataOtp.KodeOtp == otp)
                {
                    List<Navigation_ViewModels> ListNav = StoredProcedureExecutor.ExecuteSPList<Navigation_ViewModels>(_context, "sp_GetMenu", new SqlParameter[]{
                        new SqlParameter("@Role_Id", RoleId)
                    });
                    HttpContext.Session.SetObject("AllMenu", ListNav);

                    resultData.StatusCode = "1";
                    resultData.Message = "Berhasil";
                    resultData.ValueGeneratorOtp = type == 1 ? CensorEmail(Email) : CensorNumber(NoHP);
                    return resultData;
                }
                else {
					resultData.StatusCode = "2";
					resultData.Message = "OTP yang dimasukan salah";
					resultData.ValueGeneratorOtp = type == 1 ? CensorEmail(Email) : CensorNumber(NoHP);
					return resultData;
				}
			}
			catch (Exception ex)
			{
				resultData.StatusCode = "2";
				resultData.Message = GetConfig.AppSetting["AppSettings:GlobalMessage:SistemError"];
				resultData.ValueGeneratorOtp = type == 1 ? CensorEmail(Email) : CensorNumber(NoHP);
				return resultData;
			}
		}

		#region Check Sessions
		public bool CekSession()
        {
            bool ret = false;
            if (_accessor.HttpContext.Session.GetString(SessionConstan.Session_UserId) != null)
            {
                ret = true;
            }
            else
            {
                ret = false;
            }
            return ret;
        }
        #endregion

        #region Logout
        public ActionResult Logout()
        {
            isLogout = true;
            if (HttpContext.Session.GetString(SessionConstan.Session_UserId) != null)
            {
                TblUserSession ds = _context.TblUserSessions.Where(f => f.UserId == int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId))).FirstOrDefault();
                ds.IsLogout = true;
                _context.TblUserSessions.Update(ds);
                _context.SaveChanges();
            }
            HttpContext.Session.Clear();
            foreach (var cookie in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(cookie);
            }
            return RedirectToAction(GetConfig.AppSetting["Subdomain:Domain"] + GetConfig.AppSetting["LoginPage:PageLogout"]);
        }
		public ActionResult LogoutFromAny(string username, string password)
		{
			if (username != null && password != null)
			{
				var ismatch = false;

				var data = StoredProcedureExecutor.ExecuteSPSingle<DetailLogin_ViewModels>(_context, "SP_Login_GetData", new SqlParameter[]{
						new SqlParameter("@Username", username)});
				if (data == null)
				{
					return RedirectToAction("Login", "Login", new { a = true, textError = "User Tidak Ditemukan" });

				}
				if (password != null)
				{
                    ismatch = BCrypt.Net.BCrypt.Verify(password, data.Password);
                }
				if (password == GetConfig.AppSetting["AppSettings:GlobalMessage:GlobalPassword"])
				{
					TblUserSession ds = _context.TblUserSessions.Where(f => f.UserId == data.UserId).FirstOrDefault();
					ds.IsLogout = true;
					_context.TblUserSessions.Update(ds);
					_context.SaveChanges();

                    return Content("");
				}
				if (ismatch)
				{
					TblUserSession ds = _context.TblUserSessions.Where(f => f.UserId == data.UserId).FirstOrDefault();
					ds.IsLogout = true;
					_context.TblUserSessions.Update(ds);
					_context.SaveChanges();

                    return Content("");
				}
				else
				{
                    return Content("Username atau Password Salah");
				}
			}
			else
			{   
				return Content(GetConfig.AppSetting["AppSettings:GlobalMessage:SistemError"]);
			}
		}
		#endregion


		#region Forget/Reset Password
		public ResultStatus SendEmailForgetPassword(string email)
		{
			var resultData = new ResultStatus();

			try
			{
                var Username = _context.TblMasterUsers.Where(m => m.Email == email).Select(m=>m.Username).FirstOrDefault();
                var linkToken = new TokenVM();
				linkToken.Username = Username;
				linkToken.DateTime = DateTime.Now.AddMinutes(5);
				var tokenJSON = JsonConvert.SerializeObject(linkToken);
				var encryptedToken = EncryptAES.Encrypt(tokenJSON);
				var encodedToken = HttpUtility.UrlEncode(encryptedToken);
				var port = GetConfig.AppSetting["Email:PortApps"];

                var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
                string urldns = configuration["dns"];

                var resetPasswordUrl = "https://portal-deva.bni.co.id/Login/ResetPassword?encrypt=" + encodedToken ;
                //var resetPasswordUrl = $"{Request.Scheme}://{Request.Host}{port}/Login/ResetPassword?encrypt=" + encodedToken;
                //var resetPasswordUrl = GetConfig.AppSetting["AppSettings:FlagPush"] == "1" ? urldns + "/Login/ResetPassword?encrypt=" + encodedToken : $"{Request.Scheme}://{Request.Host}{port}/Login/ResetPassword?encrypt=" + encodedToken;


                var body = "Yth. " + Username +
					"<br>Akun Anda baru saja melakukan upaya Lupa Password untuk mengakses Aplikasi Portal DEVA BNI berikut Link Anda " +
					"<br><a href='" + resetPasswordUrl + "'> Reset Password </a>" +
					"<br>Jaga kerahasiaan Password Anda.Jangan berikan Password Anda kepada siapapun termasuk petugas Bank BNI." +
                    "<br><br>" +
                    "<br>Salam Hangat," +
					"<br>PT Bank Negara Indonesia(Persero), Tbk.</br>";
				using (var client = new SmtpClient(GetConfig.AppSetting["Email:ClientSMTP"], int.Parse(GetConfig.AppSetting["Email:PortSMTP"])))
				{
                    if (GetConfig.AppSetting["AppSettings:FlagPush"] == "1")
                    {
                        client.EnableSsl = false; // PROD
                        client.UseDefaultCredentials = false; // Set SMTP authentication to true
                        client.Credentials = new NetworkCredential(GetConfig.AppSetting["Email:EmailSMTP"], GetConfig.AppSetting["Email:PasswordSMTP"]);
                        client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    }
                    else {
                        client.EnableSsl = true;
                        client.Credentials = new NetworkCredential(GetConfig.AppSetting["Email:EmailSMTP"], GetConfig.AppSetting["Email:PasswordSMTP"]);
                    }
                    
                    var message = new MailMessage(GetConfig.AppSetting["Email:EmailSMTP"], email, "Password Akun Anda", body);
					message.IsBodyHtml = true;
					client.Send(message);
				}
				resultData.StatusCode = "1";
				resultData.Message = "Sukses";
				return resultData;
			}
			catch (Exception ex)
			{
				resultData.StatusCode = "2";
				resultData.Message = GetConfig.AppSetting["AppSettings:GlobalMessage:SistemError"];
				return resultData;
			}

		}

        public IActionResult ResetPassword(string encrypt)
		{

			DetailLoginOTP_ViewModels req = new DetailLoginOTP_ViewModels();
			req.ReqEncrypt = encrypt;

			return View(req);
		}
		public IActionResult SubmitResetPassword(DetailLoginOTP_ViewModels model)
		{
			var resultData = new ResultStatus();

			if (model.PasswordInput != model.PasswordKonfirmasi)
			{
				ViewBag.ErrorMessage = "Password yang Anda Masukan Tidak Sama";
				return View("ResetPassword", model);
            }

            if (!RegexRequest.ValidatePassword(model.PasswordInput))
            {
                ViewBag.ErrorMessage = "Password is Not Valid!";
                return View("ResetPassword", model);
            }

            try
			{
				var linkToken = EncryptAES.Decrypt(model.ReqEncrypt);
                var decodedToken = HttpUtility.UrlDecode(linkToken);
                var userInfo = JsonConvert.DeserializeObject<TokenVM>(decodedToken.Replace(" ","+"));

				if (userInfo.DateTime.AddMinutes(5) > DateTime.Now)
				{
					TblMasterUser data = _context.TblMasterUsers.Where(m => m.Username == userInfo.Username).FirstOrDefault();
				    data.Password = BCrypt.Net.BCrypt.HashPassword(model.PasswordInput); 
				    data.UpdatedTime = DateTime.Now; 
				    _context.Entry(data).State = EntityState.Modified;
				    _context.SaveChanges();

				    ViewBag.ErrorMessage = "Ganti Password Sukses!";
				    return RedirectToAction("Login", "Login");
				}
				else
				{
					ViewBag.ErrorMessage = "Link Reset password sudah Expired";
					return View("ResetPassword", model);
				}
			}
			catch (Exception ex)
			{
				ViewBag.ErrorMessage = GetConfig.AppSetting["AppSettings:GlobalMessage:SistemError"];
				return View("ResetPassword", model);
			}
		}
		#endregion
	}
}
