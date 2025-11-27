using DashboardDevaBNI.Component;
using DashboardDevaBNI.Models;
using DashboardDevaBNI.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.Transactions;
using Trustee.Component;

namespace DashboardDevaBNI.Controllers
{
    public class DownloadListController : Controller
    {
        private readonly DbDashboardDevaBniContext _context;
        private readonly IConfiguration _configuration;
        private readonly LastSessionLog lastSession;
        private readonly AccessSecurity accessSecurity;
        public DownloadListController(IConfiguration config, DbDashboardDevaBniContext context, IHttpContextAccessor accessor)
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
            return View();
        }

        #region LoadData
        [HttpPost]
        public async Task<IActionResult> LoadData()
        {
            //if (!lastSession.Update())
            //{
            //    return RedirectToAction("Login", GetConfig.AppSetting["AppSettings:Subdomain:DomainController"] + "Login", new { a = true });
            //}
            //var location = new Uri($"{Request.Scheme}://{Request.Host}{Request.Path}");
            //string Path = location.AbsolutePath.Replace(this.ControllerContext.RouteData.Values["action"].ToString(), "Index");
            //if (!accessSecurity.IsGetAccess(".." + Path))
            //{
            //    return RedirectToAction("NotAccess", "Error");
            //}

            try
            {
                //DELETE FILE
                var hardDelete = await _context.TblDownloadBigFiles.Where(x => x.CreatedTime.Value.Date != DateTime.Now.Date).ToListAsync();
                if (hardDelete.Count > 0)
                {
                    foreach (var item in hardDelete)
                    {
                        if (System.IO.File.Exists(item.Path))
                        {
                            System.IO.File.Delete(item.Path);
                        }
                    }
                    _context.RemoveRange(hardDelete);
                    _context.SaveChanges();
                }

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


                //Untuk mengetahui info jumlah page dan total skip data
                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                int recordsTotal = 0;
                List<Downloadlist_LoadDataVM> list = new List<Downloadlist_LoadDataVM>();

                list = StoredProcedureExecutor.ExecuteSPList<Downloadlist_LoadDataVM>(_context, "sp_Load_DownloadList_View", new SqlParameter[]{
                        new SqlParameter("@Name", KeySearchParam),
                        new SqlParameter("@UserId", HttpContext.Session.GetString(SessionConstan.Session_UserId)),
                        new SqlParameter("@sortColumn", sortColumn),
                        new SqlParameter("@sortColumnDir", sortColumnDir),
                        new SqlParameter("@PageNumber", pageNumber),
                        new SqlParameter("@RowsPage", pageSize)});

                recordsTotal = StoredProcedureExecutor.ExecuteScalarInt(_context, "sp_Load_DownloadList_Count", new SqlParameter[]{
                        new SqlParameter("@Name", KeySearchParam),
                        new SqlParameter("@UserId", HttpContext.Session.GetString(SessionConstan.Session_UserId)),
                });

                if (list == null)
                {
                    list = new List<Downloadlist_LoadDataVM>();
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
        public async Task<object> DownloadFile(int id)
        {

            //if (!lastSession.Update())
            //{
            //    return RedirectToAction("Login", GetConfig.AppSetting["AppSettings:Subdomain:DomainController"] + "Login", new { a = true });
            //}
            //var location = new Uri($"{Request.Scheme}://{Request.Host}{Request.Path}");
            //string Patha = location.AbsolutePath.Replace(this.ControllerContext.RouteData.Values["action"].ToString(), "Index");
            //if (!accessSecurity.IsGetAccess(".." + Patha))
            //{
            //    return RedirectToAction("NotAccess", "Error");
            //}

            try
            {
                var UserId = HttpContext.Session.GetString(SessionConstan.Session_UserId);
                //Local
                var data = new object();
                var check = await _context.TblDownloadBigFiles.Where(x => x.Id == id && x.CreatedById == int.Parse(UserId)).FirstOrDefaultAsync();
                if (System.IO.File.Exists(check.Path))
                {
                    // Read the file into a byte array
                    byte[] fileBytes = System.IO.File.ReadAllBytes(check.Path);

                    // Set the content type and response headers
                    Response.Headers.Add("Content-Disposition", "attachment; filename=\"" + check.FileName + "." + check.FileExt + "\"");
                    Response.Headers.Add("Content-Type", "application/octet-stream");

                    // Return the file as a file stream
                    return File(fileBytes, "application/octet-stream");
                }
                else
                {
                    return NotFound("File not found");
                }

                //Minio
                //var data = new object();
                //var check = await _context.TblDownloadBigFiles.Where(x => x.Id == id).FirstOrDefaultAsync();
                //if (await ExternalAPI.CheckMinio(check.Path))
                //{
                //    // Read the file into a byte array

                //    byte[] fileBytes = await ExternalAPI.DownloadMinio(check.Path);

                //    // Set the content type and response headers
                //    Response.Headers.Add("Content-Disposition", "attachment; filename=\"" + Path.GetFileName(Regex.Replace(check.Path, @"\\", "/")) + "\"");
                //    Response.Headers.Add("Content-Type", "application/octet-stream");

                //    // Return the file as a file stream
                //    return File(fileBytes, "application/octet-stream");
                //}
                //else
                //{
                //    return NotFound("File not found");
                //}
            }
            catch (Exception Ex)
            {
                return Content(Ex.Message.ToString());
            }
        }

    }
}
