using DashboardDevaBNI.Component;
using DashboardDevaBNI.Models;
using DashboardDevaBNI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NPOI.HSSF.UserModel;
using NPOI.POIFS.Crypt.Dsig;
using NPOI.SS.UserModel;
using NPOI.Util;
using NPOI.XSSF.UserModel;
using DinkToPdf.Contracts;
using System;
using System.Globalization;
using System.Runtime.ConstrainedExecution;
using System.Security.Policy;
using System.Transactions;
using DinkToPdf;
using iText.Kernel.Pdf;
using iText.Layout.Element;
using RazorLight;
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;
using Minio.DataModel;
using iText.StyledXmlParser.Jsoup.Nodes;
using System.Reactive;
using NPOI.SS.Formula.Functions;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.Net;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Utilities;

namespace DashboardDevaBNI.Controllers
{
    public class NoticeOfDisbursementController : Controller
    {
        private readonly DbDashboardDevaBniContext _context;
        private readonly IConfiguration _configuration;
        private readonly IConverter _converter;
        private readonly LastSessionLog lastSession;
        private readonly AccessSecurity accessSecurity;
        public NoticeOfDisbursementController(IConfiguration config, DbDashboardDevaBniContext context, IHttpContextAccessor accessor)
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
                var NodNo = dict["columns[2][search][value]"];
                var NodDate = dict["columns[3][search][value]"];

                //Untuk mengetahui info jumlah page dan total skip data
                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                int recordsTotal = 0;

                CultureInfo culture = new CultureInfo("id-ID");
                var dateArray = new string[] { };

                if (!string.IsNullOrEmpty(NodDate))
                {
                    dateArray = NodDate.Split(" to ");
                }

                List<NoticeOfDisbursement_ViewModel> list = new List<NoticeOfDisbursement_ViewModel>();
                list = StoredProcedureExecutor.ExecuteSPList<NoticeOfDisbursement_ViewModel>(_context, "sp_Load_NoticeOfDisburesement_View", new SqlParameter[]{
                        new SqlParameter("@NodNo", NodNo),
                        new SqlParameter("@NodDateFrom", dateArray.Count() == 0 ? "" : DateTime.Parse(dateArray[0], culture)),
                        new SqlParameter("@NodDateTo", dateArray.Count() == 0 ? "" : DateTime.Parse(dateArray[1], culture)),
                        new SqlParameter("@sortColumn", sortColumn),
                        new SqlParameter("@sortColumnDir", sortColumnDir),
                        new SqlParameter("@PageNumber", pageNumber),
                        new SqlParameter("@RowsPage", pageSize)});

                recordsTotal = StoredProcedureExecutor.ExecuteScalarInt(_context, "sp_Load_NoticeOfDisburesement_Count", new SqlParameter[]{
                        new SqlParameter("@NodNo", NodNo),
                        new SqlParameter("@NodDateFrom", dateArray.Count() == 0 ? "" : DateTime.Parse(dateArray[0], culture)),
                        new SqlParameter("@NodDateTo", dateArray.Count() == 0 ? "" : DateTime.Parse(dateArray[1], culture)),
                });

                if (list == null)
                {
                    list = new List<NoticeOfDisbursement_ViewModel>();
                    recordsTotal = 0;
                }

                return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = list });
            }
            catch (Exception Ex)
            {
                throw;
            }
        }
        [HttpPost]
        public IActionResult LoadDataDetailNodTemp(string randomstring)
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

                //Untuk mengetahui info jumlah page dan total skip data
                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                int recordsTotal = 0;

                List<NoticeOfDisbursement_ViewModel> list = new List<NoticeOfDisbursement_ViewModel>();

                list = StoredProcedureExecutor.ExecuteSPList<NoticeOfDisbursement_ViewModel>(_context, "sp_Load_NoticeOfDisburesementDetailTemp_View", new SqlParameter[]{
                        new SqlParameter("@RandomString", randomstring),
                        new SqlParameter("@sortColumn", sortColumn),
                        new SqlParameter("@sortColumnDir", sortColumnDir),
                        new SqlParameter("@PageNumber", pageNumber),
                        new SqlParameter("@RowsPage", pageSize)});

                recordsTotal = StoredProcedureExecutor.ExecuteScalarInt(_context, "sp_Load_NoticeOfDisburesementDetailTemp_Count", new SqlParameter[]{
                        new SqlParameter("@RandomString", randomstring),
                });

                if (list == null)
                {
                    list = new List<NoticeOfDisbursement_ViewModel>();
                    recordsTotal = 0;
                }

                return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = list });
            }
            catch (Exception Ex)
            {
                throw;
            }
        }
        [HttpPost]
        public IActionResult LoadDataDetailNod(string NodId)
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

                //Untuk mengetahui info jumlah page dan total skip data
                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                int recordsTotal = 0;

                List<NoticeOfDisbursement_ViewModel> list = new List<NoticeOfDisbursement_ViewModel>();

                list = StoredProcedureExecutor.ExecuteSPList<NoticeOfDisbursement_ViewModel>(_context, "sp_Load_NoticeOfDisburesementDetail_View", new SqlParameter[]{
                        new SqlParameter("@NodId", NodId),
                        new SqlParameter("@sortColumn", sortColumn),
                        new SqlParameter("@sortColumnDir", sortColumnDir),
                        new SqlParameter("@PageNumber", pageNumber),
                        new SqlParameter("@RowsPage", pageSize)});

                recordsTotal = StoredProcedureExecutor.ExecuteScalarInt(_context, "sp_Load_NoticeOfDisburesementDetail_Count", new SqlParameter[]{
                        new SqlParameter("@NodId", NodId),
                });

                if (list == null)
                {
                    list = new List<NoticeOfDisbursement_ViewModel>();
                    recordsTotal = 0;
                }

                return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = list });
            }
            catch (Exception Ex)
            {
                throw;
            }
        }

        [HttpPost]
        public IActionResult LoadDataFileNodTemp(string randomstring)
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

                //Untuk mengetahui info jumlah page dan total skip data
                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                int recordsTotal = 0;

                List<NoticeOfDisbursementFile_ViewModel> list = new List<NoticeOfDisbursementFile_ViewModel>();

                list = StoredProcedureExecutor.ExecuteSPList<NoticeOfDisbursementFile_ViewModel>(_context, "sp_Load_NoticeOfDisburesementFileTemp_View", new SqlParameter[]{
                        new SqlParameter("@RandomString", randomstring),
                        new SqlParameter("@sortColumn", sortColumn),
                        new SqlParameter("@sortColumnDir", sortColumnDir),
                        new SqlParameter("@PageNumber", pageNumber),
                        new SqlParameter("@RowsPage", pageSize)});

                recordsTotal = StoredProcedureExecutor.ExecuteScalarInt(_context, "sp_Load_NoticeOfDisburesementFileTemp_Count", new SqlParameter[]{
                        new SqlParameter("@RandomString", randomstring),
                });

                if (list == null)
                {
                    list = new List<NoticeOfDisbursementFile_ViewModel>();
                    recordsTotal = 0;
                }

                return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = list });
            }
            catch (Exception Ex)
            {
                throw;
            }
        }
        [HttpPost]
        public IActionResult LoadDataFileNod(string NodId)
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

                //Untuk mengetahui info jumlah page dan total skip data
                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                int recordsTotal = 0;

                List<NoticeOfDisbursementFile_ViewModel> list = new List<NoticeOfDisbursementFile_ViewModel>();

                list = StoredProcedureExecutor.ExecuteSPList<NoticeOfDisbursementFile_ViewModel>(_context, "sp_Load_NoticeOfDisburesementFile_View", new SqlParameter[]{
                        new SqlParameter("@NodId", NodId),
                        new SqlParameter("@sortColumn", sortColumn),
                        new SqlParameter("@sortColumnDir", sortColumnDir),
                        new SqlParameter("@PageNumber", pageNumber),
                        new SqlParameter("@RowsPage", pageSize)});

                recordsTotal = StoredProcedureExecutor.ExecuteScalarInt(_context, "sp_Load_NoticeOfDisburesementFile_Count", new SqlParameter[]{
                        new SqlParameter("@NodId", NodId),
                });

                if (list == null)
                {
                    list = new List<NoticeOfDisbursementFile_ViewModel>();
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
            ViewBag.Cur = new SelectList("", "");

            return PartialView("_Create");
        }
        public ActionResult CreateBulk()
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
            return PartialView("_CreateBulk");
        }
        public ActionResult CreateDetailNod()
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

            ViewBag.CreditorRef = new SelectList("", "");
            return PartialView("_CreateDetailNod");
        }
        public ActionResult CreateFileNod()
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

            return PartialView("_CreateFileNod");
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<ActionResult> SubmitCreate(NoticeOfDisbursement_ViewModel data, string randomString)
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
                var regex = await RegexRequest.RegexValidation(data);
                if (!regex)
                {
                    return Content("Bad Request!");
                }
                TblNoticeOfDisbursement dataNodNo = _context.TblNoticeOfDisbursements.Where(m => m.NodNo == data.NodNo && m.IsDeleted != 1).FirstOrDefault();
                if (dataNodNo != null)
                {
                    return Content("NodNo sudah terdaftar atas " + dataNodNo.NodNo);
                }

                using (TransactionScope trx = new TransactionScope())
                {
                    TblNoticeOfDisbursement model = new TblNoticeOfDisbursement();
                    model.NodNo = data.NodNo;
                    model.NodDate = data.NodDate;
                    model.Cur = _context.TblMasterLookups.Where(m => m.Value == int.Parse(data.Cur) && m.Type == "Currency").Select(m=>m.Name).FirstOrDefault();
                    model.ValueDate = data.ValueDate;
                    model.Status = "Created";
                    model.CreatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                    model.CreatedTime = DateTime.Now;
                    model.IsDeleted = 0;
                    model.IsActive = 1;
                    _context.TblNoticeOfDisbursements.Add(model);
                    _context.SaveChanges();

                    List<TblNoticeOfDisbursementDetailTemp> nodDetailList = _context.TblNoticeOfDisbursementDetailTemps.Where(m => m.RandomString == randomString && m.IsActive == 1 && m.IsDeleted == 0).ToList();
                    if (nodDetailList != null)
                    {
                        foreach (var item in nodDetailList)
                        {
                            TblNoticeOfDisbursementDetail nodDetail = new TblNoticeOfDisbursementDetail();
                            nodDetail.NodId = model.Id;
                            nodDetail.CreditorRef = item.CreditorRef;
                            nodDetail.Amount = item.Amount;
                            nodDetail.AmountIdr = item.AmountIdr;
                            nodDetail.CreatedTime = DateTime.Now;
                            nodDetail.CreatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                            nodDetail.IsActive = 1;
                            nodDetail.IsDeleted = 0;
                            _context.TblNoticeOfDisbursementDetails.Add(nodDetail);
                        }
                        _context.SaveChanges();
                    }

                    List<TblFileUploadNodTemp> nodFileList = _context.TblFileUploadNodTemps.Where(m => m.RandomString == randomString && m.IsActive == 1 && m.IsDeleted == 0).ToList();
                    if (nodFileList != null)
                    {
                        foreach (var item in nodFileList)
                        {
                            TblFileUploadNod nodFile = new TblFileUploadNod();
                            nodFile.IdNod = model.Id;
                            nodFile.FileName = item.FileName;
                            nodFile.FileSize = item.FileSize;
                            nodFile.FilePath = item.FilePath;
                            nodFile.FileExt = item.FileExt;
                            nodFile.UploadTime = DateTime.Now;
                            nodFile.UploadById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                            nodFile.IsActive = 1;
                            nodFile.IsDeleted = 0;
                            _context.TblFileUploadNods.Add(nodFile);
                        }
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

        [HttpPost]
        public async Task<ActionResult> SubmitExcelCreate(NoticeOfDisbursement_ViewModel model)
        {
            if (!lastSession.Update())
            {
                return RedirectToAction("Login", GetConfig.AppSetting["AppSettings:Subdomain:DomainController"] + "Login", new { a = true });
            }
            var location = new Uri($"{Request.Scheme}://{Request.Host}{Request.Path}");
            string Patha = location.AbsolutePath.Replace(this.ControllerContext.RouteData.Values["action"].ToString(), "Index");
            if (!accessSecurity.IsGetAccess(".." + Patha))
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

                var increment = 0;
                using (var trx = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions
                {
                    Timeout = new TimeSpan(1, 5, 0)
                }, TransactionScopeAsyncFlowOption.Enabled))
                {
                    #region File Standing Instruction
                    if (model.File == null)
                    {
                        return Content("Masukkan file terlebih dahulu");
                    }
                    else {
                        TblMasterSystemParameter ConfigAppsLocalPath = _context.TblMasterSystemParameters.Where(m => m.Key == "PathFileExcel").FirstOrDefault();

                        List<TblNoticeOfDisbursementDetailTemp> list = _context.TblNoticeOfDisbursementDetailTemps.Where(m => m.RandomString == model.RandomString).ToList();
                        if (list.Count() != 0)
                        {
                            return Content("File sudah ter-upload");
                        }

                        var RowErrorKe = 0;

                        //Ambil ext file yang diijinkan
                        string AllowedFileUploadType = ".xls,.xlxs,.xlsx";
                        decimal SizeFileNon = model.File.Length / 1000000;
                        string ExtNon = Path.GetExtension(model.File.FileName);

                        //Validate Upload
                        if (!AllowedFileUploadType.Contains(ExtNon))
                        {
                            return Content("Tipe File tidak sesuai");
                        }

                        /* if (!Directory.Exists(PathFolder))
                         {
                             Directory.CreateDirectory(PathFolder);
                         }*/

                        if (SizeFileNon <= 10)
                        {
                            string sFileExtensionNon = Path.GetExtension(model.File.FileName).ToLower();
                            ISheet sheet;
                            string fullPathNon = Path.Combine(model.File.FileName);
                            var fileNameReplaceSpaceNon = model.File.FileName.Replace(" ", "_");
                            var path = Path.Combine(ConfigAppsLocalPath.Value, fileNameReplaceSpaceNon);
                            using (var stream = new MemoryStream())
                            {
                                model.File.CopyTo(stream);
                                stream.Position = 0;
                                if (sFileExtensionNon == ".xls")
                                {
                                    HSSFWorkbook hssfwb = new HSSFWorkbook(stream); //This will read the Excel 97-2000 formats  
                                    sheet = hssfwb.GetSheetAt(0); //get first sheet from workbook  
                                }
                                else
                                {
                                    XSSFWorkbook hssfwb = new XSSFWorkbook(stream); //This will read 2007 Excel format  
                                    sheet = hssfwb.GetSheetAt(0); //get first sheet from workbook   
                                }
                            }


                            using (TransactionScope trx2 = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions
                            {
                                Timeout = new TimeSpan(1, 5, 0)
                            }, TransactionScopeAsyncFlowOption.Enabled))
                            {

                                for (int i = (sheet.FirstRowNum + 1); i <= sheet.LastRowNum; i++) //Read Excel File
                                {
                                    RowErrorKe = i;
                                    IRow row = sheet.GetRow(i);
                                    if (row == null) continue;
                                    if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;

                                    var a = row.GetCell(0).ToString();
                                    if (row.GetCell(0).ToString() != "")
                                    {
                                        CultureInfo culture = new CultureInfo("id-ID");

                                        var NodNotempNod = row.GetCell(0).ToString() == "" || row.GetCell(0).ToString() == null ? "" : row.GetCell(0).ToString();
                                        var NodNotempNodTemp = row.GetCell(4).ToString() == "" || row.GetCell(4).ToString() == null ? "" : row.GetCell(4).ToString();

                                        TblNoticeOfDisbursement ListTempNod = _context.TblNoticeOfDisbursements.Where(m => m.NodNo == NodNotempNod && m.IsDeleted == 0).FirstOrDefault();
                                        TblNoticeOfDisbursementDetail ListTempNodDetail = _context.TblNoticeOfDisbursementDetails.Where(m => m.CreditorRef == NodNotempNodTemp && m.IsDeleted == 0).FirstOrDefault();

                                        if (ListTempNod == null)
                                        {
                                            TblNoticeOfDisbursement ListTempNew = new TblNoticeOfDisbursement();
                                            ListTempNew.NodNo = row.GetCell(0).ToString() == "" || row.GetCell(0).ToString() == null ? "" : row.GetCell(0).ToString();
                                            ListTempNew.NodDate = DateTime.Parse(row.GetCell(1).ToString() == "" || row.GetCell(1).ToString() == null ? "" : row.GetCell(1).ToString(), culture);
                                            ListTempNew.ValueDate = DateTime.Parse(row.GetCell(2).ToString() == "" || row.GetCell(2).ToString() == null ? "" : row.GetCell(2).ToString(), culture);
                                            var Currency = row.GetCell(3).ToString() == "" || row.GetCell(3).ToString() == null ? "" : row.GetCell(3).ToString();
                                            var CheckCurrency = _context.TblMasterLookups.Where(m => m.Name == Currency && m.Type == "Currency").Select(m => m.Name).FirstOrDefault();
                                            ListTempNew.Cur = CheckCurrency;
                                            ListTempNew.Status = "Created";
                                            ListTempNew.IsActive = 1;
                                            ListTempNew.IsDeleted = 0;
                                            ListTempNew.CreatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                                            ListTempNew.CreatedTime = DateTime.Now;
                                            _context.TblNoticeOfDisbursements.Add(ListTempNew);
                                            _context.SaveChanges();

                                            if (ListTempNodDetail == null)
                                            {
                                                TblNoticeOfDisbursementDetail ListTempNewDetail = new TblNoticeOfDisbursementDetail();
                                                ListTempNewDetail.NodId = ListTempNew.Id;
                                                ListTempNewDetail.CreditorRef = row.GetCell(4).ToString() == "" || row.GetCell(4).ToString() == null ? "" : row.GetCell(4).ToString();
                                                ListTempNewDetail.Amount = decimal.Parse(row.GetCell(5).ToString() == "" || row.GetCell(5).ToString() == "0" ? "" : row.GetCell(5).ToString());
                                                ListTempNewDetail.AmountIdr = decimal.Parse(row.GetCell(6).ToString() == "" || row.GetCell(6).ToString() == "0" ? "" : row.GetCell(6).ToString());
                                                ListTempNewDetail.IsActive = 1;
                                                ListTempNewDetail.IsDeleted = 0;
                                                ListTempNewDetail.CreatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                                                ListTempNewDetail.CreatedTime = DateTime.Now;
                                                bool CheckCreRef = CheckCreditorRef(row.GetCell(4).ToString() == "" || row.GetCell(4).ToString() == null ? "" : row.GetCell(4).ToString());
                                                if(CheckCreRef)
                                                {
                                                    _context.TblNoticeOfDisbursementDetails.Add(ListTempNewDetail);
                                                }
                                            }
                                            else
                                            {
                                                ListTempNodDetail.NodId = ListTempNew.Id;
                                                ListTempNodDetail.CreditorRef = row.GetCell(4).ToString() == "" || row.GetCell(4).ToString() == null ? "" : row.GetCell(4).ToString();
                                                ListTempNodDetail.Amount = decimal.Parse(row.GetCell(5).ToString() == "" || row.GetCell(5).ToString() == "0" ? "" : row.GetCell(5).ToString());
                                                ListTempNodDetail.AmountIdr = decimal.Parse(row.GetCell(6).ToString() == "" || row.GetCell(6).ToString() == "0" ? "" : row.GetCell(6).ToString());
                                                ListTempNodDetail.IsActive = 1;
                                                ListTempNodDetail.IsDeleted = 0;
                                                ListTempNodDetail.CreatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                                                ListTempNodDetail.CreatedTime = DateTime.Now;
                                                bool CheckCreRef = CheckCreditorRef(row.GetCell(4).ToString() == "" || row.GetCell(4).ToString() == null ? "" : row.GetCell(4).ToString());
                                                if (CheckCreRef)
                                                {
                                                    _context.TblNoticeOfDisbursementDetails.Add(ListTempNodDetail);
                                                }
                                            }
                                            _context.SaveChanges();

                                        }
                                        else if(ListTempNod != null && ListTempNod.Status != "Verified")
                                        {
                                            ListTempNod.NodNo = row.GetCell(0).ToString() == "" || row.GetCell(0).ToString() == null ? "" : row.GetCell(0).ToString();
                                            ListTempNod.NodDate = DateTime.Parse(row.GetCell(1).ToString() == "" || row.GetCell(1).ToString() == null ? "" : row.GetCell(1).ToString(), culture);
                                            ListTempNod.ValueDate = DateTime.Parse(row.GetCell(2).ToString() == "" || row.GetCell(2).ToString() == null ? "" : row.GetCell(2).ToString(), culture);
                                            var Currency = row.GetCell(3).ToString() == "" || row.GetCell(3).ToString() == null ? "" : row.GetCell(3).ToString();
                                            var CheckCurrency = _context.TblMasterLookups.Where(m => m.Name == Currency && m.Type == "Currency").Select(m => m.Name).FirstOrDefault();
                                            ListTempNod.Cur = CheckCurrency;
                                            ListTempNod.IsActive = 1;
                                            ListTempNod.IsDeleted = 0;
                                            ListTempNod.CreatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                                            ListTempNod.CreatedTime = DateTime.Now;
                                            _context.Entry(ListTempNod).State = EntityState.Modified;

                                            if (ListTempNodDetail == null)
                                            {
                                                TblNoticeOfDisbursementDetail ListTempNewDetail = new TblNoticeOfDisbursementDetail();
                                                ListTempNewDetail.NodId = ListTempNod.Id;
                                                ListTempNewDetail.CreditorRef = row.GetCell(4).ToString() == "" || row.GetCell(4).ToString() == null ? "" : row.GetCell(4).ToString();
                                                ListTempNewDetail.Amount = decimal.Parse(row.GetCell(5).ToString() == "" || row.GetCell(5).ToString() == "0" ? "" : row.GetCell(5).ToString());
                                                ListTempNewDetail.AmountIdr = decimal.Parse(row.GetCell(6).ToString() == "" || row.GetCell(6).ToString() == "0" ? "" : row.GetCell(6).ToString());
                                                ListTempNewDetail.IsActive = 1;
                                                ListTempNewDetail.IsDeleted = 0;
                                                ListTempNewDetail.CreatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                                                ListTempNewDetail.CreatedTime = DateTime.Now;
                                                bool CheckCreRef = CheckCreditorRef(row.GetCell(4).ToString() == "" || row.GetCell(4).ToString() == null ? "" : row.GetCell(4).ToString());
                                                if (CheckCreRef)
                                                {
                                                    _context.TblNoticeOfDisbursementDetails.Add(ListTempNewDetail);
                                                }
                                            }
                                            else
                                            {
                                                ListTempNodDetail.NodId = ListTempNod.Id;
                                                ListTempNodDetail.CreditorRef = row.GetCell(4).ToString() == "" || row.GetCell(4).ToString() == null ? "" : row.GetCell(4).ToString();
                                                ListTempNodDetail.Amount = decimal.Parse(row.GetCell(5).ToString() == "" || row.GetCell(5).ToString() == null ? "" : row.GetCell(5).ToString());
                                                ListTempNodDetail.AmountIdr = decimal.Parse(row.GetCell(6).ToString() == "" || row.GetCell(6).ToString() == null ? "" : row.GetCell(6).ToString());
                                                ListTempNodDetail.IsActive = 1;
                                                ListTempNodDetail.IsDeleted = 0;
                                                ListTempNodDetail.CreatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                                                ListTempNodDetail.CreatedTime = DateTime.Now;
                                                bool CheckCreRef = CheckCreditorRef(row.GetCell(4).ToString() == "" || row.GetCell(4).ToString() == null ? "" : row.GetCell(4).ToString());
                                                if (CheckCreRef)
                                                {
                                                    _context.Entry(ListTempNodDetail).State = EntityState.Modified;
                                                }
                                            }

                                            _context.SaveChanges();
                                        }
                                    }
                                }
                                trx2.Complete();
                            }
                            trx.Complete();
                            return Content("");
                        }
                        else
                        {
                            return Content("File Max 10MB");
                        }
                    }
                    #endregion
                }
            }
            catch (Exception Ex)
            {
                return Content(GetConfig.AppSetting["AppSettings:SistemError"]);
            }
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult SubmitCreateDetailNodTemp(NoticeOfDisbursement_ViewModel data, string uniq)
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
                //TblNoticeOfDisbursementDetail dataNodNo = _context.TblNoticeOfDisbursementDetails.Where(m => m.CreditorRef == data.CreditorRef && m.IsDeleted != 1).FirstOrDefault();
                //if (dataNodNo != null)
                //{
                //    return Content("CreditorRefId sudah terdaftar");
                //}

                using (TransactionScope trx = new TransactionScope())
                {
                    TblNoticeOfDisbursementDetailTemp model = new TblNoticeOfDisbursementDetailTemp();
                    model.CreditorRef = data.CreditorRef;
                    model.Amount = decimal.Parse(data.Amount.Replace(".", ""));
                    model.AmountIdr = decimal.Parse(data.AmountIDR.Replace(".", ""));
                    model.IsActive = 1;
                    model.IsDeleted = 0;
                    model.RandomString = uniq;

                    _context.TblNoticeOfDisbursementDetailTemps.Add(model);
                    _context.SaveChanges();


                    trx.Complete();
                }

                return Content("");


            }
            catch (Exception ex)
            {
                return Content(GetConfig.AppSetting["GlobalMessage:SistemError"]);
            }
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult SubmitCreateDetailNod(NoticeOfDisbursement_ViewModel data, string uniq)
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
                //TblNoticeOfDisbursementDetail dataNodNo = _context.TblNoticeOfDisbursementDetails.Where(m => m.CreditorRef == data.CreditorRef && m.IsDeleted != 1).FirstOrDefault();
                //if (dataNodNo != null)
                //{
                //    return Content("CreditorRefId sudah terdaftar");
                //}

                TblNoticeOfDisbursement dataAssign = _context.TblNoticeOfDisbursements.Where(m => m.Id == int.Parse(uniq) && m.IsDeleted == 0).FirstOrDefault();

                if (dataAssign.IdNodFromApi != null)
                {
                    var jsonParseReturnCheck = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>("");
                    var urlCheck = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:Nod:GetDataById"] + dataAssign.IdNodFromApi;
                    (bool resultApiCheck, string resultCheck) = RequestToAPI.GetJsonStringWebApi(urlCheck, null);
                    if (resultApiCheck && !string.IsNullOrEmpty(resultCheck))
                    {
                        jsonParseReturnCheck = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>(resultCheck);
                        if (jsonParseReturnCheck.StatusCode == 200 || jsonParseReturnCheck.StatusCode == 201)
                        {
                            if (jsonParseReturnCheck.Data.Status == "Unverified")
                            {
                                using (TransactionScope trx = new TransactionScope())
                                {
                                    TblNoticeOfDisbursementDetail model = new TblNoticeOfDisbursementDetail();
                                    model.NodId = int.Parse(uniq);
                                    model.CreditorRef = data.CreditorRef;
                                    model.Amount = decimal.Parse(data.Amount.Replace(".", ""));
                                    model.AmountIdr = decimal.Parse(data.AmountIDR.Replace(".", ""));
                                    model.CreatedTime = DateTime.Now;
                                    model.CreatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                                    model.IsActive = 1;
                                    model.IsDeleted = 0;
                                    _context.TblNoticeOfDisbursementDetails.Add(model);
                                    _context.SaveChanges();
                                    trx.Complete();
                                }
                            }
                            else
                            {
                                dataAssign.Status = jsonParseReturnCheck.Data.Status;
                                dataAssign.LastSentDate = DateTime.Now;
                                _context.Entry(dataAssign).State = EntityState.Modified;
                                _context.SaveChanges();

                                return Content("Data sudah Verified");
                            }
                        }
                        else { 
                            return Content(GetConfig.AppSetting["GlobalMessage:SistemError"]);
                        }
                    }
                }
                else {
                    using (TransactionScope trx = new TransactionScope())
                    {
                        TblNoticeOfDisbursementDetail model = new TblNoticeOfDisbursementDetail();
                        model.NodId = int.Parse(uniq);
                        model.CreditorRefId = data.CreditorRefId;
                        model.CreditorRef = data.CreditorRef;
                        model.Amount = decimal.Parse(data.Amount.Replace(".", ""));
                        model.AmountIdr = decimal.Parse(data.AmountIDR.Replace(".", ""));
                        model.CreatedTime = DateTime.Now;
                        model.CreatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                        model.IsActive = 1;
                        model.IsDeleted = 0;
                        _context.TblNoticeOfDisbursementDetails.Add(model);
                        _context.SaveChanges();
                        trx.Complete();
                    }
                }
                return Content("");
            }
            catch (Exception ex)
            {
                return Content(GetConfig.AppSetting["GlobalMessage:SistemError"]);
            }
        }

        [HttpPost]
        public ActionResult SubmitCreateFileNodTemp(NoticeOfDisbursementFileUpload_ViewModel data)
        {
            if (!lastSession.Update())
            {
                return RedirectToAction("Login", "Login", new { a = true });
            }

            var location = new Uri($"{Request.Scheme}://{Request.Host}{Request.Path}");
            string Patha = location.AbsolutePath.Replace(this.ControllerContext.RouteData.Values["action"].ToString(), "Index");

            if (!accessSecurity.IsGetAccess(".." + Patha))
            {
                return RedirectToAction("NotAccess", "Error");
            }

            try
            {

                var FilePath = _context.TblMasterSystemParameters.Where(m=>m.Key == "PathPendukung").Select(m=>m.Value).FirstOrDefault();
                if (data.File == null)
                {
                    return Content("Masukkan file terlebih dahulu");
                }

                string FileNameNoExt = Path.GetFileNameWithoutExtension(data.File.FileName);
                string Ext = Path.GetExtension(data.File.FileName);
                string FullName = FileNameNoExt + "-" + DateTime.Now.ToString("ddMMyyyyHHmmss") + Ext;
                string FullPath = FilePath + FullName;

                string[] allowedExtensions = { ".xlsx", ".xls", ".doc", ".docx", ".pdf" };
                if (!allowedExtensions.Contains(Ext))
                {
                    return Content("Extention tidak sesuai");
                }

                using (TransactionScope trx = new TransactionScope())
                {
                    try
                    {
                        TblFileUploadNodTemp model = new TblFileUploadNodTemp();
                        model.FileName = FullName;
                        model.FileSize = Convert.ToInt32(data.File.Length);
                        model.FilePath = FullPath;
                        model.FileExt = Ext;
                        model.UploadTime = DateTime.Now;
                        model.UploadById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                        model.IsActive = 1;
                        model.IsDeleted = 0;
                        model.RandomString = data.uniq;
                        _context.TblFileUploadNodTemps.Add(model);
                        _context.SaveChanges();

                        //Local Only
                        if (!Directory.Exists(FilePath))
                        {
                            Directory.CreateDirectory(FilePath);
                        }

                        using (FileStream fileStream = new FileStream(FullPath, FileMode.Create))
                        {
                            //SAVE FILE
                            data.File.CopyTo(fileStream);
                        }

                        trx.Complete();
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                }

                return Content("");
            }
            catch (Exception ex)
            {
                return Content(GetConfig.AppSetting["GlobalMessage:SistemError"]);
            }
        }

        [HttpPost]
        public ActionResult SubmitCreateFileNod(NoticeOfDisbursement_ViewModel data, string uniq)
        {
            if (!lastSession.Update())
            {
                return RedirectToAction("Login", GetConfig.AppSetting["AppSettings:Subdomain:DomainController"] + "Login", new { a = true });
            }

            var location = new Uri($"{Request.Scheme}://{Request.Host}{Request.Path}");
            string Patha = location.AbsolutePath.Replace(this.ControllerContext.RouteData.Values["action"].ToString(), "Index");

            if (!accessSecurity.IsGetAccess(".." + Patha))
            {
                return RedirectToAction("NotAccess", "Error");
            }

            try
            {
                TblNoticeOfDisbursement dataAssign = _context.TblNoticeOfDisbursements.Where(m => m.Id == int.Parse(uniq) && m.IsDeleted == 0).FirstOrDefault();

                if (dataAssign.IdNodFromApi != null)
                {
                    var jsonParseReturnCheck = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>("");
                    var urlCheck = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:Nod:GetDataById"] + dataAssign.IdNodFromApi;
                    (bool resultApiCheck, string resultCheck) = RequestToAPI.GetJsonStringWebApi(urlCheck, null);
                    if (resultApiCheck && !string.IsNullOrEmpty(resultCheck))
                    {
                        jsonParseReturnCheck = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>(resultCheck);
                        if (jsonParseReturnCheck.StatusCode == 200 || jsonParseReturnCheck.StatusCode == 201)
                        {
                            if (jsonParseReturnCheck.Data.Status == "Unverified")
                            {
                                var FilePath = _context.TblMasterSystemParameters.Where(m => m.Key == "PathPendukung").Select(m => m.Value).FirstOrDefault();
                                if (data.File == null)
                                {
                                    return Content("Masukkan file terlebih dahulu");
                                }

                                string FileNameNoExt = Path.GetFileNameWithoutExtension(data.File.FileName);
                                string Ext = Path.GetExtension(data.File.FileName);
                                string FullName = FileNameNoExt + "-" + DateTime.Now.ToString("ddMMyyyyHHmmss") + Ext;
                                string FullPath = FilePath + FullName;

                                string[] allowedExtensions = { ".xlsx", ".xls", ".doc", ".docx", ".pdf" };
                                if (!allowedExtensions.Contains(Ext))
                                {
                                    return Content("Extention tidak sesuai");
                                }

                                using (TransactionScope trx = new TransactionScope())
                                {
                                    try
                                    {
                                        TblFileUploadNod model = new TblFileUploadNod();
                                        model.IdNod = int.Parse(uniq);
                                        model.FileName = FullName;
                                        model.FileSize = Convert.ToInt32(data.File.Length);
                                        model.FilePath = FullPath;
                                        model.FileExt = Ext;
                                        model.UploadTime = DateTime.Now;
                                        model.UploadById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                                        model.IsActive = 1;
                                        model.IsDeleted = 0;
                                        _context.TblFileUploadNods.Add(model);
                                        _context.SaveChanges();

                                        //Local Only
                                        if (!Directory.Exists(FilePath))
                                        {
                                            Directory.CreateDirectory(FilePath);
                                        }

                                        using (FileStream fileStream = new FileStream(FullPath, FileMode.Create))
                                        {
                                            //SAVE FILE
                                            data.File.CopyTo(fileStream);
                                        }

                                        trx.Complete();
                                    }
                                    catch (Exception ex)
                                    {
                                        throw;
                                    }
                                }
                            }
                            else
                            {
                                var FilePath = _context.TblMasterSystemParameters.Where(m => m.Key == "PathPendukung").Select(m => m.Value).FirstOrDefault();
                                if (data.File == null)
                                {
                                    return Content("Masukkan file terlebih dahulu");
                                }

                                string FileNameNoExt = Path.GetFileNameWithoutExtension(data.File.FileName);
                                string Ext = Path.GetExtension(data.File.FileName);
                                string FullName = FileNameNoExt + "-" + DateTime.Now.ToString("ddMMyyyyHHmmss") + Ext;
                                string FullPath = FilePath + FullName;

                                string[] allowedExtensions = { ".xlsx", ".xls", ".doc", ".docx", ".pdf" };
                                if (!allowedExtensions.Contains(Ext))
                                {
                                    return Content("Extention tidak sesuai");
                                }

                                if (!Directory.Exists(FilePath))
                                {
                                    Directory.CreateDirectory(FilePath);
                                }

                                using (FileStream fileStream = new FileStream(FullPath, FileMode.Create))
                                {
                                    //SAVE FILE
                                    data.File.CopyTo(fileStream);
                                }

                                byte[] fileBytes = System.IO.File.ReadAllBytes(FullPath);

                                //Convert the byte array to a Base64 string
                                string base64String = Convert.ToBase64String(fileBytes);

                                //Base64
                                var jsonParseReturnUploadBase64 = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>("");
                                var urlUploadBase64 = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:Nod:UploadBase64"] + dataAssign.IdNodFromApi;
                                (bool resultApiUploadBase64, string resultUploadBase64) = RequestToAPI.PostRequestToWebApi(urlUploadBase64, new
                                {
                                    FileName = FullName,
                                    FileContent = base64String,
                                }, null);
                                if (resultApiUploadBase64 && !string.IsNullOrEmpty(resultUploadBase64))
                                {
                                    jsonParseReturnUploadBase64 = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>(resultUploadBase64);
                                    if (jsonParseReturnUploadBase64.StatusCode == 200 || jsonParseReturnUploadBase64.StatusCode == 201)
                                    {
                                        using (TransactionScope trx = new TransactionScope())
                                        {
                                            try
                                            {
                                                TblFileUploadNod model = new TblFileUploadNod();
                                                model.IdNod = int.Parse(uniq);
                                                model.IdFileFromApi = jsonParseReturnUploadBase64.Data.Key;
                                                model.FileName = FullName;
                                                model.FileSize = Convert.ToInt32(data.File.Length);
                                                model.FilePath = FullPath;
                                                model.FileExt = Ext;
                                                model.UploadTime = DateTime.Now;
                                                model.UploadById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                                                model.IsActive = 1;
                                                model.IsDeleted = 0;
                                                _context.TblFileUploadNods.Add(model);
                                                _context.SaveChanges();

                                                trx.Complete();
                                            }
                                            catch (Exception ex)
                                            {
                                                throw;
                                            }
                                        }
                                    }
                                }


                                //FormData
                                //using (var httpClient = new HttpClient())
                                //{
                                //    // Create the multipart form data content
                                //    using(var formData = new MultipartFormDataContent("----WebKitFormBoundary7MA4YWxkTrZu0gW"))
                                //    {
                                //        var fileContent = new ByteArrayContent(fileBytes);
                                //        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                                //        fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                                //        {
                                //            Name = "formData",
                                //            FileName = Path.GetFileName(check.FilePath)
                                //        };

                                //        formData.Add(fileContent, "formData", Path.GetFileName(check.FilePath));

                                //        // Set headers
                                //        formData.Headers.ContentType.MediaType = "multipart/form-data";
                                //        httpClient.DefaultRequestHeaders.Add("Cookie", $"cookiesession1=678B294F5DCBE997B72E06BECD898F85");

                                //        // Construct the full URL
                                //        var fullUrl = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:Nod:UploadFile"] + jsonParseReturnAdd.Data.Id;

                                //        var response = await httpClient.PostAsync(fullUrl, formData);
                                //        var responseContent = await response.Content.ReadAsStringAsync();
                                //        var jsonResponse = JObject.Parse(responseContent);
                                //        var datac = jsonResponse["Data"];

                                //        // Check the response status code
                                //        if (response.IsSuccessStatusCode)
                                //        {
                                //            string resultUploadFile = await response.Content.ReadAsStringAsync();

                                //        }
                                //    }
                                //}

                                

                                //dataAssign.Status = jsonParseReturnCheck.Data.Status;
                                //dataAssign.LastSentDate = DateTime.Now;
                                //_context.Entry(dataAssign).State = EntityState.Modified;
                                //_context.SaveChanges();

                                //return Content("Data sudah Verified");
                            }
                        }
                        else
                        {
                            return Content(GetConfig.AppSetting["GlobalMessage:SistemError"]);
                        }
                    }
                }
                else
                {
                    var FilePath = _context.TblMasterSystemParameters.Where(m => m.Key == "PathPendukung").Select(m => m.Value).FirstOrDefault();
                    if (data.File == null)
                    {
                        return Content("Masukkan file terlebih dahulu");
                    }

                    string FileNameNoExt = Path.GetFileNameWithoutExtension(data.File.FileName);
                    string Ext = Path.GetExtension(data.File.FileName);
                    string FullName = FileNameNoExt + "-" + DateTime.Now.ToString("ddMMyyyyHHmmss") + Ext;
                    string FullPath = FilePath + FullName;

                    using (TransactionScope trx = new TransactionScope())
                    {
                        try
                        {
                            TblFileUploadNod model = new TblFileUploadNod();
                            model.IdNod = int.Parse(uniq);
                            model.FileName = FullName;
                            model.FileSize = Convert.ToInt32(data.File.Length);
                            model.FilePath = FullPath;
                            model.FileExt = Ext;
                            model.UploadTime = DateTime.Now;
                            model.UploadById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                            model.IsActive = 1;
                            model.IsDeleted = 0;
                            _context.TblFileUploadNods.Add(model);
                            _context.SaveChanges();

                            //Local Only
                            if (!Directory.Exists(FilePath))
                            {
                                Directory.CreateDirectory(FilePath);
                            }

                            using (FileStream fileStream = new FileStream(FullPath, FileMode.Create))
                            {
                                //SAVE FILE
                                data.File.CopyTo(fileStream);
                            }

                            trx.Complete();
                        }
                        catch (Exception ex)
                        {
                            throw;
                        }
                    }
                }
                return Content("");

            }
            catch (Exception ex)
            {
                return Content(GetConfig.AppSetting["GlobalMessage:SistemError"]);
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


            try
            {
                TblNoticeOfDisbursement nod = _context.TblNoticeOfDisbursements.Where(m => m.Id == id).FirstOrDefault();

                var data = new NoticeOfDisbursement_ViewModel
                {
                    Id = nod.Id,
                    NodNo = nod.NodNo,
                    Cur = _context.TblMasterLookups.Where(m => m.Name == nod.Cur && m.Type == "Currency").Select(m => m.Value).FirstOrDefault().ToString(),
                    NodDate = nod.NodDate,
                    ValueDate = nod.ValueDate,
                    Beneficiary = nod.Beneficiary,
                    IsActive = nod.IsActive,
                };

                if (data.Cur != null && data.Cur != "")
                {
                    ViewBag.Cur = new SelectList(Utility.SelectDataLookupById(int.Parse(data.Cur), "Currency", _context), "id", "text", data.Cur);
                }
                else
                {
                    ViewBag.Cur = new SelectList("", "");
                }

                return PartialView("_Edit", data);
            }
            catch (Exception ex) 
            {
                var data = new NoticeOfDisbursement_ViewModel();

                return PartialView("_Edit", data);
            }
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<ActionResult> SubmitEdit(NoDHeaderDetail_ViewModel model)
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
                    TblNoticeOfDisbursement data = _context.TblNoticeOfDisbursements.Where(m => m.Id == model.Id).FirstOrDefault(); // Ambil data sesuai dengan ID
                    if (data.NodNo != model.NodNo)
                    {
                        TblNoticeOfDisbursement dataNodNo = _context.TblNoticeOfDisbursements.Where(m => m.NodNo == model.NodNo && m.IsDeleted != 1).FirstOrDefault();
                        if (dataNodNo != null)
                        {
                            return Content("Kode sudah terdaftar atas " + dataNodNo.NodNo);
                        }
                    }

                    if (data.IdNodFromApi != null)
                    {
                        var jsonParseReturnCheck = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>("");
                        var urlCheck = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:Nod:GetDataById"] + data.IdNodFromApi;
                        (bool resultApiCheck, string resultCheck) = RequestToAPI.GetJsonStringWebApi(urlCheck, null);
                        if (resultApiCheck && !string.IsNullOrEmpty(resultCheck))
                        {
                            jsonParseReturnCheck = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>(resultCheck);
                            if (jsonParseReturnCheck.StatusCode == 200 || jsonParseReturnCheck.StatusCode == 201)
                            {
                                if (jsonParseReturnCheck.Data.Status == "Verified")
                                {
                                    data.Status = jsonParseReturnCheck.Data.Status;
                                    _context.Entry(data).State = EntityState.Modified;
                                    _context.SaveChanges();
                                    trx.Complete();

                                    return Content("Data sudah Verified");
                                }
                                else
                                {
                                    data.NodNo = model.NodNo;
                                    data.NodDate = DateTime.Parse(model.NodDate.ToString());
                                    data.ValueDate = model.ValueDate;
                                    data.Cur = _context.TblMasterLookups.Where(m => m.Value == int.Parse(model.Cur) && m.Type == "Currency").Select(m => m.Name).FirstOrDefault();
                                    data.Beneficiary = model.Beneficiary;
                                    data.IsActive = model.IsActive = 1;
                                    data.UpdatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                                    data.UpdatedTime = DateTime.Now;
                                    _context.Entry(data).State = EntityState.Modified;
                                    _context.SaveChanges();
                                }
                            }
                            else
                            {
                                return Content(GetConfig.AppSetting["GlobalMessage:SistemError"]);
                            }
                        }

                        
                    }
                    else {
                        data.NodNo = model.NodNo;
                        data.NodDate = DateTime.Parse(model.NodDate.ToString());
                        data.ValueDate = model.ValueDate;
                        data.Cur = _context.TblMasterLookups.Where(m => m.Value == int.Parse(model.Cur) && m.Type == "Currency").Select(m => m.Name).FirstOrDefault();
                        data.Beneficiary = model.Beneficiary;
                        data.IsActive = model.IsActive = 1;
                        data.UpdatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                        data.UpdatedTime = DateTime.Now;
                        _context.Entry(data).State = EntityState.Modified;
                        _context.SaveChanges();
                    }
                    
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

        #region EditView
        public ActionResult EditView(int id)
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
                TblNoticeOfDisbursement nod = _context.TblNoticeOfDisbursements.Where(m => m.Id == id).FirstOrDefault();

                var data = new NoticeOfDisbursement_ViewModel
                {
                    Id = nod.Id,
                    NodNo = nod.NodNo,
                    Cur = _context.TblMasterLookups.Where(m => m.Name == nod.Cur && m.Type == "Currency").Select(m => m.Value).FirstOrDefault().ToString(),
                    NodDate = nod.NodDate,
                    ValueDate = nod.ValueDate,
                    Beneficiary = nod.Beneficiary,
                    IsActive = nod.IsActive,
                };

                if (data.Cur != null && data.Cur != "")
                {
                    ViewBag.Cur = new SelectList(Utility.SelectDataLookupById(int.Parse(data.Cur), "Currency", _context), "id", "text", data.Cur);
                }
                else
                {
                    ViewBag.Cur = new SelectList("", "");
                }

                return PartialView("_EditView", data);
            }
            catch (Exception ex)
            {
                var data = new NoticeOfDisbursement_ViewModel();

                return PartialView("_EditView", data);
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

            try
            {
                TblNoticeOfDisbursement nod = _context.TblNoticeOfDisbursements.Where(m => m.Id == id).FirstOrDefault();

                var data = new NoticeOfDisbursement_ViewModel
                {
                    Id = nod.Id,
                    NodNo = nod.NodNo,
                    Cur = _context.TblMasterLookups.Where(m => m.Name == nod.Cur && m.Type == "Currency").Select(m => m.Value).FirstOrDefault().ToString(),
                    NodDate = nod.NodDate,
                    ValueDate = nod.ValueDate,
                    Beneficiary = nod.Beneficiary,
                    IsActive = nod.IsActive,
                };

                if (data.Cur != null && data.Cur != "")
                {
                    ViewBag.Cur = new SelectList(Utility.SelectDataLookupById(int.Parse(data.Cur), "Currency", _context), "id", "text", data.Cur);
                }
                else
                {
                    ViewBag.Cur = new SelectList("", "");
                }

                return PartialView("_View", data);
            }
            catch (Exception ex)
            {
                var data = new NoticeOfDisbursement_ViewModel();

                return PartialView("_View", data);
            }
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

                List<TblNoticeOfDisbursement> Transaksis = _context.TblNoticeOfDisbursements.Where(x => confirmedDeleteId.Contains(x.Id)).ToList(); //Ambil data sesuai dengan ID
                for (int i = 0; i < confirmedDeleteId.Length; i++)
                {
                    if (Transaksis[i].IdNodFromApi != null)
                    {
                        var jsonParseReturnCheck = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>("");
                        var urlCheck = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:Nod:GetDataById"] + Transaksis[i].IdNodFromApi;
                        (bool resultApiCheck, string resultCheck) = RequestToAPI.GetJsonStringWebApi(urlCheck, null);
                        if (resultApiCheck && !string.IsNullOrEmpty(resultCheck))
                        {
                            jsonParseReturnCheck = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>(resultCheck);
                            if (jsonParseReturnCheck.StatusCode == 200 || jsonParseReturnCheck.StatusCode == 201)
                            {
                                if (jsonParseReturnCheck.Data.Status == "Unverified")
                                {
                                    var jsonParseReturnDelete = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>("");
                                    var urlDelete = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:Nod:Delete"] + Transaksis[i].IdNodFromApi;
                                    (bool resultApiDelete, string resultDelete) = RequestToAPI.DeleteRequestToWebApi(urlDelete, null);
                                    if (resultApiDelete && !string.IsNullOrEmpty(resultDelete))
                                    {
                                        jsonParseReturnDelete = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>(resultDelete);
                                        if (jsonParseReturnDelete.StatusCode == 200 || jsonParseReturnDelete.StatusCode == 201)
                                        {
                                            TblNoticeOfDisbursement data = _context.TblNoticeOfDisbursements.Find(Transaksis[i].Id);
                                            data.IsDeleted = 1;
                                            _context.Entry(data).State = EntityState.Modified;
                                            _context.SaveChanges();
                                        }
                                    }
                                }
                                else
                                {
                                    TblNoticeOfDisbursement data = _context.TblNoticeOfDisbursements.Find(Transaksis[i].Id);
                                    data.Status = jsonParseReturnCheck.Data.Status;
                                    _context.Entry(data).State = EntityState.Modified;
                                    _context.SaveChanges();

                                    return Content("Data sudah Verified");
                                }
                            }
                            else
                            {
                                return Content(GetConfig.AppSetting["GlobalMessage:SistemError"]);
                            }
                        }

                        
                    }
                    else
                    {
                        TblNoticeOfDisbursement data = _context.TblNoticeOfDisbursements.Find(Transaksis[i].Id);
                        data.IsDeleted = 1;
                        _context.Entry(data).State = EntityState.Modified;
                        _context.SaveChanges();
                    }
                }
                return Content("");
            }
            catch
            {
                return Content("Gagal");
            }
        }
        public ActionResult DeleteDetailNodTemp(string Ids)
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

                List<TblNoticeOfDisbursementDetailTemp> Transaksis = _context.TblNoticeOfDisbursementDetailTemps.Where(x => confirmedDeleteId.Contains(x.Id)).ToList(); //Ambil data sesuai dengan ID
                for (int i = 0; i < confirmedDeleteId.Length; i++)
                {
                    TblNoticeOfDisbursementDetailTemp data = _context.TblNoticeOfDisbursementDetailTemps.Find(Transaksis[i].Id);
                    data.IsDeleted = 1; //Jika true data tidak akan ditampilkan dan data masih tersimpan di dalam database
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
        public ActionResult DeleteDetailNod(string Ids)
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

                List<TblNoticeOfDisbursementDetail> Transaksis = _context.TblNoticeOfDisbursementDetails.Where(x => confirmedDeleteId.Contains(x.Id)).ToList(); //Ambil data sesuai dengan ID
                for (int i = 0; i < confirmedDeleteId.Length; i++)
                {
                    if (Transaksis[i].IdNodDetailFromApi != null)
                    {
                        TblNoticeOfDisbursement checkParent = _context.TblNoticeOfDisbursements.Where(x => x.Id == Transaksis[i].NodId).FirstOrDefault();

                        var jsonParseReturnCheck = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>("");
                        var urlCheck = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:Nod:GetDataById"] + checkParent.IdNodFromApi;
                        (bool resultApiCheck, string resultCheck) = RequestToAPI.GetJsonStringWebApi(urlCheck, null);
                        if (resultApiCheck && !string.IsNullOrEmpty(resultCheck))
                        {
                            jsonParseReturnCheck = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>(resultCheck);
                            if (jsonParseReturnCheck.StatusCode == 200 || jsonParseReturnCheck.StatusCode == 201)
                            {
                                if (jsonParseReturnCheck.Data.Status == "Unverified")
                                {
                                    var jsonParseReturnDelete = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>("");
                                    var urlDelete = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:DetailNod:Delete"] + Transaksis[i].IdNodDetailFromApi;
                                    (bool resultApiDelete, string resultDelete) = RequestToAPI.DeleteRequestToWebApi(urlDelete, null);
                                    if (resultApiDelete && !string.IsNullOrEmpty(resultDelete))
                                    {
                                        jsonParseReturnDelete = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>(resultDelete);
                                        if (jsonParseReturnDelete.StatusCode == 200 || jsonParseReturnDelete.StatusCode == 201)
                                        {
                                            TblNoticeOfDisbursementDetail data = _context.TblNoticeOfDisbursementDetails.Find(Transaksis[i].Id);
                                            data.IsActive = 0;
                                            data.IsDeleted = 1;
                                            _context.Entry(data).State = EntityState.Modified;
                                            _context.SaveChanges();
                                        }
                                    }
                                }
                                else
                                {
                                    TblNoticeOfDisbursement data = _context.TblNoticeOfDisbursements.Find(checkParent.IdNodFromApi);
                                    data.Status = jsonParseReturnCheck.Data.Status;
                                    _context.Entry(data).State = EntityState.Modified;
                                    _context.SaveChanges();

                                    return Content("Data sudah Verified");
                                }
                            }
                            else
                            {
                                return Content(GetConfig.AppSetting["GlobalMessage:SistemError"]);
                            }
                        }
                    }
                    else
                    {
                        TblNoticeOfDisbursementDetail data = _context.TblNoticeOfDisbursementDetails.Find(Transaksis[i].Id);
                        data.IsDeleted = 1;
                        _context.Entry(data).State = EntityState.Modified;
                        _context.SaveChanges();
                    }
                }
                return Content("");
            }
            catch
            {
                return Content("Gagal");
            }
        }
        public ActionResult DeleteFileNodTemp(string Ids)
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

                List<TblFileUploadNodTemp> Transaksis = _context.TblFileUploadNodTemps.Where(x => confirmedDeleteId.Contains(x.Id)).ToList(); //Ambil data sesuai dengan ID
                for (int i = 0; i < confirmedDeleteId.Length; i++)
                {
                    TblFileUploadNodTemp data = _context.TblFileUploadNodTemps.Find(Transaksis[i].Id);
                    data.IsDeleted = 1; //Jika true data tidak akan ditampilkan dan data masih tersimpan di dalam database
                    if (System.IO.File.Exists(Transaksis[i].FilePath))
                    {
                        System.IO.File.Delete(Transaksis[i].FilePath);
                    }
                    _context.RemoveRange(data);
                    _context.SaveChanges();
                }
                return Content("");
            }
            catch
            {
                return Content("Gagal");
            }
        }
        public ActionResult DeleteFileNod(string Ids)
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

                List<TblFileUploadNod> Transaksis = _context.TblFileUploadNods.Where(x => confirmedDeleteId.Contains(x.Id)).ToList(); //Ambil data sesuai dengan ID
                for (int i = 0; i < confirmedDeleteId.Length; i++)
                {
                    if (Transaksis[i].IdFileFromApi != null)
                    {
                        TblNoticeOfDisbursement checkParent = _context.TblNoticeOfDisbursements.Where(x => x.Id == Transaksis[i].IdNod).FirstOrDefault();

                        var jsonParseReturnCheck = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>("");
                        var urlCheck = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:Nod:GetDataById"] + checkParent.IdNodFromApi;
                        (bool resultApiCheck, string resultCheck) = RequestToAPI.GetJsonStringWebApi(urlCheck, null);
                        if (resultApiCheck && !string.IsNullOrEmpty(resultCheck))
                        {
                            jsonParseReturnCheck = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>(resultCheck);
                            if (jsonParseReturnCheck.StatusCode == 200 || jsonParseReturnCheck.StatusCode == 201)
                            {
                                if (jsonParseReturnCheck.Data.Status == "Unverified")
                                {
                                    var jsonParseReturnDelete = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>("");
                                    var urlDelete = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:File:Delete"] + Transaksis[i].IdFileFromApi;
                                    (bool resultApiDelete, string resultDelete) = RequestToAPI.DeleteRequestToWebApi(urlDelete, null);
                                    if (resultApiDelete && !string.IsNullOrEmpty(resultDelete))
                                    {
                                        jsonParseReturnDelete = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>(resultDelete);
                                        if (jsonParseReturnDelete.StatusCode == 200 || jsonParseReturnDelete.StatusCode == 201)
                                        {
                                            TblFileUploadNod data = _context.TblFileUploadNods.Find(Transaksis[i].Id);
                                            data.IsActive = 0; 
                                            data.IsDeleted = 1; //Jika true data tidak akan ditampilkan dan data masih tersimpan di dalam database
                                            if (System.IO.File.Exists(Transaksis[i].FilePath))
                                            {
                                                System.IO.File.Delete(Transaksis[i].FilePath);
                                            }
                                            _context.Entry(data).State = EntityState.Modified;
                                            _context.SaveChanges();
                                        }
                                    }

                                    if (resultApiDelete)
                                    {
                                        TblFileUploadNod data = _context.TblFileUploadNods.Find(Transaksis[i].Id);
                                        data.IsActive = 0;
                                        data.IsDeleted = 1; //Jika true data tidak akan ditampilkan dan data masih tersimpan di dalam database
                                        if (System.IO.File.Exists(Transaksis[i].FilePath))
                                        {
                                            System.IO.File.Delete(Transaksis[i].FilePath);
                                        }
                                        _context.Entry(data).State = EntityState.Modified;
                                        _context.SaveChanges();
                                    }
                                }
                                else
                                {
                                    var jsonParseReturnDelete = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>("");
                                    var urlDelete = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:File:Delete"] + Transaksis[i].IdFileFromApi;
                                    (bool resultApiDelete, string resultDelete) = RequestToAPI.DeleteRequestToWebApi(urlDelete, null);
                                    if (resultApiDelete && !string.IsNullOrEmpty(resultDelete))
                                    {
                                        jsonParseReturnDelete = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>(resultDelete);
                                        if (jsonParseReturnDelete.StatusCode == 200 || jsonParseReturnDelete.StatusCode == 201)
                                        {
                                            TblFileUploadNod data = _context.TblFileUploadNods.Find(Transaksis[i].Id);
                                            data.IsActive = 0;
                                            data.IsDeleted = 1; //Jika true data tidak akan ditampilkan dan data masih tersimpan di dalam database
                                            if (System.IO.File.Exists(Transaksis[i].FilePath))
                                            {
                                                System.IO.File.Delete(Transaksis[i].FilePath);
                                            }
                                            _context.Entry(data).State = EntityState.Modified;
                                            _context.SaveChanges();
                                        }
                                    }

                                    if (resultApiDelete)
                                    {
                                        TblFileUploadNod data = _context.TblFileUploadNods.Find(Transaksis[i].Id);
                                        data.IsActive = 0;
                                        data.IsDeleted = 1; //Jika true data tidak akan ditampilkan dan data masih tersimpan di dalam database
                                        if (System.IO.File.Exists(Transaksis[i].FilePath))
                                        {
                                            System.IO.File.Delete(Transaksis[i].FilePath);
                                        }
                                        _context.Entry(data).State = EntityState.Modified;
                                        _context.SaveChanges();
                                    }

                                    //TblNoticeOfDisbursement data = _context.TblNoticeOfDisbursements.Find(checkParent.IdNodFromApi);
                                    //data.Status = jsonParseReturnCheck.Data.Status;
                                    //_context.Entry(data).State = EntityState.Modified;
                                    //_context.SaveChanges();

                                    //return Content("Data sudah Verified");
                                }
                            }
                            else
                            {
                                return Content(GetConfig.AppSetting["GlobalMessage:SistemError"]);
                            }
                        }
                    }
                    else
                    {
                        TblFileUploadNod data = _context.TblFileUploadNods.Find(Transaksis[i].Id);
                        data.IsDeleted = 1; //Jika true data tidak akan ditampilkan dan data masih tersimpan di dalam database
                        if (System.IO.File.Exists(Transaksis[i].FilePath))
                        {
                            System.IO.File.Delete(Transaksis[i].FilePath);
                        }
                        _context.Entry(data).State = EntityState.Modified;
                        _context.SaveChanges();
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

        #region Send to Deva
        [HttpPost]
        public async Task<IActionResult> SendToDeva(string Ids)
        {
            if (!lastSession.Update())
            {
                return RedirectToAction("Login", GetConfig.AppSetting["AppSettings:Subdomain:DomainController"] + "Login", new { a = true });
            }
            var location = new Uri($"{Request.Scheme}://{Request.Host}{Request.Path}");
            string path = location.AbsolutePath.Replace(this.ControllerContext.RouteData.Values["action"].ToString(), "Index");
            if (!accessSecurity.IsGetAccess(".." + path))
            {
                return RedirectToAction("NotAccess", "Error");
            }

            try
            {
                var regex = await RegexRequest.RegexValidation(Ids);
                if (!regex)
                {
                    return Content("Bad Request!");
                }

                var send = new SendToDeva(_converter);
                Task.Run(() => send.SendToDevaNod(Ids));
                return Content("Data sedang di kirim ke Deva, Mohon Cek list Notice Of Disbursment secara berkala!");
            }
            catch (Exception ex)
            {
                return Content("Gagal Kirim data ke Deva");
            }
        }

        //No Use
        [HttpPost]
        public async Task<ActionResult> SendToDevaOld(string Ids)
        {
            if (!lastSession.Update())
            {
                return RedirectToAction("Login", GetConfig.AppSetting["AppSettings:Subdomain:DomainController"] + "Login", new { a = true });
            }
            var location = new Uri($"{Request.Scheme}://{Request.Host}{Request.Path}");
            string Patha = location.AbsolutePath.Replace(this.ControllerContext.RouteData.Values["action"].ToString(), "Index");
            if (!accessSecurity.IsGetAccess(".." + Patha))
            {
                return RedirectToAction("NotAccess", "Error");
            }

            try
            {
                string[] ArrayIds = Ids.Split(',');

                foreach (var item in ArrayIds)
                {
                    TblNoticeOfDisbursement dataAssign = _context.TblNoticeOfDisbursements.Where(m => m.Id == int.Parse(item) && m.IsDeleted == 0).FirstOrDefault();

                    NoticeOfDisbursementToAPI_ViewModels model = new NoticeOfDisbursementToAPI_ViewModels();
                    model.NodNo = dataAssign.NodNo;
                    model.NodDate = dataAssign.NodDate;
                    model.ValueDate = dataAssign.ValueDate;
                    model.Cur = dataAssign.Cur;
                    model.NodDetail = _context.TblNoticeOfDisbursementDetails.Where(m => m.NodId == int.Parse(item) && m.IsDeleted == 0).ToList();

                    //Hit API Deva
                    if (dataAssign.IdNodFromApi != null)
                    {
                        var jsonParseReturnCheck = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>("");
                        var urlCheck = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:Nod:GetDataById"] + dataAssign.IdNodFromApi;
                        (bool resultApiCheck, string resultCheck) = RequestToAPI.GetJsonStringWebApi(urlCheck, null);
                        if (resultApiCheck && !string.IsNullOrEmpty(resultCheck))
                        {
                            jsonParseReturnCheck = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>(resultCheck);
                            if (jsonParseReturnCheck.StatusCode == 200 || jsonParseReturnCheck.StatusCode == 201)
                            {
                                if (jsonParseReturnCheck.Data.Status == "Unverified")
                                {
                                    //Send Detail NOD
                                    List<TblNoticeOfDisbursementDetail> dataAssignDetail = _context.TblNoticeOfDisbursementDetails.Where(m => m.NodId == int.Parse(item) && m.IsDeleted == 0).ToList();
                                    List<TblNoticeOfDisbursementDetail> dataAssignDetailDeleted = _context.TblNoticeOfDisbursementDetails.Where(m => m.NodId == int.Parse(item) && m.IdNodDetailFromApi != null && m.IsDeleted == 1 && m.IsActive == 1).ToList();
                                    //Delete Detail NOD
                                    foreach (var itemDetailDelete in dataAssignDetailDeleted)
                                    {
                                        if (itemDetailDelete.IdNodDetailFromApi != null)
                                        {
                                            var jsonParseReturnUpdateDetail = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>("");
                                            var urlUpdateDetail = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:DetailNod:Delete"] + itemDetailDelete.IdNodDetailFromApi;
                                            (bool resultApiUpdateDetail, string resultUpdateDetail) = RequestToAPI.DeleteRequestToWebApi(urlUpdateDetail, null);
                                            if (resultApiUpdateDetail && !string.IsNullOrEmpty(resultUpdateDetail))
                                            {
                                                jsonParseReturnUpdateDetail = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>(resultUpdateDetail);
                                                if (jsonParseReturnUpdateDetail.StatusCode == 200 || jsonParseReturnUpdateDetail.StatusCode == 201)
                                                {
                                                    itemDetailDelete.IsActive = 0;
                                                    itemDetailDelete.IsDeleted = 1;
                                                    _context.Entry(itemDetailDelete).State = EntityState.Modified;
                                                    _context.SaveChanges();
                                                }
                                            }
                                        }
                                    }
                                    //Update Detail NOD
                                    foreach (var itemDetail in dataAssignDetail)
                                    {
                                        //CHECK DetailNOD Registered or No
                                        if (itemDetail.IdNodDetailFromApi != null)
                                        {
                                            var jsonParseReturnUpdateDetail = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>("");
                                            var urlUpdateDetail = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:DetailNod:Update"] + itemDetail.IdNodDetailFromApi;
                                            (bool resultApiUpdateDetail, string resultUpdateDetail) = RequestToAPI.PutRequestToWebApi(urlUpdateDetail, new
                                            {
                                                NodId = dataAssign.IdNodFromApi,
                                                CreditorRef = itemDetail.CreditorRef,
                                                Amount = itemDetail.Amount,
                                                AmountIDR = itemDetail.AmountIdr
                                            }, null);
                                            if (resultApiUpdateDetail && !string.IsNullOrEmpty(resultUpdateDetail))
                                            {
                                                jsonParseReturnUpdateDetail = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>(resultUpdateDetail);
                                                if (jsonParseReturnUpdateDetail.StatusCode == 200 || jsonParseReturnUpdateDetail.StatusCode == 201)
                                                {
                                                    itemDetail.IdNodDetailFromApi = jsonParseReturnUpdateDetail.Data.Id;
                                                    _context.Entry(itemDetail).State = EntityState.Modified;
                                                    _context.SaveChanges();
                                                }
                                            }

                                            
                                        }
                                        else
                                        {
                                            var jsonParseReturnAddDetail = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>("");
                                            var urlAddDetail = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:DetailNod:Add"];
                                            (bool resultApiAddDetail, string resultAddDetail) = RequestToAPI.PostRequestToWebApi(urlAddDetail, new
                                            {
                                                NodId = dataAssign.IdNodFromApi,
                                                CreditorRef = itemDetail.CreditorRef,
                                                Amount = itemDetail.Amount,
                                                AmountIDR = itemDetail.AmountIdr
                                            }, null);
                                            if (resultApiAddDetail && !string.IsNullOrEmpty(resultAddDetail))
                                            {
                                                jsonParseReturnAddDetail = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>(resultAddDetail);
                                                if (jsonParseReturnAddDetail.StatusCode == 200 || jsonParseReturnAddDetail.StatusCode == 201)
                                                {
                                                    itemDetail.IdNodDetailFromApi = jsonParseReturnAddDetail.Data.Id;
                                                    _context.Entry(itemDetail).State = EntityState.Modified;
                                                    _context.SaveChanges();
                                                }
                                                //else
                                                //{
                                                //    //Delete Detail
                                                //    itemDetail.IsDeleted = 1;
                                                //    _context.Entry(itemDetail).State = EntityState.Modified;
                                                //    _context.SaveChanges();
                                                //}
                                            }
                                        }
                                    }
                                    
                                    //Send File NOD
                                    List<TblFileUploadNod> dataAssignFile = _context.TblFileUploadNods.Where(m => m.IdNod == int.Parse(item) && m.IsDeleted == 0).ToList();
                                    List<TblFileUploadNod> dataAssignFileDeleted = _context.TblFileUploadNods.Where(m => m.IdNod == int.Parse(item) && m.IdFileFromApi != null && m.IsDeleted == 1 && m.IsActive == 1).ToList();
                                    //Delete File NOD
                                    foreach (var itemFileDelete in dataAssignFileDeleted)
                                    {
                                        if (itemFileDelete.IdFileFromApi != null)
                                        {
                                            var jsonParseReturnUpdateFile = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>("");
                                            var urlUpdateFile = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:File:Delete"] + itemFileDelete.IdFileFromApi;
                                            (bool resultApiUpdateFile, string resultUpdateFile) = RequestToAPI.DeleteRequestToWebApi(urlUpdateFile, null);
                                            if (resultApiUpdateFile && !string.IsNullOrEmpty(resultUpdateFile))
                                            {
                                                jsonParseReturnUpdateFile = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>(resultUpdateFile);
                                                if (jsonParseReturnUpdateFile.StatusCode == 200 || jsonParseReturnUpdateFile.StatusCode == 201)
                                                {
                                                    itemFileDelete.IsActive = 0;
                                                    itemFileDelete.IsDeleted = 1;
                                                    _context.Entry(itemFileDelete).State = EntityState.Modified;
                                                    _context.SaveChanges();
                                                }
                                            }
                                            if (resultApiUpdateFile)
                                            {
                                                itemFileDelete.IsActive = 0;
                                                itemFileDelete.IsDeleted = 1;
                                                _context.Entry(itemFileDelete).State = EntityState.Modified;
                                                _context.SaveChanges();
                                            }
                                        }
                                    }
                                    //Update Detail NOD
                                    foreach (var itemFile in dataAssignFile)
                                    {
                                        var data = new object();
                                        var check = await _context.TblFileUploadNods.Where(x => x.Id == itemFile.Id).FirstOrDefaultAsync();
                                        if (System.IO.File.Exists(check.FilePath))
                                        {
                                            byte[] fileBytes = System.IO.File.ReadAllBytes(check.FilePath);

                                            // Convert the byte array to a Base64 string
                                            string base64String = Convert.ToBase64String(fileBytes);

                                            var jsonParseReturnUploadBase64 = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>("");
                                            var urlUploadBase64 = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:Nod:UploadBase64"] + dataAssign.IdNodFromApi;
                                            (bool resultApiUploadBase64, string resultUploadBase64) = RequestToAPI.PostRequestToWebApi(urlUploadBase64, new
                                            {
                                                FileName = itemFile.FileName,
                                                FileContent = base64String,
                                            }, null);
                                            if (resultApiUploadBase64 && !string.IsNullOrEmpty(resultUploadBase64))
                                            {
                                                jsonParseReturnUploadBase64 = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>(resultUploadBase64);
                                                if (jsonParseReturnUploadBase64.StatusCode == 200 || jsonParseReturnUploadBase64.StatusCode == 201)
                                                {
                                                    itemFile.IdFileFromApi = jsonParseReturnUploadBase64.Data.Key;
                                                    _context.Entry(itemFile).State = EntityState.Modified;
                                                    _context.SaveChanges();
                                                }
                                            }
                                        }
                                    }
                                    

                                    //Send Update NOD
                                    var jsonParseReturnUpdate = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>("");
                                    var urlUpdate = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:Nod:Update"] + dataAssign.IdNodFromApi;
                                    (bool resultApiUpdate, string resultUpdate) = RequestToAPI.PutRequestToWebApi(urlUpdate, model, null);
                                    if (resultApiUpdate && !string.IsNullOrEmpty(resultUpdate))
                                    {
                                        jsonParseReturnUpdate = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>(resultUpdate);
                                        if (jsonParseReturnUpdate.StatusCode == 200 || jsonParseReturnUpdate.StatusCode == 201)
                                        {
                                            dataAssign.Status = jsonParseReturnUpdate.Data.Status;
                                            dataAssign.LastSentDate = DateTime.Now;
                                            _context.Entry(dataAssign).State = EntityState.Modified;
                                            _context.SaveChanges();
                                        }
                                    }
                                }
                                else
                                {
                                    dataAssign.Status = jsonParseReturnCheck.Data.Status;
                                    dataAssign.LastSentDate = DateTime.Now;
                                    _context.Entry(dataAssign).State = EntityState.Modified;
                                    _context.SaveChanges();
                                }
                            }
                        }
                    }
                    else
                    {
                        var jsonParseReturnAdd = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>("");
                        var urlAdd = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:Nod:Add"];
                        (bool resultApiAdd, string resultAdd) = RequestToAPI.PostRequestToWebApi(urlAdd, model, null);
                        if (resultApiAdd && !string.IsNullOrEmpty(resultAdd))
                        {
                            jsonParseReturnAdd = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>(resultAdd);
                            if (jsonParseReturnAdd.StatusCode == 200 || jsonParseReturnAdd.StatusCode == 201)
                            {
                                dataAssign.IdNodFromApi = jsonParseReturnAdd.Data.Id;
                                dataAssign.Status = jsonParseReturnAdd.Data.Status;
                                dataAssign.LastSentDate = DateTime.Now;
                                _context.Entry(dataAssign).State = EntityState.Modified;
                                _context.SaveChanges();

                                //NOD Detail Send
                                List<TblNoticeOfDisbursementDetail> dataAssignDetail = _context.TblNoticeOfDisbursementDetails.Where(m => m.NodId == int.Parse(item) && m.IsDeleted == 0).ToList();
                                foreach (var itemDetail in dataAssignDetail)
                                {
                                    //CHECK DetailNOD Registered or No
                                    var jsonParseReturnAddDetail = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>("");
                                    var urlAddDetail = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:DetailNod:Add"];
                                    (bool resultApiAddDetail, string resultAddDetail) = RequestToAPI.PostRequestToWebApi(urlAddDetail, new
                                    {
                                        NodId = jsonParseReturnAdd.Data.Id,
                                        CreditorRef = itemDetail.CreditorRef,
                                        Amount = itemDetail.Amount,
                                        AmountIDR = itemDetail.AmountIdr
                                    }, null);
                                    if (resultApiAddDetail && !string.IsNullOrEmpty(resultAddDetail))
                                    {
                                        jsonParseReturnAddDetail = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>(resultAddDetail);
                                        if (jsonParseReturnAddDetail.StatusCode == 200 || jsonParseReturnAddDetail.StatusCode == 201)
                                        {
                                            itemDetail.IdNodDetailFromApi = jsonParseReturnAddDetail.Data.Id;
                                            _context.Entry(itemDetail).State = EntityState.Modified;
                                            _context.SaveChanges();
                                        }
                                        else
                                        {
                                            itemDetail.IsDeleted = 1;
                                            _context.Entry(itemDetail).State = EntityState.Modified;
                                            _context.SaveChanges();
                                        }
                                    }
                                }

                                //NOD File Send
                                List<TblFileUploadNod> dataAssignFile = _context.TblFileUploadNods.Where(m => m.IdNod == int.Parse(item) && m.IsDeleted == 0).ToList();
                                foreach (var itemFile in dataAssignFile)
                                {
                                    var data = new object();
                                    var check = await _context.TblFileUploadNods.Where(x => x.Id == itemFile.Id).FirstOrDefaultAsync();
                                    if (System.IO.File.Exists(check.FilePath))
                                    {
                                        byte[] fileBytes = System.IO.File.ReadAllBytes(check.FilePath);

                                        //Convert the byte array to a Base64 string
                                        string base64String = Convert.ToBase64String(fileBytes);

                                        //Base64
                                        var jsonParseReturnUploadBase64 = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>("");
                                        var urlUploadBase64 = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:Nod:UploadBase64"] + jsonParseReturnAdd.Data.Id;
                                        (bool resultApiUploadBase64, string resultUploadBase64) = RequestToAPI.PostRequestToWebApi(urlUploadBase64, new
                                        {
                                            FileName = itemFile.FileName,
                                            FileContent = base64String,
                                        }, null);
                                        if (resultApiUploadBase64 && !string.IsNullOrEmpty(resultUploadBase64))
                                        {
                                            jsonParseReturnUploadBase64 = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>(resultUploadBase64);
                                            if (jsonParseReturnUploadBase64.StatusCode == 200 || jsonParseReturnUploadBase64.StatusCode == 201)
                                            {
                                                itemFile.IdFileFromApi = jsonParseReturnUploadBase64.Data.Key;
                                                _context.Entry(itemFile).State = EntityState.Modified;
                                                _context.SaveChanges();
                                            }
                                        }


                                        //FormData
                                        //using (var httpClient = new HttpClient())
                                        //{
                                        //    // Create the multipart form data content
                                        //    using(var formData = new MultipartFormDataContent("----WebKitFormBoundary7MA4YWxkTrZu0gW"))
                                        //    {
                                        //        var fileContent = new ByteArrayContent(fileBytes);
                                        //        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                                        //        fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                                        //        {
                                        //            Name = "formData",
                                        //            FileName = Path.GetFileName(check.FilePath)
                                        //        };

                                        //        formData.Add(fileContent, "formData", Path.GetFileName(check.FilePath));

                                        //        // Set headers
                                        //        formData.Headers.ContentType.MediaType = "multipart/form-data";
                                        //        httpClient.DefaultRequestHeaders.Add("Cookie", $"cookiesession1=678B294F5DCBE997B72E06BECD898F85");

                                        //        // Construct the full URL
                                        //        var fullUrl = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:Nod:UploadFile"] + jsonParseReturnAdd.Data.Id;

                                        //        var response = await httpClient.PostAsync(fullUrl, formData);
                                        //        var responseContent = await response.Content.ReadAsStringAsync();
                                        //        var jsonResponse = JObject.Parse(responseContent);
                                        //        var datac = jsonResponse["Data"];

                                        //        // Check the response status code
                                        //        if (response.IsSuccessStatusCode)
                                        //        {
                                        //            string resultUploadFile = await response.Content.ReadAsStringAsync();

                                        //        }
                                        //    }
                                        //}
                                    }
                                }
                            }
                        }
                    }
                }
                return Content("");
            }
            catch (Exception ex)
            {
                return Content("Data Gagal Di kirim");
            }
        }
        #endregion

        #region Check to Deva
        [HttpPost]
        public async Task CheckToDeva()
        {
            try
            {
                List<TblNoticeOfDisbursement> data = _context.TblNoticeOfDisbursements.Where(m => m.Status == "Unverified").ToList();

                List<string> dataId = _context.TblNoticeOfDisbursements.Where(m => m.Status == "Unverified").Select(m => m.IdNodFromApi).ToList();

                string formattedData = $"('{string.Join("','", dataId)}')";

                var jsonParseReturnCheck = JsonConvert.DeserializeObject<ResultStatusDataInt<NoticeOfDisbursementJOB_ViewModel>>("");
                var urlCheck = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:Nod:GetData"] + "filter=Id in" + formattedData;
                (bool resultApiCheck, string resultCheck) = RequestToAPI.GetJsonStringWebApi(urlCheck, null);
                if (resultApiCheck && !string.IsNullOrEmpty(resultCheck))
                {
                    jsonParseReturnCheck = JsonConvert.DeserializeObject<ResultStatusDataInt<NoticeOfDisbursementJOB_ViewModel>>(resultCheck);

                    if (jsonParseReturnCheck.Value != null)
                    {
                        foreach (var item in data)
                        {
                            var updatedStatus = jsonParseReturnCheck.Value.FirstOrDefault(m => m.Id == item.IdNodFromApi);
                            if (updatedStatus != null)
                            {
                                item.Status = updatedStatus.Status;
                                item.UpdatedTime = DateTime.Now;
                                _context.Entry(item).State = EntityState.Modified;
                                _context.SaveChanges();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        [HttpPost]
        public async Task CheckToDevaOld()
        {
            try
            {
                List<TblNoticeOfDisbursement> data = _context.TblNoticeOfDisbursements.Where(m => m.Status == "Unverified").ToList();

                foreach (var item in data)
                {
                    //Hit API Deva
                    if (item.IdNodFromApi != null)
                    {
                        var dataAssign = await _context.TblNoticeOfDisbursements.Where(m => m.Id == item.Id && m.IsDeleted == 0).FirstOrDefaultAsync();

                        var jsonParseReturnCheck = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>("");
                        var urlCheck = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:Nod:GetData"] + dataAssign.IdNodFromApi;
                        (bool resultApiCheck, string resultCheck) = RequestToAPI.GetJsonStringWebApi(urlCheck, null);
                        if (resultApiCheck && !string.IsNullOrEmpty(resultCheck))
                        {
                            jsonParseReturnCheck = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNod>>(resultCheck);
                            if (jsonParseReturnCheck.StatusCode == 200 || jsonParseReturnCheck.StatusCode == 201)
                            {
                                if (jsonParseReturnCheck.Data.Status == "Verified")
                                {
                                    item.Status = jsonParseReturnCheck.Data.Status;
                                    _context.Entry(item).State = EntityState.Modified;
                                    _context.SaveChanges();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        #endregion

        #region Export File
        public async Task<IActionResult> PrintExcelNoD(string Tanggal, string TypeExcel)
        {
            if (!lastSession.Update())
            {
                return RedirectToAction("Login", GetConfig.AppSetting["AppSettings:Subdomain:DomainController"] + "Login", new { a = true });
            }
            var location = new Uri($"{Request.Scheme}://{Request.Host}{Request.Path}");
            string path = location.AbsolutePath.Replace(this.ControllerContext.RouteData.Values["action"].ToString(), "Index");
            if (!accessSecurity.IsGetAccess(".." + path))
            {
                return RedirectToAction("NotAccess", "Error");
            }

            var insertData = new TblDownloadBigFile();

            try
            {
                var req = new { TypeExcel = TypeExcel };

                var regex = await RegexRequest.RegexValidation(req);
                if (!regex)
                {
                    return Content("Bad Request!");
                }

                insertData.Path = GetConfig.AppSetting["Path"] + "Export_Excel_NoD_" + DateTime.Now.ToString("ddMMyyyyHHmmssfff") + "." + TypeExcel;
                insertData.FileName = "Export_Excel_NoD_" + DateTime.Now.ToString("ddMMyyyyHHmmssfff");
                insertData.StatusDownload = 0;
                insertData.FileExt = "xlsx";
                insertData.CreatedTime = DateTime.Now;
                insertData.CreatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                await _context.TblDownloadBigFiles.AddAsync(insertData);
                await _context.SaveChangesAsync();

                var print = new PrintExport(_converter);
                Task.Run(() => print.PrintNoDExcel(TypeExcel, insertData.FileName, insertData.Id, Tanggal));
                return Content("Data Berhasil di Request untuk Download, Mohon Cek di Menu Download List File!");
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task<IActionResult> PrintPDFNoD(string Tanggal)
        {
            if (!lastSession.Update())
            {
                return RedirectToAction("Login", GetConfig.AppSetting["AppSettings:Subdomain:DomainController"] + "Login", new { a = true });
            }
            var location = new Uri($"{Request.Scheme}://{Request.Host}{Request.Path}");
            string path = location.AbsolutePath.Replace(this.ControllerContext.RouteData.Values["action"].ToString(), "Index");
            if (!accessSecurity.IsGetAccess(".." + path))
            {
                return RedirectToAction("NotAccess", "Error");
            }

            var insertData = new TblDownloadBigFile();

            try
            {
                var req = new { tanggal = Tanggal};

                var regex = await RegexRequest.RegexValidation(req);
                if (!regex)
                {
                    return Content("Bad Request!");
                }



                insertData.Path = GetConfig.AppSetting["Path"] + "Export_Pdf_NoD_" + DateTime.Now.ToString("ddMMyyyyHHmmssfff") + ".pdf";
                insertData.FileName = "Export_Pdf_NoD_" + DateTime.Now.ToString("ddMMyyyyHHmmssfff");
                insertData.StatusDownload = 0;
                insertData.FileExt = "pdf";
                insertData.CreatedTime = DateTime.Now;
                insertData.CreatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                await _context.TblDownloadBigFiles.AddAsync(insertData);
                await _context.SaveChangesAsync();

                var print = new PrintExport(_converter);
                var namaPegawai = HttpContext.Session.GetString(SessionConstan.Session_Name);
                var requestScheme = Request.Scheme;
                var requestHost = Request.Host.ToString();
                Task.Run(() => print.PrintNoDPDF(Tanggal, namaPegawai, requestScheme, requestHost, insertData.Id));
                return Content("Data Berhasil di Request untuk Download, Mohon Cek di Menu Download List File!");

            }
            catch (Exception ex)
            {
                var log = new TblLogErrorPrint();
                log.IdFile = insertData.Id;
                log.ErrorMessage = ex.Message + " - " + ex.InnerException;
                log.CreatedTime = DateTime.Now;
                await _context.TblLogErrorPrints.AddAsync(log);
                await _context.SaveChangesAsync();
                throw;
            }
        }
        #endregion

        #region Download File Pendukung
        public async Task<object> DownloadFilePendukungTemp(int id)
        {

            if (!lastSession.Update())
            {
                return RedirectToAction("Login", "Login", new { a = true });
            }
            var location = new Uri($"{Request.Scheme}://{Request.Host}{Request.Path}");
            string Patha = location.AbsolutePath.Replace(this.ControllerContext.RouteData.Values["action"].ToString(), "Index");
            if (!accessSecurity.IsGetAccess(".." + Patha))
            {
                return RedirectToAction("NotAccess", "Error");
            }

            try
            {
                //Local
                var data = new object();
                var check = await _context.TblFileUploadNodTemps.Where(x => x.Id == id).FirstOrDefaultAsync();
                if (System.IO.File.Exists(check.FilePath))
                {
                    // Read the file into a byte array
                    byte[] fileBytes = System.IO.File.ReadAllBytes(check.FilePath);

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
            }
            catch (Exception Ex)
            {
                return Content(Ex.Message.ToString());
            }
        }
        public async Task<object> DownloadFilePendukung(int id)
        {

            if (!lastSession.Update())
            {
                return RedirectToAction("Login", "Login", new { a = true });
            }
            var location = new Uri($"{Request.Scheme}://{Request.Host}{Request.Path}");
            string Patha = location.AbsolutePath.Replace(this.ControllerContext.RouteData.Values["action"].ToString(), "Index");
            if (!accessSecurity.IsGetAccess(".." + Patha))
            {
                return RedirectToAction("NotAccess", "Error");
            }

            try
            {
                //Local
                var data = new object();
                var check = await _context.TblFileUploadNods.Where(x => x.Id == id).FirstOrDefaultAsync();
                if (System.IO.File.Exists(check.FilePath))
                {
                    // Read the file into a byte array
                    byte[] fileBytes = System.IO.File.ReadAllBytes(check.FilePath);

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
            }
            catch (Exception Ex)
            {
                return Content(Ex.Message.ToString());
            }
        }
        #endregion

        #region CheckCreditRef
        public bool CheckCreditorRef(string CreditorRef)
        {
            //lastSession.Update();
            ListDataDropdownServerSideDeva source = new ListDataDropdownServerSideDeva();

            var Search = CreditorRef == null ? "" : CreditorRef;
            try
            {
                var url = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:CreditorRef"] + "top=" + 1 + "&skip=" + 0 + "&orderby=CreditorRef asc&filter=contains(CreditorRef , '" + Search + "')";
                (bool resultApi, string result) = RequestToAPI.GetJsonStringWebApi(url, null);
                if (resultApi && !string.IsNullOrEmpty(result))
                {
                    var jsonParseReturn = JsonConvert.DeserializeObject<ResultStatusDataInt<DataDropdownServerSideDeva>>(result);

                    if (jsonParseReturn.Value.Count() != 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else { 
                    return false;
                }
            }
            catch (Exception Ex)
            {
                return false;
            }
        }
        #endregion
    }
}
