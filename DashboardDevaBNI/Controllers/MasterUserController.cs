using DashboardDevaBNI.Component;
using DashboardDevaBNI.Models;
using DashboardDevaBNI.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Globalization;
using System.Net.Mail;
using System.Net;
using System.Transactions;
using System.Web;
using System.Data;
using NPOI.POIFS.Crypt.Dsig;

namespace DashboardDevaBNI.Controllers
{
	public class MasterUserController : Controller
	{
		private readonly DbDashboardDevaBniContext _context;
		private readonly IConfiguration _configuration;
		private readonly LastSessionLog lastSession;
		private readonly AccessSecurity accessSecurity;
		public MasterUserController(IConfiguration config, DbDashboardDevaBniContext context, IHttpContextAccessor accessor)
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
				return RedirectToAction("Login", GetConfig.AppSetting["Subdomain:DomainController"] + "Login", new { a = true });
			}
			var location = new Uri($"{Request.Scheme}://{Request.Host}{Request.Path}");
			string Path = location.AbsolutePath;
			if (!accessSecurity.IsGetAccess(".." + Path))
			{
				return RedirectToAction("NotAccess", GetConfig.AppSetting["Subdomain:DomainController"] + "Error");
			}

			ViewBag.CurrentPath = Path;
			ViewBag.Pengumuman = _context.TblMasterSystemParameters.Where(x => x.Key == "Pengumuman").Select(x => x.Value).FirstOrDefault();
			ViewBag.Speed = int.Parse(_context.TblMasterSystemParameters.Where(x => x.Key == "Speed").Select(x => x.Value).FirstOrDefault());
			return View();
		}

		#region LoadData
		[HttpPost]
		public IActionResult LoadData()
		{
			if (!lastSession.Update())
			{
				return RedirectToAction("Login", GetConfig.AppSetting["Subdomain:DomainController"] + "Login", new { a = true });
			}
			var location = new Uri($"{Request.Scheme}://{Request.Host}{Request.Path}");
			string Path = location.AbsolutePath.Replace(this.ControllerContext.RouteData.Values["action"].ToString(), "Index");
			if (!accessSecurity.IsGetAccess(".." + Path))
			{
				return RedirectToAction("NotAccess", "Error");
			}

			try
			{
				var dict = Request.Form.ToDictionary(x => x.Key, x => x.Value.ToString());

				var draw = dict["draw"];

				//Untuk mengetahui info paging dari datatable
				var start = dict["start"];
				var length = dict["length"];

				//Server side datatable hanya support untuk mendapatkan data mulai ke berapa, untuk mengirim row ke berapa
				//Kita perlu membuat logika sendiri
				var pageNumber = (int.Parse(start) / int.Parse(length)) + 1;

				//Untuk mengetahui info order column datatable
				var sortColumn = dict["columns[" + dict["order[0][column]"] + "][data]"];
				var sortColumnDir = dict["order[0][dir]"];
				var NameSearchParam = dict["columns[2][search][value]"];
				var UsernameSearchParam = dict["columns[3][search][value]"];


				//Untuk mengetahui info jumlah page dan total skip data
				int pageSize = length != null ? Convert.ToInt32(length) : 0;
				int skip = start != null ? Convert.ToInt32(start) : 0;
				int recordsTotal = 0;
				List<MasterUser_ViewModel> list = new List<MasterUser_ViewModel>();


				list = StoredProcedureExecutor.ExecuteSPList<MasterUser_ViewModel>(_context, "sp_Load_MasterUser_View", new SqlParameter[]{
						new SqlParameter("@Name", NameSearchParam),
						new SqlParameter("@Username", UsernameSearchParam),
						new SqlParameter("@sortColumn", sortColumn),
						new SqlParameter("@sortColumnDir", sortColumnDir),
						new SqlParameter("@PageNumber", pageNumber),
						new SqlParameter("@RowsPage", pageSize),
				});


				recordsTotal = StoredProcedureExecutor.ExecuteScalarInt(_context, "sp_Load_MasterUser_Count", new SqlParameter[]{
						new SqlParameter("@Name", NameSearchParam),
						new SqlParameter("@Username", UsernameSearchParam)
				});

				if (list == null)
				{
					list = new List<MasterUser_ViewModel>();
					recordsTotal = 0;
				}

				return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = list });
			}
			catch (Exception Ex)
			{
				throw;
			}
		}
		#endregion

		#region Create
		public ActionResult Create()
		{
			if (!lastSession.Update())
			{
				return RedirectToAction("Login", GetConfig.AppSetting["Subdomain:DomainController"] + "Login", new { a = true });
			}
			var location = new Uri($"{Request.Scheme}://{Request.Host}{Request.Path}");
			string Path = location.AbsolutePath.Replace(this.ControllerContext.RouteData.Values["action"].ToString(), "Index");
			if (!accessSecurity.IsGetAccess(".." + Path))
			{
				return RedirectToAction("NotAccess", "Error");
			}

			ViewBag.RoleId = new SelectList("", "");
			return PartialView("_Create");
		}

		[ValidateAntiForgeryToken]
		[HttpPost]
		public async Task<ActionResult> SubmitCreate(TblMasterUser model)
		{
			if (!lastSession.Update())
			{
				return RedirectToAction("Login", GetConfig.AppSetting["Subdomain:DomainController"] + "Login", new { a = true });
			}
			var location = new Uri($"{Request.Scheme}://{Request.Host}{Request.Path}");
			string Path = location.AbsolutePath.Replace(this.ControllerContext.RouteData.Values["action"].ToString(), "Index");
			if (!accessSecurity.IsGetAccess(".." + Path))
			{
				return RedirectToAction("NotAccess", "Error");
			}

			try
			{
				var Password = model.Password;

				var regex = await RegexRequest.RegexValidation(model);
				if (!regex)
				{
					return Content("Bad Request!");
				}

				if (!RegexRequest.ValidatePassword(model.Password))
				{
					return Content("Password is Not Valid!");
				}
				using (TransactionScope trx = new TransactionScope())
				{
					TblMasterUser dataUserName = _context.TblMasterUsers.Where(m => m.Username == model.Username && m.IsActive == true && m.IsDeleted != true).FirstOrDefault();
					if (dataUserName != null)
					{
						return Content(dataUserName.Username + " Sudah terdaftar");
					}
					TblMasterUser dataEmail = _context.TblMasterUsers.Where(m => m.Email == model.Email && m.IsActive == true && m.IsDeleted != true).FirstOrDefault();
					if (dataEmail != null)
					{
						return Content(dataEmail.Email + " Sudah terdaftar");
					}
                    TblMasterUser dataWa = _context.TblMasterUsers.Where(m => m.NoTelp == model.NoTelp && m.IsActive == true && m.IsDeleted != true).FirstOrDefault();
                    if (dataEmail != null)
                    {
                        return Content(dataEmail.NoTelp + " Sudah terdaftar");
                    }
                    model.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);
					model.IsActive = true;
					model.IsDeleted = false;
					model.CreatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
					model.IsVerifEmail = "false";
					model.IsVerifNoTelp = "false";
					model.CreatedTime = DateTime.Now;

                    var UserId = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                    var RoleKode = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_RoleKode));

                    if (RoleKode == 1)
                    {
                        _context.TblMasterUsers.Add(model);
                        _context.SaveChanges();
                    }
                    else
                    {
                        if (model.RoleId == 1)
                        {
                            return Content(GetConfig.AppSetting["SistemError"]);
                        }
                        else
                        {
                            _context.TblMasterUsers.Add(model);
                            _context.SaveChanges();
                        }
                    }

					trx.Complete();
				}
				SendEmailConfig(model, Password);
				return Content("");

			}
			catch (Exception Ex)
			{
				return Content(GetConfig.AppSetting["SistemError"]);
			}
		}

		public ResultStatus SendEmailConfig(TblMasterUser model, string Password)
		{
			var result = new ResultStatus();
			try
			{
                var location = new Uri($"{Request.Scheme}://{Request.Host}{Request.Path}");
                string Path = location.AbsolutePath;

                var linkweb = "https://portal-deva.bni.co.id/";

				var body = "Kepada Yth Bapak/Ibu," +
					"<br>Berikut Username dan Password yang bisa digunakan untuk akses Aplikasi Portal DEVA BNI <a href='" + linkweb + "'> Klik disini </a>" +
					"<br><br>" +
					"Username: " + model.Username +
					"<br>Password: " + Password +
					"<br><br>" +
					"<br>Harap tidak memberikan username dan password tersebut kepada pihak yang tidak berkepentingan."+
					"<br><br>" +
                    "<br>Terima Kasih";
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

					var message = new MailMessage(GetConfig.AppSetting["Email:EmailSMTP"], model.Email, "Account Created", body);
					message.IsBodyHtml = true;
					client.Send(message);
				}
				result.StatusCode = "1";
				result.Message = "Sukses";

				return result;
			}
			catch (Exception ex)
			{
				result.StatusCode = "2";
				result.Message = "[Message - " + ex.Message + "] - [InnerMessage - " + ex.InnerException + "]";
				return result;
			}
		}
		#endregion

		#region Edit
		public ActionResult Edit(int id)
		{
			if (!lastSession.Update())
			{
				return RedirectToAction("Login", GetConfig.AppSetting["Subdomain:DomainController"] + "Login", new { a = true });
			}
			var location = new Uri($"{Request.Scheme}://{Request.Host}{Request.Path}");
			string Path = location.AbsolutePath.Replace(this.ControllerContext.RouteData.Values["action"].ToString(), "Index");
			if (!accessSecurity.IsGetAccess(".." + Path))
			{
				return RedirectToAction("NotAccess", "Error");
			}

            TblMasterUser data = _context.TblMasterUsers.Where(m => m.Id == id).FirstOrDefault();
            if (data == null)
            {
                data = new TblMasterUser();
            }
            else
            {
                if (data.RoleId != null)
                {
                    ViewBag.RoleId = new SelectList(Utility.SelectDataRole(int.Parse(data.RoleId.ToString()), _context), "id", "text", data.RoleId);
                }
                else
                {
                    ViewBag.RoleId = new SelectList("", "");
                }
            }

            var UserId = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
            var RoleKode = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_RoleKode));

            if (RoleKode == 1)
			{
                return PartialView("_Edit", data);
            }
            else { 
				if(data.RoleId == 1)
				{ 
					return PartialView("_View", data);
                }
                else
				{
                    return PartialView("_Edit", data);
                }
            }
		}

		[ValidateAntiForgeryToken]
		[HttpPost]
		public async Task<ActionResult> SubmitEdit(TblMasterUser model)
		{
			if (!lastSession.Update())
			{
				return RedirectToAction("Login", GetConfig.AppSetting["Subdomain:DomainController"] + "Login", new { a = true });
			}
			var location = new Uri($"{Request.Scheme}://{Request.Host}{Request.Path}");
			string Path = location.AbsolutePath.Replace(this.ControllerContext.RouteData.Values["action"].ToString(), "Index");
			if (!accessSecurity.IsGetAccess(".." + Path))
			{
				return RedirectToAction("NotAccess", "Error");
			}

			try
			{

				var regex = await RegexRequest.RegexValidation(model);
				if (!regex)
				{
					return Content("Bad Request!");
				}

				// if (!RegexRequest.ValidatePassword(model.Password))
				// {
				// 	return Content("Password is Not Valid!");
				// }

				using (TransactionScope trx = new TransactionScope())
				{
					TblMasterUser data = _context.TblMasterUsers.Where(m => m.Id == model.Id).FirstOrDefault(); // Ambil data sesuai dengan ID
					if (data.Username != model.Username)
					{
						TblMasterUser dataUserName = _context.TblMasterUsers.Where(m => m.Username == model.Username && m.IsActive == true && m.IsDeleted != true).FirstOrDefault();
						if (dataUserName != null)
						{
							return Content(dataUserName.Username + " Sudah terdaftar");
						}
					}
					if (data.Email != model.Email)
					{
						TblMasterUser dataEmail = _context.TblMasterUsers.Where(m => m.Email == model.Email && m.IsActive == true && m.IsDeleted != true).FirstOrDefault();
						if (dataEmail != null)
						{
							return Content(dataEmail.Email + " Sudah terdaftar");
						}
					}
					data.Username = model.Username;
					data.Fullname = model.Fullname;
					data.Email = model.Email;
					data.NoTelp = model.NoTelp;
					data.RoleId = model.RoleId;
					data.IsActive = model.IsActive;
					data.UpdatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
					data.UpdatedTime = DateTime.Now;

                    var UserId = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                    var RoleKode = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_RoleKode));

                    if (RoleKode == 1)
                    {
                        _context.Entry(data).State = EntityState.Modified;
                        _context.SaveChanges();
                    }
                    else
                    {
                        if (data.RoleId == 1)
                        {
                            return Content(GetConfig.AppSetting["SistemError"]);
                        }
                        else
                        {
                            _context.Entry(data).State = EntityState.Modified;
                            _context.SaveChanges();
                        }
                    }

					trx.Complete();

				}
				return Content("");

			}
			catch
			{
				return Content(GetConfig.AppSetting["SistemError"]);
			}
		}
		#endregion

		#region View
		public ActionResult View(int id)
		{
			if (!lastSession.Update())
			{
				return RedirectToAction("Login", GetConfig.AppSetting["Subdomain:DomainController"] + "Login", new { a = true });
			}
			var location = new Uri($"{Request.Scheme}://{Request.Host}{Request.Path}");
			string Path = location.AbsolutePath.Replace(this.ControllerContext.RouteData.Values["action"].ToString(), "Index");
			if (!accessSecurity.IsGetAccess(".." + Path))
			{
				return RedirectToAction("NotAccess", "Error");
			}

			TblMasterUser data = _context.TblMasterUsers.Where(m => m.Id == id).FirstOrDefault();
            if (data == null)
            {
                data = new TblMasterUser();
            }
            else
            {
                if (data.RoleId != null)
                {
                    ViewBag.RoleId = new SelectList(Utility.SelectDataRole(int.Parse(data.RoleId.ToString()), _context), "id", "text", data.RoleId);
                }
                else
                {
                    ViewBag.RoleId = new SelectList("", "");
                }
            }
            return PartialView("_View", data);
		}

		#endregion

		#region Delete
		public ActionResult Delete(string Ids)
		{
			if (!lastSession.Update())
			{
				return RedirectToAction("Login", GetConfig.AppSetting["Subdomain:DomainController"] + "Login", new { a = true });
			}
			var location = new Uri($"{Request.Scheme}://{Request.Host}{Request.Path}");
			string Path = location.AbsolutePath.Replace(this.ControllerContext.RouteData.Values["action"].ToString(), "Index");
			if (!accessSecurity.IsGetAccess(".." + Path))
			{
				return RedirectToAction("NotAccess", "Error");
			}

			try
			{
				long[] confirmedDeleteId = Ids.Split(',').Select(long.Parse).ToArray();

				List<TblMasterUser> Transaksis = _context.TblMasterUsers.Where(x => confirmedDeleteId.Contains(x.Id)).ToList(); //Ambil data sesuai dengan ID
				for (int i = 0; i < confirmedDeleteId.Length; i++)
				{
					var Username = _context.TblMasterUsers.Where(m => m.Id == Transaksis[i].Id).Select(m => m.Username).FirstOrDefault();

					TblMasterUser data = _context.TblMasterUsers.Find(Transaksis[i].Id);
					data.IsDeleted = true; //Jika true data tidak akan ditampilkan dan data masih tersimpan di dalam database
					data.DeletedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId)); ;
					data.DeletedTime = DateTime.Now;

                    var UserId = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                    var RoleKode = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_RoleKode));

                    if (RoleKode == 1)
                    {
                        _context.Entry(data).State = EntityState.Modified;
                        _context.SaveChanges();
                    }
                    else
                    {
                        if (Transaksis[i].RoleId == 1)
                        {
                            return Content("Gagal");
                        }
                        else
                        {
                            _context.Entry(data).State = EntityState.Modified;
                            _context.SaveChanges();
                        }
                    }
				}
				return Content("");
			}
			catch
			{
				return Content("Gagal");
			}
		}
		#endregion

	}
}
