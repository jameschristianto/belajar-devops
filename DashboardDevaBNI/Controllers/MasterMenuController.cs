using DashboardDevaBNI.Component;
using DashboardDevaBNI.Models;
using DashboardDevaBNI.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Transactions;

namespace DashboardDevaBNI.Controllers
{
    public class MasterMenuController : Controller
    {
        private readonly DbDashboardDevaBniContext _context;
        private readonly IConfiguration _configuration;
        private readonly LastSessionLog lastSession;
        private readonly AccessSecurity accessSecurity;
        public MasterMenuController(IConfiguration config, DbDashboardDevaBniContext context, IHttpContextAccessor accessor)
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
                var NamaSearchParam = dict["columns[2][search][value]"];

                //Untuk mengetahui info jumlah page dan total skip data
                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                int recordsTotal = 0;
                List<MasterMenu_ViewModel> list = new List<MasterMenu_ViewModel>();

                list = StoredProcedureExecutor.ExecuteSPList<MasterMenu_ViewModel>(_context, "[sp_Load_PengaturanMenu_View]", new SqlParameter[]{
                        new SqlParameter("@Name", NamaSearchParam),
                        new SqlParameter("@Type", ""),
                        new SqlParameter("@sortColumn", sortColumn),
                        new SqlParameter("@sortColumnDir", sortColumnDir),
                        new SqlParameter("@PageNumber", pageNumber),
                    new SqlParameter("@RowsPage", pageSize)});

                recordsTotal = StoredProcedureExecutor.ExecuteScalarInt(_context, "[sp_Load_PengaturanMenu_Count]", new SqlParameter[]{
                        new SqlParameter("@Name", NamaSearchParam),
                        new SqlParameter("@Type", "")
                });

                if (list == null)
                {
                    list = new List<MasterMenu_ViewModel>();
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

            ViewBag.ParentId = new SelectList("", "");
            ViewBag.TipeId = new SelectList("", "");
            ViewBag.RolePegawai = new SelectList("", "");
            ViewBag.RolePegawai = new SelectList(Utility.SelectDataMasterRole(_context), "id", "text");


            return PartialView("_Create");
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<ActionResult> SubmitCreate(MasterMenu_ViewModel model, string Roles)
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
                if (Roles == null)
                {
                    return Content(GetConfig.AppSetting["AppSettings:PilihRolesNavigation:BelumPilihRoles"]);
                }

                var regex = await RegexRequest.RegexValidation(model);
                if (!regex)
                {
                    return Content("Bad Request!");
                }


                using (TransactionScope trx = new TransactionScope())
                {
                    TblNavigation data = new TblNavigation();
                    data.Name = model.Nama;
                    data.Type = model.TipeId;
                    data.ParentNavigationId = model.ParentId;
                    data.OrderBy = model.OrderBy;
                    data.Route = model.Route;
                    data.Icon = model.Icon;
                    data.IsActive = model.IsActive;
                    data.IsDeleted = false;
                    data.CreatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                    data.CreatedTime = DateTime.Now;
                    _context.TblNavigations.Add(data);
                    _context.SaveChanges();

                    string[] ArrayRoles = Roles.Split(',');
                    foreach (var item in ArrayRoles)
                    {
                        TblNavigationAssignment dataAssign = new TblNavigationAssignment();
                        dataAssign.NavigationId = data.Id;
                        dataAssign.RoleId = int.Parse(item);
                        dataAssign.CreatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                        dataAssign.CreatedTime = DateTime.Now;
                        dataAssign.IsActive = true;
                        dataAssign.IsDeleted = false;
                        _context.TblNavigationAssignments.Add(dataAssign);
                        _context.SaveChanges();
                    }

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

            MasterMenu_ViewModel data = new MasterMenu_ViewModel();
            data = StoredProcedureExecutor.ExecuteSPSingle<MasterMenu_ViewModel>(_context, "[sp_Load_PengaturanMenu_ById]", new SqlParameter[]{
                        new SqlParameter("@Id", id)
            });

            if (data != null)
            {
                if (data.ParentId != null)
                {
                    ViewBag.ParentId = new SelectList(Utility.SelectDataParentMenu(data.ParentId, _context), "id", "text", data.ParentId);
                }
                else
                {
                    ViewBag.ParentId = new SelectList("", "");
                }

                if (data.TipeId != null)
                {
                    ViewBag.TipeId = new SelectList(Utility.SelectDataLookupById(data.TipeId, "TypeMenu", _context), "id", "text", data.TipeId);
                }
                else
                {
                    ViewBag.TipeId = new SelectList("", "");
                }

                ViewBag.RolePegawai = new SelectList("", "");
                var dataAssignment = _context.TblNavigationAssignments.Where(m => m.NavigationId == data.Id).Select(m => m.RoleId).ToList();
                ViewBag.NavigationAssignment = String.Join(",", dataAssignment.ToArray());
            }

            return PartialView("_Edit", data);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<ActionResult> SubmitEdit(MasterMenu_ViewModel model, string Roles)
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
                    TblNavigation data = _context.TblNavigations.Where(m => m.Id == model.Id).FirstOrDefault(); // Ambil data sesuai dengan ID
                    data.Type = model.TipeId;
                    data.ParentNavigationId = model.ParentId;
                    data.Name = model.Nama;
                    data.Route = model.Route;
                    data.Icon = model.Icon;
                    data.OrderBy = model.OrderBy;
                    data.IsActive = model.IsActive;
                    data.UpdatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                    data.UpdatedTime = DateTime.Now;
                    _context.Entry(data).State = EntityState.Modified;
                    _context.SaveChanges();

                    //Ambil semua data assigment Menu
                    var AssignMenu = _context.TblNavigationAssignments.Where(m => m.NavigationId == data.Id).Select(m => m.RoleId.ToString()).ToList();
                    string[] ArrayAssignMenu = AssignMenu.ToArray();
                    string[] ArrayRoles = Roles.Split(',');

                    var TambahData = ArrayRoles.Except(ArrayAssignMenu);
                    var DeleteData = ArrayAssignMenu.Except(ArrayRoles);

                    if (DeleteData != null)
                    {
                        foreach (var item in DeleteData)
                        {
                            int IdRole = int.Parse(item);
                            TblNavigationAssignment dataNA = _context.TblNavigationAssignments.Where(m => m.NavigationId == data.Id && m.RoleId == IdRole).FirstOrDefault();
                            _context.TblNavigationAssignments.Remove(dataNA);
                        }
                    }

                    //Tambahkan Data Sasaran Unit
                    if (TambahData != null)
                    {
                        foreach (var item in TambahData)
                        {
                            TblNavigationAssignment dataAssigment = new TblNavigationAssignment();
                            dataAssigment.NavigationId = model.Id;
                            dataAssigment.RoleId = int.Parse(item);
                            dataAssigment.CreatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                            dataAssigment.CreatedTime = DateTime.Now;
                            dataAssigment.IsActive = true;
                            _context.TblNavigationAssignments.Add(dataAssigment);
                        }
                    }

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

            MasterMenu_ViewModel data = new MasterMenu_ViewModel();
            data = StoredProcedureExecutor.ExecuteSPSingle<MasterMenu_ViewModel>(_context, "[sp_Load_PengaturanMenu_ById]", new SqlParameter[]{
                        new SqlParameter("@Id", id)
            });

            if (data != null)
            {
                if (data.ParentId != null)
                {
                    ViewBag.ParentId = new SelectList(Utility.SelectDataParentMenu(data.ParentId, _context), "id", "text", data.ParentId);
                }
                else
                {
                    ViewBag.ParentId = new SelectList("", "");
                }

                if (data.TipeId != null)
                {
                    ViewBag.TipeId = new SelectList(Utility.SelectDataLookupById(data.TipeId, "TypeMenu", _context), "id", "text", data.TipeId);
                }
                else
                {
                    ViewBag.TipeId = new SelectList("", "");
                }

                ViewBag.RolePegawai = new SelectList("", "");
                var dataAssignment = _context.TblNavigationAssignments.Where(m => m.NavigationId == data.Id).Select(m => m.RoleId).ToList();
                ViewBag.NavigationAssignment = String.Join(",", dataAssignment.ToArray());
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

                List<TblNavigation> Transaksis = _context.TblNavigations.Where(x => confirmedDeleteId.Contains(x.Id)).ToList(); //Ambil data sesuai dengan ID
                for (int i = 0; i < confirmedDeleteId.Length; i++)
                {
                    TblNavigation data = _context.TblNavigations.Find(Transaksis[i].Id);
                    data.IsDeleted = true; //Jika true data tidak akan ditampilkan dan data masih tersimpan di dalam database
                    data.DeletedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                    data.DeletedTime = System.DateTime.Now;
                    _context.Entry(data).State = EntityState.Modified;
                    _context.SaveChanges();
                }
                return Content("");
            }
            catch
            {
                return Content("gagal");
            }
        }
        #endregion
    }
}
