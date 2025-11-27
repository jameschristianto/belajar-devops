using DashboardDevaBNI.Component;
using DashboardDevaBNI.Models;
using DashboardDevaBNI.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Transactions;

namespace DashboardDevaBNI.Controllers
{
    public class MasterSystemParameterController : Controller
    {
        private readonly DbDashboardDevaBniContext _context;
        private readonly IConfiguration _configuration;
        private readonly LastSessionLog lastSession;
        private readonly AccessSecurity accessSecurity;
        public MasterSystemParameterController(IConfiguration config, DbDashboardDevaBniContext context, IHttpContextAccessor accessor)
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
                return RedirectToAction("Login", GetConfig.AppSetting["AppSettings:Subdomain:DomainController"] + "Login", new { a = true });
            }
            var location = new Uri($"{Request.Scheme}://{Request.Host}{Request.Path}");
            string Path = location.AbsolutePath;
            if (!accessSecurity.IsGetAccess(".." + Path))
            {
                return RedirectToAction("NotAccess", GetConfig.AppSetting["AppSettings:Subdomain:DomainController"] + "Error");
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
                return RedirectToAction("Login", GetConfig.AppSetting["AppSettings:Subdomain:DomainController"] + "Login", new { a = true });
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
                var KeySearchParam = dict["columns[2][search][value]"];
                var ValueSearchParam = dict["columns[3][search][value]"];


                //Untuk mengetahui info jumlah page dan total skip data
                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                int recordsTotal = 0;
                List<MasterSystemParameter_ViewModel> list = new List<MasterSystemParameter_ViewModel>();

                list = StoredProcedureExecutor.ExecuteSPList<MasterSystemParameter_ViewModel>(_context, "sp_Load_MasterSystemParameter_View", new SqlParameter[]{
                        new SqlParameter("@Key", KeySearchParam),
                        new SqlParameter("@Value", ValueSearchParam),
                        new SqlParameter("@sortColumn", sortColumn),
                        new SqlParameter("@sortColumnDir", sortColumnDir),
                        new SqlParameter("@PageNumber", pageNumber),
                        new SqlParameter("@RowsPage", pageSize)});

                recordsTotal = StoredProcedureExecutor.ExecuteScalarInt(_context, "sp_Load_MasterSystemParameter_Count", new SqlParameter[]{
                        new SqlParameter("@Key", KeySearchParam),
                        new SqlParameter("@Value", ValueSearchParam),
                });

                if (list == null)
                {
                    list = new List<MasterSystemParameter_ViewModel>();
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
                return RedirectToAction("Login", GetConfig.AppSetting["AppSettings:Subdomain:DomainController"] + "Login", new { a = true });
            }
            var location = new Uri($"{Request.Scheme}://{Request.Host}{Request.Path}");
            string Path = location.AbsolutePath.Replace(this.ControllerContext.RouteData.Values["action"].ToString(), "Index");
            if (!accessSecurity.IsGetAccess(".." + Path))
            {
                return RedirectToAction("NotAccess", "Error");
            }
            return PartialView("_Create");
        }


        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<ActionResult> SubmitCreate(TblMasterSystemParameter model)
        {
            if (!lastSession.Update())
            {
                return RedirectToAction("Login", GetConfig.AppSetting["AppSettings:Subdomain:DomainController"] + "Login", new { a = true });
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

                using (TransactionScope trx = new TransactionScope())
                {
                    model.IsDeleted = false;
                    model.CreatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                    model.CreatedTime = DateTime.Now;
                    _context.TblMasterSystemParameters.Add(model);
                    _context.SaveChanges();

                    trx.Complete();
                }

                return Content("");

            }
            catch (Exception Ex)
            {
                return Content(GetConfig.AppSetting["AppSettings:SistemError"]);
            }
        }
        #endregion

        #region Edit
        public ActionResult Edit(int id)
        {
            if (!lastSession.Update())
            {
                return RedirectToAction("Login", GetConfig.AppSetting["AppSettings:Subdomain:DomainController"] + "Login", new { a = true });
            }
            var location = new Uri($"{Request.Scheme}://{Request.Host}{Request.Path}");
            string Path = location.AbsolutePath.Replace(this.ControllerContext.RouteData.Values["action"].ToString(), "Index");
            if (!accessSecurity.IsGetAccess(".." + Path))
            {
                return RedirectToAction("NotAccess", "Error");
            }

            TblMasterSystemParameter data = _context.TblMasterSystemParameters.Where(m => m.Id == id).FirstOrDefault();
            if (data == null)
            {
                data = new TblMasterSystemParameter();
            }

            return PartialView("_Edit", data);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<ActionResult> SubmitEdit(TblMasterSystemParameter model)
        {
            if (!lastSession.Update())
            {
                return RedirectToAction("Login", GetConfig.AppSetting["AppSettings:Subdomain:DomainController"] + "Login", new { a = true });
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

                using (TransactionScope trx = new TransactionScope())
                {
                    TblMasterSystemParameter data = _context.TblMasterSystemParameters.Where(m => m.Id == model.Id).FirstOrDefault(); // Ambil data sesuai dengan ID
                    data.Value = model.Value;
                    data.Key = model.Key;
                    data.Description = model.Description;
                    data.IsActive = model.IsActive;
                    data.UpdatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                    data.UpdatedTime = DateTime.Now;
                    _context.Entry(data).State = EntityState.Modified;
                    _context.SaveChanges();

                    trx.Complete();

                }
                return Content("");

            }
            catch
            {
                return Content(GetConfig.AppSetting["AppSettings:SistemError"]);
            }
        }
        #endregion

        #region View
        public ActionResult View(int id)
        {
            if (!lastSession.Update())
            {
                return RedirectToAction("Login", GetConfig.AppSetting["AppSettings:Subdomain:DomainController"] + "Login", new { a = true });
            }
            var location = new Uri($"{Request.Scheme}://{Request.Host}{Request.Path}");
            string Path = location.AbsolutePath.Replace(this.ControllerContext.RouteData.Values["action"].ToString(), "Index");
            if (!accessSecurity.IsGetAccess(".." + Path))
            {
                return RedirectToAction("NotAccess", "Error");
            }

            TblMasterSystemParameter data = _context.TblMasterSystemParameters.Where(m => m.Id == id).FirstOrDefault();
            if (data == null)
            {
                data = new TblMasterSystemParameter();
            }

            return PartialView("_View", data);
        }

        #endregion

        #region Delete
        public ActionResult Delete(string Ids)
        {
            if (!lastSession.Update())
            {
                return RedirectToAction("Login", GetConfig.AppSetting["AppSettings:Subdomain:DomainController"] + "Login", new { a = true });
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

                List<TblMasterSystemParameter> Transaksis = _context.TblMasterSystemParameters.Where(x => confirmedDeleteId.Contains(x.Id)).ToList(); //Ambil data sesuai dengan ID
                for (int i = 0; i < confirmedDeleteId.Length; i++)
                {
                    TblMasterSystemParameter data = _context.TblMasterSystemParameters.Find(Transaksis[i].Id);
                    data.IsDeleted = true; //Jika true data tidak akan ditampilkan dan data masih tersimpan di dalam database
                    data.DeletedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId)); ;
                    data.DeletedTime = DateTime.Now;
                    _context.Entry(data).State = EntityState.Modified;
                    _context.SaveChanges();
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
