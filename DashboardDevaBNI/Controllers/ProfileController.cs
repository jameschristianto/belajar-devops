using DashboardDevaBNI.Component;
using DashboardDevaBNI.Models;
using DashboardDevaBNI.ViewModels;
using DinkToPdf.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NPOI.POIFS.Crypt.Dsig;

namespace DashboardDevaBNI.Controllers
{
    public class ProfileController : Controller
    {
        private readonly DbDashboardDevaBniContext _context;
        private readonly IConfiguration _configuration;
        private readonly IConverter _converter;
        private readonly LastSessionLog lastSession;
        private readonly AccessSecurity accessSecurity;
        public ProfileController(IConfiguration config, DbDashboardDevaBniContext context, IHttpContextAccessor accessor)
        {
            _context = context;
            _configuration = config;
            lastSession = new LastSessionLog(accessor, context, config);
            accessSecurity = new AccessSecurity(accessor, context, config);
        }

        public IActionResult Index()
        {
            if (!lastSession.Update())
            {
                return RedirectToAction("Login", "Login", new { a = true });
            }
            var location = new Uri($"{Request.Scheme}://{Request.Host}{Request.Path}");
            string Path = location.AbsolutePath;

            ViewBag.CurrentPath = Path;
            return View();
        }
        public IActionResult DataProfile()
        {
            if (!lastSession.Update())
            {
                return RedirectToAction("Login", "Login", new { a = true });
            }

            var resultData = new ResultStatusDataString<Profile_ViewModels>();
            
            try
            {
                var UserId = HttpContext.Session.GetString(SessionConstan.Session_UserId);
                TblMasterUser tblMasterUser = _context.TblMasterUsers.Where(m => m.Id == int.Parse(UserId)).FirstOrDefault();
                Profile_ViewModels data = new Profile_ViewModels();
                data.Username = tblMasterUser.Username;
                data.RoleName = _context.TblMasterRoles.Where(m => m.Id == tblMasterUser.RoleId).Select(m=>m.Name).FirstOrDefault();
                data.Name = tblMasterUser.Fullname;
                data.Email = tblMasterUser.Email;
                data.NoHp = tblMasterUser.NoTelp;
                data.IsVerifEmail = tblMasterUser.IsVerifEmail == "true" ? true : false;
                data.IsVerifNoTelp = tblMasterUser.IsVerifNoTelp == "true" ? true : false;

                resultData.StatusCode = "1";
                resultData.Message = "Sukses";
                resultData.Data = data;

                return Json(resultData);
            }
            catch (Exception ex)
            {
                resultData.StatusCode = "2";
                resultData.Message = GetConfig.AppSetting["GlobalMessage:SistemError"];

                return Json(resultData);
            }
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
                    if (type == 1)
                    {
                        HttpContext.Session.SetString(SessionConstan.Session_IsVerifEmail, "true");
                        TblMasterUser TblMasterUser = _context.TblMasterUsers.Where(m => m.Username == Username).FirstOrDefault();
                        TblMasterUser.IsVerifEmail = "true";
                        _context.Entry(TblMasterUser).State = EntityState.Modified;
                        _context.SaveChanges();
                    }
                    else {
                        HttpContext.Session.SetString(SessionConstan.Session_IsVerifNoTelp, "true");
                        TblMasterUser TblMasterUser = _context.TblMasterUsers.Where(m => m.Username == Username).FirstOrDefault();
                        TblMasterUser.IsVerifNoTelp = "true";
                        _context.Entry(TblMasterUser).State = EntityState.Modified;
                        _context.SaveChanges();
                    }
                    resultData.StatusCode = "1";
                    resultData.Message = "Berhasil";
                    resultData.ValueGeneratorOtp = type == 1 ? CensorEmail(Email) : CensorNumber(NoHP);
                    return resultData;
                }
                else
                {
                    resultData.StatusCode = "2";
                    resultData.Message = "OTP yang dimasukan salah";
                    resultData.ValueGeneratorOtp = type == 1 ? CensorEmail(Email) : CensorNumber(NoHP);
                    return resultData;
                }
            }
            catch (Exception ex)
            {
                resultData.StatusCode = "2";
                resultData.Message = GetConfig.AppSetting["GlobalMessage:SistemError"];
                resultData.ValueGeneratorOtp = type == 1 ? CensorEmail(Email) : CensorNumber(NoHP);
                return resultData;
            }
        }
        public ResultStatus ValidatePassword(string password)
        {
            var resultData = new ResultStatus();
            var UserId = HttpContext.Session.GetString(SessionConstan.Session_UserId);
            try
            {
                TblMasterUser tblMasterUser = _context.TblMasterUsers.Where(m => m.Id == int.Parse(UserId)).FirstOrDefault();
                var ismatch = BCrypt.Net.BCrypt.Verify(password, tblMasterUser.Password);

                if (ismatch)
                {
                    resultData.StatusCode = "1";
                    resultData.Message = "Sukses";
                    return resultData;
                }
                else {
                    resultData.StatusCode = "2";
                    resultData.Message = "Password yang di masukan salah";
                    return resultData;
                }
            }
            catch (Exception ex)
            {
                resultData.StatusCode = "2";
                resultData.Message = GetConfig.AppSetting["GlobalMessage:SistemError"];
                return resultData;
            }
        }
        public ResultStatus UpdateProfile(string value, int changeWhat, int typeOtp)
        {
            var resultData = new ResultStatus();
            var UserId = HttpContext.Session.GetString(SessionConstan.Session_UserId);

            try
            {
                TblMasterUser tblMasterUser = _context.TblMasterUsers.Where(m => m.Id == int.Parse(UserId)).FirstOrDefault();

                var model = new { email = "", noHP = "", password = "", otpType = 0 };
                if (changeWhat == 1)
                {
                    tblMasterUser.Email = value;
                    tblMasterUser.UpdatedById = int.Parse(UserId);
                    tblMasterUser.UpdatedTime = DateTime.Now;
                    _context.Entry(tblMasterUser).State = EntityState.Modified;
                    _context.SaveChanges();
                    HttpContext.Session.SetString(SessionConstan.Session_Email, value);
                }
                else if (changeWhat == 2)
                {
                    tblMasterUser.NoTelp = value;
                    tblMasterUser.UpdatedById = int.Parse(UserId);
                    tblMasterUser.UpdatedTime = DateTime.Now;
                    _context.Entry(tblMasterUser).State = EntityState.Modified;
                    _context.SaveChanges();
                    HttpContext.Session.SetString(SessionConstan.Session_NoHP, value);
                }
                else if (changeWhat == 3)
                {
                    if (!RegexRequest.ValidatePassword(value))
                    {
                        resultData.StatusCode = "2";
                        resultData.Message = "Password is Not Valid!";
                        return resultData;
                    }
                    tblMasterUser.Password = BCrypt.Net.BCrypt.HashPassword(value);
                    tblMasterUser.UpdatedById = int.Parse(UserId);
                    tblMasterUser.UpdatedTime = DateTime.Now;
                    _context.Entry(tblMasterUser).State = EntityState.Modified;
                    _context.SaveChanges();
                }

                resultData.StatusCode = "1";
                resultData.Message = "Sukses";
                return resultData;
            }
            catch (Exception ex)
            {
                resultData.StatusCode = "2";
                resultData.Message = GetConfig.AppSetting["GlobalMessage:SistemError"];
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
    }
}
