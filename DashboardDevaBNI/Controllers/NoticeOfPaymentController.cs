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
using Amazon.Auth.AccessControlPolicy;
using iText.Kernel.Pdf.Canvas.Parser.ClipperLib;
using ThirdParty.Json.LitJson;

namespace DashboardDevaBNI.Controllers
{
    public class NoticeOfPaymentController : Controller
    {
        private readonly DbDashboardDevaBniContext _context;
        private readonly IConfiguration _configuration;
        private readonly IConverter _converter;
        private readonly LastSessionLog lastSession;
        private readonly AccessSecurity accessSecurity;
        public NoticeOfPaymentController(IConfiguration config, DbDashboardDevaBniContext context, IHttpContextAccessor accessor)
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
                var NopNoSearchParam = dict["columns[2][search][value]"];
                var DueDate = dict["columns[3][search][value]"];


                //Untuk mengetahui info jumlah page dan total skip data
                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                int recordsTotal = 0;

                CultureInfo culture = new CultureInfo("id-ID");
                var dateArray = new string[] { };

                if (!string.IsNullOrEmpty(DueDate))
                {
                    dateArray = DueDate.Split(" to ");
                }

                List<NoticeOfPayment_ViewModel> list = new List<NoticeOfPayment_ViewModel>();
                list = StoredProcedureExecutor.ExecuteSPList<NoticeOfPayment_ViewModel>(_context, "sp_Load_NoticeOfPayment_View", new SqlParameter[]{
                        new SqlParameter("@NopNo", NopNoSearchParam),
                        new SqlParameter("@DueDateFrom", dateArray.Count() == 0 ? "" : DateTime.Parse(dateArray[0], culture)),
                        new SqlParameter("@DueDateTo", dateArray.Count() == 0 ? "" : DateTime.Parse(dateArray[1], culture)),
                        new SqlParameter("@sortColumn", sortColumn),
                        new SqlParameter("@sortColumnDir", sortColumnDir),
                        new SqlParameter("@PageNumber", pageNumber),
                        new SqlParameter("@RowsPage", pageSize)});

                recordsTotal = StoredProcedureExecutor.ExecuteScalarInt(_context, "sp_Load_NoticeOfPayment_Count", new SqlParameter[]{
                        new SqlParameter("@NopNo", NopNoSearchParam),
                        new SqlParameter("@DueDateFrom", dateArray.Count() == 0 ? "" : DateTime.Parse(dateArray[0], culture)),
                        new SqlParameter("@DueDateTo", dateArray.Count() == 0 ? "" : DateTime.Parse(dateArray[1], culture)),
                });

                if (list == null)
                {
                    list = new List<NoticeOfPayment_ViewModel>();
                    recordsTotal = 0;
                }

                return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = list });
            }
            catch (Exception Ex)
            {
                throw;
            }
        }

        //No Use
        [HttpPost]
        public IActionResult LoadDataDetailNopTemp(string randomstring)
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

                List<NoticeOfPayment_ViewModel> list = new List<NoticeOfPayment_ViewModel>();

                list = StoredProcedureExecutor.ExecuteSPList<NoticeOfPayment_ViewModel>(_context, "sp_Load_NoticeOfPaymentDetailTemp_View", new SqlParameter[]{
                        new SqlParameter("@RandomString", randomstring),
                        new SqlParameter("@sortColumn", sortColumn),
                        new SqlParameter("@sortColumnDir", sortColumnDir),
                        new SqlParameter("@PageNumber", pageNumber),
                        new SqlParameter("@RowsPage", pageSize)});

                recordsTotal = StoredProcedureExecutor.ExecuteScalarInt(_context, "sp_Load_NoticeOfPaymentDetailTemp_Count", new SqlParameter[]{
                        new SqlParameter("@RandomString", randomstring),
                });

                if (list == null)
                {
                    list = new List<NoticeOfPayment_ViewModel>();
                    recordsTotal = 0;
                }

                return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = list });
            }
            catch (Exception Ex)
            {
                throw;
            }
        }
        //No Use
        public IActionResult LoadDataDetailNop(string NopId)
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

                List<NoticeOfPayment_ViewModel> list = new List<NoticeOfPayment_ViewModel>();

                list = StoredProcedureExecutor.ExecuteSPList<NoticeOfPayment_ViewModel>(_context, "sp_Load_NoticeOfPaymentDetail_View", new SqlParameter[]{
                        new SqlParameter("@NopId", NopId),
                        new SqlParameter("@sortColumn", sortColumn),
                        new SqlParameter("@sortColumnDir", sortColumnDir),
                        new SqlParameter("@PageNumber", pageNumber),
                        new SqlParameter("@RowsPage", pageSize)});

                recordsTotal = StoredProcedureExecutor.ExecuteScalarInt(_context, "sp_Load_NoticeOfPaymentDetail_Count", new SqlParameter[]{
                        new SqlParameter("@NopId", NopId),
                });

                if (list == null)
                {
                    list = new List<NoticeOfPayment_ViewModel>();
                    recordsTotal = 0;
                }

                return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = list });
            }
            catch (Exception Ex)
            {
                throw;
            }
        //No Use
        }
        //public IActionResult LoadDataDetailNopNew()
        //{
        //    if (!lastSession.Update())
        //    {
        //        return RedirectToAction("Login", GetConfig.AppSetting["AppSettings:Subdomain:DomainController"] + "Login", new { a = true });
        //    }
        //    var location = new Uri($"{Request.Scheme}://{Request.Host}{Request.Path}");
        //    string Path = location.AbsolutePath.Replace(this.ControllerContext.RouteData.Values["action"].ToString(), "Index");
        //    if (!accessSecurity.IsGetAccess(".." + Path))
        //    {
        //        return RedirectToAction("NotAccess", "Error");
        //    }

        //    try
        //    {
        //        var dict = Request.Form.ToDictionary(x => x.Key, x => x.Value.ToString());

        //        var draw = dict["draw"];

        //        //Untuk mengetahui info paging dari datatable
        //        var start = dict["start"];
        //        var length = dict["length"];

        //        //Server side datatable hanya support untuk mendapatkan data mulai ke berapa, untuk mengirim row ke berapa
        //        //Kita perlu membuat logika sendiri
        //        var pageNumber = (int.Parse(start) / int.Parse(length)) + 1;

        //        //Untuk mengetahui info order column datatable
        //        var sortColumn = dict["columns[" + dict["order[0][column]"] + "][data]"];
        //        var sortColumnDir = dict["order[0][dir]"];
        //        var DueDate = dict["columns[2][search][value]"];


        //        //Untuk mengetahui info jumlah page dan total skip data
        //        int pageSize = length != null ? Convert.ToInt32(length) : 0;
        //        int skip = start != null ? Convert.ToInt32(start) : 0;
        //        int recordsTotal = 0;

        //        List<CreditorRefDetail_ViewModels> list = new List<CreditorRefDetail_ViewModels>();

        //        if (DueDate == "")
        //        {
        //            list = null;
        //            recordsTotal = 0;

        //            return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = list });
        //        }

        //        DateTime dateTime = DateTime.ParseExact(DueDate, "yyyy-MM-dd", null);
        //        DateTimeOffset dateTimeOffset = new DateTimeOffset(dateTime, TimeSpan.Zero);
        //        string FinalDueDate = dateTimeOffset.ToString("yyyy-MM-ddTHH:mm:ssZ");


        //        var url = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:CreditorRefDetail"] + "top=" + pageSize + "&skip=" + skip + "&count=true&orderby=CreditorRef asc&filter= DueDate eq " + FinalDueDate;
        //        (bool resultApi, string result) = RequestToAPI.GetJsonStringWebApi(url, null);
        //        if (resultApi && !string.IsNullOrEmpty(result))
        //        {
        //            var jsonParseReturn = JsonConvert.DeserializeObject<ResultStatusDataInt<CreditorRefDetail_ViewModels>>(result);
        //            if (jsonParseReturn.Value.Count() != 0)
        //            {
        //                list = jsonParseReturn.Value;

        //                recordsTotal = jsonParseReturn.ODataCount;
        //            }
        //            else
        //            {
        //                list = null;

        //                recordsTotal = 0;
        //            }
        //        }

        //        if (list == null)
        //        {
        //            list = new List<CreditorRefDetail_ViewModels>();
        //            recordsTotal = 0;
        //        }

        //        return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = list });
        //    }
        //    catch (Exception Ex)
        //    {
        //        throw;
        //    }
        //}
        public IActionResult LoadDataDetailNopNew(string DueDate)
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
                int recordsTotal = 0;
                List<NoticeOfPayment_ViewModel> list = new List<NoticeOfPayment_ViewModel>();

                if (DueDate == "")
                {
                    list = null;
                    recordsTotal = 0;

                    return Json(new { data = list });
                }

                DateTime dateTime = DateTime.ParseExact(DueDate, "yyyy-MM-dd", null);
                DateTimeOffset dateTimeOffset = new DateTimeOffset(dateTime, TimeSpan.Zero);
                string FinalDueDate = dateTimeOffset.ToString("yyyy-MM-ddTHH:mm:ssZ");

                int skip = 0;
                int batchSize = 50;
                bool keepFetching = true;
                long id = 1;

                while (keepFetching)
                {
                    var url = GetConfig.AppSetting["ApiDeva:BaseApi"]
                              + GetConfig.AppSetting["ApiDeva:CreditorRefDetail"]
                              + "top=" + batchSize
                              + "&skip=" + skip
                              + "&count=true&orderby=CreditorRef asc&filter=DueDate eq "
                              + FinalDueDate;

                    (bool resultApi, string result) = RequestToAPI.GetJsonStringWebApi(url, null);

                    if (resultApi && !string.IsNullOrEmpty(result))
                    {
                        var jsonParseReturn = JsonConvert.DeserializeObject<ResultStatusDataInt<NoticeOfPayment_ViewModel>>(result);

                        if (jsonParseReturn.Value.Count() != 0)
                        {
                            foreach (var item in jsonParseReturn.Value)
                            {
                                NoticeOfPayment_ViewModel nopDetail = new NoticeOfPayment_ViewModel
                                {
                                    Id = id,
                                    CreditorRef = item.CreditorRef,
                                    Outstanding = item.Outstanding,
                                    Principal = item.Principal,
                                    Interest = item.Interest,
                                    Fee = item.Fee,
                                };
                                list.Add(nopDetail);
                                id++;
                            }
                        }

                        if (jsonParseReturn.Value.Count() < batchSize)
                        {
                            keepFetching = false;
                        }
                        else
                        {
                            skip += batchSize;
                        }
                    }
                    else
                    {
                        keepFetching = false;
                    }
                }
                
                return Json(new { data = list });
            }
            catch (Exception Ex)
            {
                throw;
            }
        }
        public IActionResult LoadOnlyDataDetailNopNew(int id)
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
                List<NoticeOfPayment_ViewModel> listData = new List<NoticeOfPayment_ViewModel>();
                List<TblNoticeOfPaymentDetail> list = _context.TblNoticeOfPaymentDetails.Where(m=>m.NopId == id && m.IsDeleted == 0).ToList();

                foreach (var item in list) {
                    NoticeOfPayment_ViewModel temp = new NoticeOfPayment_ViewModel();
                    temp.IdDetailFromGet = item.Id;
                    temp.IdNopDetailFromApi = item.IdNopDetailFromApi;
                    temp.CreditorRef = item.CreditorRef;
                    temp.Outstanding = item.Outstanding;
                    temp.Principal = item.Principal;
                    temp.Interest = item.Interest;
                    temp.Fee = item.Fee;
                    listData.Add(temp);
                }

                return Json(new { data = listData });
            }
            catch (Exception Ex)
            {
                throw;
            }
        }
        [HttpPost]
        public IActionResult LoadDataFileNopTemp(string randomstring)
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

                List<NoticeOfPaymentFile_ViewModel> list = new List<NoticeOfPaymentFile_ViewModel>();

                list = StoredProcedureExecutor.ExecuteSPList<NoticeOfPaymentFile_ViewModel>(_context, "sp_Load_NoticeOfPaymentFileTemp_View", new SqlParameter[]{
                        new SqlParameter("@RandomString", randomstring),
                        new SqlParameter("@sortColumn", sortColumn),
                        new SqlParameter("@sortColumnDir", sortColumnDir),
                        new SqlParameter("@PageNumber", pageNumber),
                        new SqlParameter("@RowsPage", pageSize)});

                recordsTotal = StoredProcedureExecutor.ExecuteScalarInt(_context, "sp_Load_NoticeOfPaymentFileTemp_Count", new SqlParameter[]{
                        new SqlParameter("@RandomString", randomstring),
                });

                if (list == null)
                {
                    list = new List<NoticeOfPaymentFile_ViewModel>();
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
        public IActionResult LoadDataFileNop(string NopId)
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

                List<NoticeOfPaymentFile_ViewModel> list = new List<NoticeOfPaymentFile_ViewModel>();

                list = StoredProcedureExecutor.ExecuteSPList<NoticeOfPaymentFile_ViewModel>(_context, "sp_Load_NoticeOfPaymentFile_View", new SqlParameter[]{
                        new SqlParameter("@NopId", NopId),
                        new SqlParameter("@sortColumn", sortColumn),
                        new SqlParameter("@sortColumnDir", sortColumnDir),
                        new SqlParameter("@PageNumber", pageNumber),
                        new SqlParameter("@RowsPage", pageSize)});

                recordsTotal = StoredProcedureExecutor.ExecuteScalarInt(_context, "sp_Load_NoticeOfPaymentFile_Count", new SqlParameter[]{
                        new SqlParameter("@NopId", NopId),
                });

                if (list == null)
                {
                    list = new List<NoticeOfPaymentFile_ViewModel>();
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

        #region LoadDataDetail
        public JsonResult GetDataDetailCreditorRef(string DueDate)
        {
            DateTime dateTime = DateTime.ParseExact(DueDate, "yyyy-MM-dd", null);
            DateTimeOffset dateTimeOffset = new DateTimeOffset(dateTime, TimeSpan.Zero);
            string FinalDueDate = dateTimeOffset.ToString("yyyy-MM-ddTHH:mm:ssZ");
            //var FinalDueDate = DateTime.Parse(DueDate);

            CreditorRefDetail_ViewModels source = new CreditorRefDetail_ViewModels();

            try
            {
                var url = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:CreditorRefDetail"] + "top=" + 50 + "&skip=" + 0 + "&orderby=CreditorRef asc&filter= DueDate eq " + FinalDueDate;
                (bool resultApi, string result) = RequestToAPI.GetJsonStringWebApi(url, null);
                if (resultApi && !string.IsNullOrEmpty(result))
                {
                    var jsonParseReturn = JsonConvert.DeserializeObject<ResultStatusDataInt<CreditorRefDetail_ViewModels>>(result);
                    if (jsonParseReturn.Value.Count() != 0)
                    {
                        source.Outstanding = jsonParseReturn.Value[0].Outstanding;
                        source.Principal = jsonParseReturn.Value[0].Principal;
                        source.Interest = jsonParseReturn.Value[0].Interest;
                        source.Fee = jsonParseReturn.Value[0].Fee;
                    }
                    else
                    {
                        source.Outstanding = 0;
                        source.Principal = 0;
                        source.Interest = 0;
                        source.Fee = 0;
                    }
                }

                return Json(source);

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
            ViewBag.RekId = new SelectList("", "");
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
        public ActionResult CreateDetailNop()
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
            return PartialView("_CreateDetailNop");
        }
        public ActionResult CreateFileNop()
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

            return PartialView("_CreateFileNop");
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<ActionResult> SubmitCreate(TblNoticeOfPaymentVM data, string listDetailNop, string randomString)
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
                TblNoticeOfPayment dataNopNo = _context.TblNoticeOfPayments.Where(m => m.NopNo == data.NopNo && m.IsDeleted != 1).FirstOrDefault();
                if (dataNopNo != null)
                {
                    return Content("nopNo sudah terdaftar atas " + dataNopNo.Cur);
                }

                using (TransactionScope trx = new TransactionScope())
                {
                    TblNoticeOfPayment model = new TblNoticeOfPayment();
                    model.NopNo = data.NopNo;
                    model.DueDate = data.DueDate;
                    model.InterestDays = data.InterestDays == null || data.InterestDays == "" ? null :  decimal.Parse(data.InterestDays.Replace(".", ""));
                    model.InterestRate = data.InterestRate == null || data.InterestRate == "" ? null :  decimal.Parse(data.InterestRate.Replace(".", ""));
                    model.RekId = data.RekId;
                    model.RekNameAcc = data.RekNameAcc;
                    model.Cur = _context.TblMasterLookups.Where(m => m.Value == int.Parse(data.Cur) && m.Type == "Currency").Select(m => m.Name).FirstOrDefault();
                    model.AccountNo = data.AccountNo;
                    model.AccountName = data.AccountName;
                    model.Status = "Created";
                    model.CreatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                    model.CreatedTime = DateTime.Now;
                    model.IsDeleted = 0;
                    model.IsActive = 1;
                    _context.TblNoticeOfPayments.Add(model);
                    _context.SaveChanges();


                    List<TblNoticeOfPaymentDetail> nopDetailList = JsonConvert.DeserializeObject<List<TblNoticeOfPaymentDetail>>(listDetailNop);
                    if (nopDetailList != null)
                    {
                        foreach (var item in nopDetailList)
                        {
                            TblNoticeOfPaymentDetail nopDetail = new TblNoticeOfPaymentDetail();
                            nopDetail.NopId = model.Id;
                            nopDetail.CreditorRef = item.CreditorRef;
                            nopDetail.Outstanding = item.Outstanding;
                            nopDetail.Principal = item.Principal;
                            nopDetail.Interest = item.Interest;
                            nopDetail.Fee = item.Fee;
                            nopDetail.CreatedTime = DateTime.Now;
                            nopDetail.CreatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                            nopDetail.IsActive = 1;
                            nopDetail.IsDeleted = 0;
                            _context.TblNoticeOfPaymentDetails.Add(nopDetail);
                        }
                        _context.SaveChanges();
                    }

                    List<TblFileUploadNopTemp> nopFileList = _context.TblFileUploadNopTemps.Where(m => m.RandomString == randomString && m.IsActive == 1 && m.IsDeleted == 0).ToList();
                    if (nopFileList != null)
                    {
                        foreach (var item in nopFileList)
                        {
                            TblFileUploadNop nopFile = new TblFileUploadNop();
                            nopFile.IdNop = (int?)model.Id;
                            nopFile.FileName = item.FileName;
                            nopFile.FileSize = item.FileSize;
                            nopFile.FilePath = item.FilePath;
                            nopFile.FileExt = item.FileExt;
                            nopFile.UploadTime = DateTime.Now;
                            nopFile.UploadById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                            nopFile.IsActive = 1;
                            nopFile.IsDeleted = 0;
                            _context.TblFileUploadNops.Add(nopFile);
                        }
                        _context.SaveChanges();
                    }


                    //List<CreditorRefDetail_ViewModels> list = new List<CreditorRefDetail_ViewModels>();
                    //DateTime dateTime = DateTime.ParseExact(data.DueDate?.ToString("yyyy-MM-dd"), "yyyy-MM-dd", null);
                    //DateTimeOffset dateTimeOffset = new DateTimeOffset(dateTime, TimeSpan.Zero);
                    //string FinalDueDate = dateTimeOffset.ToString("yyyy-MM-ddTHH:mm:ssZ");

                    //int skip = 0;
                    //int batchSize = 50;
                    //bool keepFetching = true;

                    //while (keepFetching)
                    //{
                    //    var url = GetConfig.AppSetting["ApiDeva:BaseApi"]
                    //              + GetConfig.AppSetting["ApiDeva:CreditorRefDetail"]
                    //              + "top=" + batchSize
                    //              + "&skip=" + skip
                    //              + "&count=true&orderby=CreditorRef asc&filter=DueDate eq "
                    //              + FinalDueDate;

                    //    (bool resultApi, string result) = RequestToAPI.GetJsonStringWebApi(url, null);

                    //    if (resultApi && !string.IsNullOrEmpty(result))
                    //    {
                    //        var jsonParseReturn = JsonConvert.DeserializeObject<ResultStatusDataInt<CreditorRefDetail_ViewModels>>(result);

                    //        if (jsonParseReturn.Value.Count() != 0)
                    //        {
                    //            foreach (var item in jsonParseReturn.Value)
                    //            {
                    //                TblNoticeOfPaymentDetail nopDetail = new TblNoticeOfPaymentDetail
                    //                {
                    //                    NopId = model.Id,
                    //                    CreditorRef = item.CreditorRef,
                    //                    Outstanding = item.Outstanding,
                    //                    Principal = item.Principal,
                    //                    Interest = item.Interest,
                    //                    Fee = item.Fee,
                    //                    CreatedTime = DateTime.Now,
                    //                    CreatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId)),
                    //                    IsActive = 1,
                    //                    IsDeleted = 0
                    //                };
                    //                _context.TblNoticeOfPaymentDetails.Add(nopDetail);
                    //            }
                    //            _context.SaveChanges();
                    //        }

                    //        if (jsonParseReturn.Value.Count() < batchSize)
                    //        {
                    //            keepFetching = false;
                    //        }
                    //        else
                    //        {
                    //            skip += batchSize;
                    //        }
                    //    }
                    //    else
                    //    {
                    //        keepFetching = false;
                    //    }
                    //}

                    

                    //List<TblNoticeOfPaymentDetailTemp> nopDetailList = _context.TblNoticeOfPaymentDetailTemps.Where(m => m.RandomString == randomString && m.IsActive == 1 && m.IsDeleted == 0).ToList();

                    //if (nopDetailList != null)
                    //{
                    //    foreach (var item in nopDetailList)
                    //    {
                    //        TblNoticeOfPaymentDetail nopDetail = new TblNoticeOfPaymentDetail();
                    //        nopDetail.NopId = model.Id;
                    //        nopDetail.CreditorRef = item.CreditorRef;
                    //        nopDetail.Outstanding = item.Outstanding;
                    //        nopDetail.Principal = item.Principal;
                    //        nopDetail.Interest = item.Interest;
                    //        nopDetail.Fee = item.Fee;
                    //        nopDetail.CreatedTime = DateTime.Now;
                    //        nopDetail.CreatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                    //        nopDetail.IsActive = 1;
                    //        nopDetail.IsDeleted = 0;
                    //        _context.TblNoticeOfPaymentDetails.Add(nopDetail);
                    //    }
                    //    _context.SaveChanges();
                    //}

                    

                    trx.Complete();

                  
                }

                return Content("");
            }
            catch (Exception Ex)
            {
                return Content(GetConfig.AppSetting["AppSettings:SistemError"]);
            }
        }
        public ActionResult SubmitCreateDetailNopTemp(TblNoticeOfPaymentDetailVM data, string uniq)
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
                //TblNoticeOfPaymentDetail dataNopNo = _context.TblNoticeOfPaymentDetails.Where(m => m.CreditorRef == data.CreditorRef && m.IsDeleted != 1).FirstOrDefault();
                //if (dataNopNo != null)
                //{
                //    return Content("CreditorRef sudah terdaftar");
                //}

                using (TransactionScope trx = new TransactionScope())
                {
                    TblNoticeOfPaymentDetailTemp model = new TblNoticeOfPaymentDetailTemp();
                    model.CreditorRef = data.CreditorRef;
                    model.Outstanding = decimal.Parse(data.Outstanding.Replace(".", ""));
                    model.Principal = decimal.Parse(data.Principal.Replace(".", ""));
                    model.Interest = decimal.Parse(data.Interest.Replace(".", ""));
                    model.Fee = decimal.Parse(data.Fee.Replace(".", ""));
                    model.IsActive = 1;
                    model.IsDeleted = 0;
                    model.RandomString = uniq;

                    _context.TblNoticeOfPaymentDetailTemps.Add(model);
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
        public ActionResult SubmitCreateDetailNop(TblNoticeOfPaymentDetailVM data, string uniq)
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
                //TblNoticeOfPaymentDetail dataNopNo = _context.TblNoticeOfPaymentDetails.Where(m => m.CreditorRef == data.CreditorRef && m.IsDeleted != 1 && m.NopId == data.NopId).FirstOrDefault();
                //if (dataNopNo != null)
                //{
                //    return Content("CreditorRef sudah terdaftar");
                //}

                using (TransactionScope trx = new TransactionScope())
                {
                    TblNoticeOfPaymentDetail model = new TblNoticeOfPaymentDetail();
                    model.NopId = int.Parse(uniq);
                    model.CreditorRef = data.CreditorRef;
                    model.Outstanding = decimal.Parse(data.Outstanding.Replace(".", ""));
                    model.Principal = decimal.Parse(data.Principal.Replace(".", ""));
                    model.Interest = decimal.Parse(data.Interest.Replace(".", ""));
                    model.Fee = decimal.Parse(data.Fee.Replace(".", ""));
                    model.CreatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                    model.IsActive = 1;
                    model.IsDeleted = 0;
                    _context.TblNoticeOfPaymentDetails.Add(model);
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
        public async Task<ActionResult> SubmitExcelCreate(NoticeOfPayment_ViewModel model)
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
                    else
                    {
                        TblMasterSystemParameter ConfigAppsLocalPath = _context.TblMasterSystemParameters.Where(m => m.Key == "PathFileExcel").FirstOrDefault();

                        List<TblNoticeOfPaymentDetailTemp> list = _context.TblNoticeOfPaymentDetailTemps.Where(m => m.RandomString == model.RandomString).ToList();
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

                                        var NopNotempNop = row.GetCell(0).ToString() == "" || row.GetCell(0).ToString() == null ? "" : row.GetCell(0).ToString();
                                        var NopNotempNopTemp = row.GetCell(5).ToString() == "" || row.GetCell(8).ToString() == null ? "" : row.GetCell(8).ToString();

                                        TblNoticeOfPayment ListTempNop = _context.TblNoticeOfPayments.Where(m => m.NopNo == NopNotempNop && m.IsDeleted == 0).FirstOrDefault();
                                        TblNoticeOfPaymentDetail ListTempNopDetail = _context.TblNoticeOfPaymentDetails.Where(m => m.CreditorRef == NopNotempNopTemp && m.IsDeleted == 0).FirstOrDefault();

                                        if (ListTempNop == null)
                                        {
                                            TblNoticeOfPayment ListTempNew = new TblNoticeOfPayment();
                                            ListTempNew.NopNo = row.GetCell(0).ToString() == "" || row.GetCell(0).ToString() == null ? "" : row.GetCell(0).ToString();
                                            ListTempNew.DueDate = DateTime.Parse(row.GetCell(1).ToString() == "" || row.GetCell(1).ToString() == null ? "" : row.GetCell(1).ToString(), culture);
                                            ListTempNew.InterestRate = decimal.Parse(row.GetCell(2).ToString() == "" || row.GetCell(2).ToString() == null ? "" : row.GetCell(2).ToString());
                                            ListTempNew.InterestDays = decimal.Parse(row.GetCell(3).ToString() == "" || row.GetCell(3).ToString() == null ? "" : row.GetCell(3).ToString());
                                            var Rek = row.GetCell(4).ToString() == "" || row.GetCell(4).ToString() == null ? "" : row.GetCell(4).ToString();
                                            var CheckRek = SelectDataRekId(Rek);
                                            ListTempNew.RekId = CheckRek.id;
                                            ListTempNew.RekNameAcc = CheckRek.text;
                                            var Currency = row.GetCell(5).ToString() == "" || row.GetCell(5).ToString() == null ? "" : row.GetCell(5).ToString();
                                            var CheckCurrency = _context.TblMasterLookups.Where(m => m.Name == Currency && m.Type == "Currency").Select(m => m.Name).FirstOrDefault();
                                            ListTempNew.Cur = CheckCurrency;
                                            ListTempNew.Status = "Created";
                                            ListTempNew.IsActive = 1;
                                            ListTempNew.IsDeleted = 0;
                                            ListTempNew.CreatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                                            ListTempNew.CreatedTime = DateTime.Now;
                                            _context.TblNoticeOfPayments.Add(ListTempNew);
                                            _context.SaveChanges();

                                            if (ListTempNopDetail == null)
                                            {
                                                TblNoticeOfPaymentDetail ListTempNewDetail = new TblNoticeOfPaymentDetail();
                                                ListTempNewDetail.NopId = ListTempNew.Id;
                                                ListTempNewDetail.CreditorRef = row.GetCell(6).ToString() == "" || row.GetCell(6).ToString() == null ? "" : row.GetCell(6).ToString();
                                                ListTempNewDetail.Outstanding = decimal.Parse(row.GetCell(7).ToString() == "" || row.GetCell(7).ToString() == null ? "" : row.GetCell(7).ToString());
                                                ListTempNewDetail.Principal = decimal.Parse(row.GetCell(8).ToString() == "" || row.GetCell(8).ToString() == null ? "" : row.GetCell(8).ToString());
                                                ListTempNewDetail.Interest = decimal.Parse(row.GetCell(9).ToString() == "" || row.GetCell(9).ToString() == null ? "" : row.GetCell(9).ToString());
                                                ListTempNewDetail.Fee = decimal.Parse(row.GetCell(10).ToString() == "" || row.GetCell(10).ToString() == null ? "" : row.GetCell(10).ToString());
                                                ListTempNewDetail.IsActive = 1;
                                                ListTempNewDetail.IsDeleted = 0;
                                                ListTempNewDetail.CreatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                                                ListTempNewDetail.CreatedTime = DateTime.Now;
                                                bool CheckCreRef = CheckCreditorRef(row.GetCell(6).ToString() == "" || row.GetCell(6).ToString() == null ? "" : row.GetCell(6).ToString());
                                                if (CheckCreRef)
                                                {
                                                    _context.TblNoticeOfPaymentDetails.Add(ListTempNewDetail);
                                                }
                                                
                                            }
                                            else
                                            {
                                                ListTempNopDetail.NopId = ListTempNew.Id;
                                                ListTempNopDetail.CreditorRef = row.GetCell(6).ToString() == "" || row.GetCell(6).ToString() == null ? "" : row.GetCell(6).ToString();
                                                ListTempNopDetail.Outstanding = decimal.Parse(row.GetCell(7).ToString() == "" || row.GetCell(7).ToString() == null ? "" : row.GetCell(7).ToString());
                                                ListTempNopDetail.Principal = decimal.Parse(row.GetCell(8).ToString() == "" || row.GetCell(8).ToString() == null ? "" : row.GetCell(8).ToString());
                                                ListTempNopDetail.Interest = decimal.Parse(row.GetCell(9).ToString() == "" || row.GetCell(9).ToString() == null ? "" : row.GetCell(9).ToString());
                                                ListTempNopDetail.Fee = decimal.Parse(row.GetCell(10).ToString() == "" || row.GetCell(10).ToString() == null ? "" : row.GetCell(10).ToString());
                                                ListTempNopDetail.IsActive = 1;
                                                ListTempNopDetail.IsDeleted = 0;
                                                ListTempNopDetail.CreatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                                                ListTempNopDetail.CreatedTime = DateTime.Now;
                                                bool CheckCreRef = CheckCreditorRef(row.GetCell(6).ToString() == "" || row.GetCell(6).ToString() == null ? "" : row.GetCell(6).ToString());
                                                if (CheckCreRef)
                                                {
                                                    _context.TblNoticeOfPaymentDetails.Add(ListTempNopDetail);
                                                }
                                            }
                                            _context.SaveChanges();

                                        }
                                        else if (ListTempNop != null && ListTempNop.Status != "Verified")
                                        {
                                            ListTempNop.NopNo = row.GetCell(0).ToString() == "" || row.GetCell(0).ToString() == null ? "" : row.GetCell(0).ToString();
                                            ListTempNop.DueDate = DateTime.Parse(row.GetCell(1).ToString() == "" || row.GetCell(1).ToString() == null ? "" : row.GetCell(1).ToString(), culture);
                                            ListTempNop.InterestRate = decimal.Parse(row.GetCell(2).ToString() == "" || row.GetCell(2).ToString() == null ? "" : row.GetCell(2).ToString());
                                            ListTempNop.InterestDays = decimal.Parse(row.GetCell(3).ToString() == "" || row.GetCell(3).ToString() == null ? "" : row.GetCell(3).ToString());
                                            var Rek = row.GetCell(4).ToString() == "" || row.GetCell(4).ToString() == null ? "" : row.GetCell(4).ToString();
                                            var CheckRek = SelectDataRekId(Rek);
                                            ListTempNop.RekId = CheckRek.id;
                                            ListTempNop.RekNameAcc = CheckRek.text;
                                            var Currency = row.GetCell(5).ToString() == "" || row.GetCell(5).ToString() == null ? "" : row.GetCell(5).ToString();
                                            var CheckCurrency = _context.TblMasterLookups.Where(m => m.Name == Currency && m.Type == "Currency").Select(m => m.Name).FirstOrDefault();
                                            ListTempNop.Cur = CheckCurrency;
                                            ListTempNop.IsActive = 1;
                                            ListTempNop.IsDeleted = 0;
                                            ListTempNop.CreatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                                            ListTempNop.CreatedTime = DateTime.Now;
                                            _context.Entry(ListTempNop).State = EntityState.Modified;

                                            if (ListTempNopDetail == null)
                                            {
                                                TblNoticeOfPaymentDetail ListTempNewDetail = new TblNoticeOfPaymentDetail();
                                                ListTempNewDetail.NopId = ListTempNop.Id;
                                                ListTempNewDetail.CreditorRef = row.GetCell(6).ToString() == "" || row.GetCell(6).ToString() == null ? "" : row.GetCell(6).ToString();
                                                ListTempNewDetail.Outstanding = decimal.Parse(row.GetCell(7).ToString() == "" || row.GetCell(7).ToString() == null ? "" : row.GetCell(7).ToString());
                                                ListTempNewDetail.Principal = decimal.Parse(row.GetCell(8).ToString() == "" || row.GetCell(8).ToString() == null ? "" : row.GetCell(8).ToString());
                                                ListTempNewDetail.Interest = decimal.Parse(row.GetCell(9).ToString() == "" || row.GetCell(9).ToString() == null ? "" : row.GetCell(9).ToString());
                                                ListTempNewDetail.Fee = decimal.Parse(row.GetCell(10).ToString() == "" || row.GetCell(10).ToString() == null ? "" : row.GetCell(10).ToString());
                                                ListTempNewDetail.IsActive = 1;
                                                ListTempNewDetail.IsDeleted = 0;
                                                ListTempNewDetail.CreatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                                                ListTempNewDetail.CreatedTime = DateTime.Now;
                                                bool CheckCreRef = CheckCreditorRef(row.GetCell(8).ToString() == "" || row.GetCell(8).ToString() == null ? "" : row.GetCell(8).ToString());
                                                if (CheckCreRef)
                                                {
                                                    _context.TblNoticeOfPaymentDetails.Add(ListTempNewDetail);
                                                }
                                            }
                                            else
                                            {
                                                ListTempNopDetail.NopId = ListTempNop.Id;
                                                ListTempNopDetail.CreditorRef = row.GetCell(6).ToString() == "" || row.GetCell(6).ToString() == null ? "" : row.GetCell(6).ToString();
                                                ListTempNopDetail.Outstanding = decimal.Parse(row.GetCell(7).ToString() == "" || row.GetCell(7).ToString() == null ? "" : row.GetCell(7).ToString());
                                                ListTempNopDetail.Principal = decimal.Parse(row.GetCell(8).ToString() == "" || row.GetCell(8).ToString() == null ? "" : row.GetCell(8).ToString());
                                                ListTempNopDetail.Interest = decimal.Parse(row.GetCell(9).ToString() == "" || row.GetCell(9).ToString() == null ? "" : row.GetCell(9).ToString());
                                                ListTempNopDetail.Fee = decimal.Parse(row.GetCell(10).ToString() == "" || row.GetCell(10).ToString() == null ? "" : row.GetCell(10).ToString());
                                                ListTempNopDetail.IsActive = 1;
                                                ListTempNopDetail.IsDeleted = 0;
                                                ListTempNopDetail.CreatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                                                ListTempNopDetail.CreatedTime = DateTime.Now;
                                                bool CheckCreRef = CheckCreditorRef(row.GetCell(8).ToString() == "" || row.GetCell(8).ToString() == null ? "" : row.GetCell(8).ToString());
                                                if (CheckCreRef)
                                                {
                                                    _context.TblNoticeOfPaymentDetails.Add(ListTempNopDetail);
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
        [HttpPost]
        public ActionResult SubmitCreateFileNopTemp(NoticeOfPaymentFileUpload_ViewModel data)
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
                        TblFileUploadNopTemp model = new TblFileUploadNopTemp();
                        model.FileName = FullName;
                        model.FileSize = Convert.ToInt32(data.File.Length);
                        model.FilePath = FullPath;
                        model.FileExt = Ext;
                        model.UploadTime = DateTime.Now;
                        model.UploadById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                        model.IsActive = 1;
                        model.IsDeleted = 0;
                        model.RandomString = data.uniq;
                        _context.TblFileUploadNopTemps.Add(model);
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
        public ActionResult SubmitCreateFileNop(NoticeOfPayment_ViewModel data, string uniq)
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
                TblNoticeOfPayment dataAssign = _context.TblNoticeOfPayments.Where(m => m.Id == int.Parse(uniq) && m.IsDeleted == 0).FirstOrDefault();

                if (dataAssign.IdNopFromApi != null)
                {
                    var jsonParseReturnCheck = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNop>>("");
                    var urlCheck = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:Nop:GetDataById"] + dataAssign.IdNopFromApi;
                    (bool resultApiCheck, string resultCheck) = RequestToAPI.GetJsonStringWebApi(urlCheck, null);
                    if (resultApiCheck && !string.IsNullOrEmpty(resultCheck))
                    {
                        jsonParseReturnCheck = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNop>>(resultCheck);
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
                                        TblFileUploadNop model = new TblFileUploadNop();
                                        model.IdNop = int.Parse(uniq);
                                        model.FileName = FullName;
                                        model.FileSize = Convert.ToInt32(data.File.Length);
                                        model.FilePath = FullPath;
                                        model.FileExt = Ext;
                                        model.UploadTime = DateTime.Now;
                                        model.UploadById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                                        model.IsActive = 1;
                                        model.IsDeleted = 0;
                                        _context.TblFileUploadNops.Add(model);
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
                                var jsonParseReturnUploadBase64 = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNop>>("");
                                var urlUploadBase64 = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:Nop:UploadBase64"] + dataAssign.IdNopFromApi;
                                (bool resultApiUploadBase64, string resultUploadBase64) = RequestToAPI.PostRequestToWebApi(urlUploadBase64, new
                                {
                                    FileName = FullName,
                                    FileContent = base64String,
                                }, null);
                                if (resultApiUploadBase64 && !string.IsNullOrEmpty(resultUploadBase64))
                                {
                                    jsonParseReturnUploadBase64 = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNop>>(resultUploadBase64);
                                    if (jsonParseReturnUploadBase64.StatusCode == 200 || jsonParseReturnUploadBase64.StatusCode == 201)
                                    {
                                        using (TransactionScope trx = new TransactionScope())
                                        {
                                            try
                                            {
                                                TblFileUploadNop model = new TblFileUploadNop();
                                                model.IdNop = int.Parse(uniq);
                                                model.IdFileFromApi = jsonParseReturnUploadBase64.Data.Key;
                                                model.FileName = FullName;
                                                model.FileSize = Convert.ToInt32(data.File.Length);
                                                model.FilePath = FullPath;
                                                model.FileExt = Ext;
                                                model.UploadTime = DateTime.Now;
                                                model.UploadById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                                                model.IsActive = 1;
                                                model.IsDeleted = 0;
                                                _context.TblFileUploadNops.Add(model);
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
                                //        var fullUrl = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:Nop:UploadFile"] + jsonParseReturnAdd.Data.Id;

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
                            TblFileUploadNop model = new TblFileUploadNop();
                            model.IdNop = int.Parse(uniq);
                            model.FileName = FullName;
                            model.FileSize = Convert.ToInt32(data.File.Length);
                            model.FilePath = FullPath;
                            model.FileExt = Ext;
                            model.UploadTime = DateTime.Now;
                            model.UploadById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                            model.IsActive = 1;
                            model.IsDeleted = 0;
                            _context.TblFileUploadNops.Add(model);
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
                TblNoticeOfPayment nop = _context.TblNoticeOfPayments.Where(m => m.Id == id).FirstOrDefault();

                var data = new TblNoticeOfPaymentVM
                {
                    Id = nop.Id,
                    NopId = nop.Id,
                    NopNo = nop.NopNo,
                    RekId = nop.RekId,
                    DueDate = nop.DueDate,
                    Cur = _context.TblMasterLookups.Where(m => m.Name == nop.Cur && m.Type == "Currency").Select(m => m.Value).FirstOrDefault().ToString(),
                    InterestDays = nop.InterestDays.ToString().Replace(",00", ""),
                    InterestRate = nop.InterestRate.ToString().Replace(",00", ""),
                    AccountNo = nop.AccountNo,
                    AccountName = nop.AccountName, 
                    IsActive = nop.IsActive,
                };

                if (nop.RekId != null)
                {
                    ViewBag.RekId = new SelectList(Utility.SelectDataRekId((int)nop.RekId), "id", "text", nop.RekId);
                }
                else
                {
                    ViewBag.RekId = new SelectList("", "");
                }

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
                var data = new NoticeOfPayment_ViewModel();
                ViewBag.RekId = new SelectList("", "");
                return PartialView("_Edit", data);
            }
        }
        public ActionResult UpdateDetail(int id)
        {
            if (!lastSession.Update())
            {
                return RedirectToAction("Login", GetConfig.AppSetting["AppSettings:Subdomain:DomainController"] + "Login", new { a = true });
            }
            var location = new Uri($"{Request.Scheme}://{Request.Host}{Request.Path}");
            string Path = location.AbsolutePath.Replace(this.ControllerContext.RouteData.Values["action"].ToString(), "Index");
            int lastIndex = Path.LastIndexOf('/');
            string modifiedUrl = Path;
            if (lastIndex != -1)
            {
                // Dapatkan bagian sebelum tanda slash terakhir (path tanpa parameter)
                modifiedUrl = Path.Substring(0, lastIndex);
            }
            if (!accessSecurity.IsGetAccess(".." + modifiedUrl))
            {
                return RedirectToAction("NotAccess", "Error");
            }

            TblNoticeOfPaymentDetail nopDetailList = _context.TblNoticeOfPaymentDetails.FirstOrDefault(n => n.Id == id && n.IsDeleted != 1);

            return Json(nopDetailList);
        }
        public ActionResult EditDetailNopCreate(int id)
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

            NoticeOfPayment_ViewModel model = new NoticeOfPayment_ViewModel();
            model.Id = id;

            return PartialView("_EditDetailNop", model);
        }
        
        public async Task<ActionResult> SubmitDetail(TblNoticeOfPaymentDetailVM model)
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
                    TblNoticeOfPaymentDetail data = _context.TblNoticeOfPaymentDetails.Where(m => m.Id == model.Id).FirstOrDefault(); // Ambil data sesuai dengan ID
                    if (data.Id != 0)
                    {


                        data.CreditorRef = model.CreditorRef;
                        data.Outstanding = decimal.Parse(model.Outstanding.Replace(".", ""));
                        data.Principal = decimal.Parse(model.Principal.Replace(".", ""));
                        data.Interest = decimal.Parse(model.Interest.Replace(".", ""));
                        data.Fee = decimal.Parse(model.Fee.Replace(".", ""));
                        data.IsActive = model.IsActive = 1;
                        data.CreatedTime = DateTime.Now;
                        data.CreatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                        data.UpdatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                        data.UpdatedTime = DateTime.Now;
                        data.IsActive = model.IsActive = 0;
                        _context.Entry(data).State = EntityState.Modified;
                        _context.SaveChanges();

                        trx.Complete();
                    }
                }
                return Content("");

            }
            catch
            {
                return Content(GetConfig.AppSetting["AppSettings:SistemError"]);
            }
        }
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
                TblNoticeOfPayment nop = _context.TblNoticeOfPayments.Where(m => m.Id == id).FirstOrDefault();

                if (nop.RekId != null)
                {
                    ViewBag.RekId = new SelectList(Utility.SelectDataRekId((int)nop.RekId), "id", "text", nop.RekId);
                }
                else
                {
                    ViewBag.RekId = new SelectList("", "");
                }

                var data = new TblNoticeOfPaymentVM
                {
                    Id = nop.Id,
                    NopId = nop.Id,
                    NopNo = nop.NopNo,
                    RekId = nop.RekId,
                    DueDate = nop.DueDate,
                    Cur = _context.TblMasterLookups.Where(m => m.Name == nop.Cur && m.Type == "Currency").Select(m => m.Value).FirstOrDefault().ToString(),
                    InterestDays = nop.InterestDays.ToString().Replace(",00", ""),
                    InterestRate = nop.InterestRate.ToString().Replace(",00", ""),
                    AccountNo = nop.AccountNo,
                    AccountName = nop.AccountName,
                    IsActive = nop.IsActive,
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
                var data = new NoticeOfPayment_ViewModel();
                ViewBag.RekId = new SelectList("", "");
                return PartialView("_EditView", data);  
            }
        }
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<ActionResult> SubmitEdit(TblNoticeOfPaymentVM model, string listDetailNop)
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
                    TblNoticeOfPayment? data = _context.TblNoticeOfPayments.Where(m => m.Id == model.Id).FirstOrDefault(); // Ambil data sesuai dengan ID
                    if (data.NopNo != model.NopNo)
                    {
                        TblNoticeOfPayment dataNopNo = _context.TblNoticeOfPayments.Where(m => m.NopNo == model.NopNo && m.IsDeleted != 1).FirstOrDefault();
                        if (dataNopNo != null)
                        {
                            return Content("Kode sudah terdaftar atas " + dataNopNo.Cur);
                        }
                    }

                    if (data.IdNopFromApi != null)
                    {
                        var jsonParseReturnCheck = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNop>>("");
                        var urlCheck = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:Nop:GetDataById"] + data.IdNopFromApi;
                        (bool resultApiCheck, string resultCheck) = RequestToAPI.GetJsonStringWebApi(urlCheck, null);
                        if (resultApiCheck && !string.IsNullOrEmpty(resultCheck))
                        {
                            jsonParseReturnCheck = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNop>>(resultCheck);
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
                                    data.NopNo = model.NopNo;
                                    data.DueDate = model.DueDate;
                                    data.Cur = _context.TblMasterLookups.Where(m => m.Value == int.Parse(model.Cur) && m.Type == "Currency").Select(m => m.Name).FirstOrDefault();
                                    data.RekId = model.RekId;
                                    data.RekNameAcc = model.RekNameAcc;
                                    data.InterestDays = model.InterestDays == null || model.InterestDays == "" ? null : decimal.Parse(model.InterestDays.Replace(".", ""));
                                    data.InterestRate = model.InterestRate == null || model.InterestRate == "" ? null : decimal.Parse(model.InterestRate.Replace(".", ""));
                                    data.AccountName = model.AccountName;
                                    data.AccountNo = model.AccountNo;
                                    data.IsActive = model.IsActive = 1;
                                    data.UpdatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                                    data.UpdatedTime = DateTime.Now;
                                    _context.Entry(data).State = EntityState.Modified;
                                    _context.SaveChanges();

                                    List<NoticeOfPayment_ViewModel> nopDetailList = JsonConvert.DeserializeObject<List<NoticeOfPayment_ViewModel>>(listDetailNop);
                                    if (nopDetailList != null)
                                    {
                                        var Check = false;
                                        foreach (var item in nopDetailList)
                                        {
                                            if (item.IdDetailFromGet != null)
                                            {
                                                TblNoticeOfPaymentDetail nopDetail = _context.TblNoticeOfPaymentDetails.Where(x => x.Id == item.IdDetailFromGet).FirstOrDefault();
                                                nopDetail.NopId = model.Id;
                                                nopDetail.CreditorRef = item.CreditorRef;
                                                nopDetail.Outstanding = item.Outstanding;
                                                nopDetail.Principal = item.Principal;
                                                nopDetail.Interest = item.Interest;
                                                nopDetail.Fee = item.Fee;
                                                nopDetail.UpdatedTime = DateTime.Now;
                                                nopDetail.UpdatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                                                nopDetail.IsActive = 1;
                                                nopDetail.IsDeleted = 0;
                                                _context.Entry(nopDetail).State = EntityState.Modified;
                                                _context.SaveChanges();
                                            }
                                            else
                                            {
                                                if (!Check)
                                                {
                                                    StoredProcedureExecutor.ExecuteSPSingle<Navigation_ViewModels>(_context, "[sp_Delete_NopDetail]", new SqlParameter[]{
                                                        new SqlParameter("@IdNop", model.Id)
                                                    });

                                                    Check = true;
                                                }

                                                TblNoticeOfPaymentDetail nopDetail = new TblNoticeOfPaymentDetail();
                                                nopDetail.NopId = model.Id;
                                                nopDetail.CreditorRef = item.CreditorRef;
                                                nopDetail.Outstanding = item.Outstanding;
                                                nopDetail.Principal = item.Principal;
                                                nopDetail.Interest = item.Interest;
                                                nopDetail.Fee = item.Fee;
                                                nopDetail.CreatedTime = DateTime.Now;
                                                nopDetail.CreatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                                                nopDetail.IsActive = 1;
                                                nopDetail.IsDeleted = 0;
                                                _context.TblNoticeOfPaymentDetails.Add(nopDetail);
                                                _context.SaveChanges();

                                            }
                                        }
                                    }
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
                        data.NopNo = model.NopNo;
                        data.DueDate = model.DueDate;
                        data.Cur = _context.TblMasterLookups.Where(m => m.Value == int.Parse(model.Cur) && m.Type == "Currency").Select(m => m.Name).FirstOrDefault();
                        data.RekId = model.RekId;
                        data.RekNameAcc = model.RekNameAcc;
                        data.InterestDays = model.InterestDays == null || model.InterestDays == "" ? null : decimal.Parse(model.InterestDays.Replace(".", ""));
                        data.InterestRate = model.InterestRate == null || model.InterestRate == "" ? null : decimal.Parse(model.InterestRate.Replace(".", ""));
                        data.AccountName = model.AccountName;
                        data.AccountNo = model.AccountNo;
                        data.IsActive = model.IsActive = 1;
                        data.UpdatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                        data.UpdatedTime = DateTime.Now;
                        _context.Entry(data).State = EntityState.Modified;
                        _context.SaveChanges();

                        List<NoticeOfPayment_ViewModel> nopDetailList = JsonConvert.DeserializeObject<List<NoticeOfPayment_ViewModel>>(listDetailNop);
                        if (nopDetailList != null)
                        {
                            var Check = false;
                            foreach (var item in nopDetailList)
                            {
                                if (item.IdDetailFromGet != null)
                                {
                                    TblNoticeOfPaymentDetail nopDetail = _context.TblNoticeOfPaymentDetails.Where(x => x.Id == item.IdDetailFromGet).FirstOrDefault();
                                    nopDetail.NopId = model.Id;
                                    nopDetail.CreditorRef = item.CreditorRef;
                                    nopDetail.Outstanding = item.Outstanding;
                                    nopDetail.Principal = item.Principal;
                                    nopDetail.Interest = item.Interest;
                                    nopDetail.Fee = item.Fee;
                                    nopDetail.UpdatedTime = DateTime.Now;
                                    nopDetail.UpdatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                                    nopDetail.IsActive = 1;
                                    nopDetail.IsDeleted = 0;
                                    _context.Entry(nopDetail).State = EntityState.Modified;
                                    _context.SaveChanges();
                                }
                                else
                                {
                                    if (!Check)
                                    {
                                        StoredProcedureExecutor.ExecuteSPSingle<Navigation_ViewModels>(_context, "[sp_Delete_NopDetail]", new SqlParameter[]{
                                        new SqlParameter("@IdNop", model.Id)
                                    });

                                        Check = true;
                                    }

                                    TblNoticeOfPaymentDetail nopDetail = new TblNoticeOfPaymentDetail();
                                    nopDetail.NopId = model.Id;
                                    nopDetail.CreditorRef = item.CreditorRef;
                                    nopDetail.Outstanding = item.Outstanding;
                                    nopDetail.Principal = item.Principal;
                                    nopDetail.Interest = item.Interest;
                                    nopDetail.Fee = item.Fee;
                                    nopDetail.CreatedTime = DateTime.Now;
                                    nopDetail.CreatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                                    nopDetail.IsActive = 1;
                                    nopDetail.IsDeleted = 0;
                                    _context.TblNoticeOfPaymentDetails.Add(nopDetail);
                                    _context.SaveChanges();

                                }
                            }
                        }
                    }

                    


                    //List<CreditorRefDetail_ViewModels> list = new List<CreditorRefDetail_ViewModels>();
                    //DateTime dateTime = DateTime.ParseExact(data.DueDate?.ToString("yyyy-MM-dd"), "yyyy-MM-dd", null);
                    //DateTimeOffset dateTimeOffset = new DateTimeOffset(dateTime, TimeSpan.Zero);
                    //string FinalDueDate = dateTimeOffset.ToString("yyyy-MM-ddTHH:mm:ssZ");

                    //int skip = 0;
                    //int batchSize = 50;
                    //bool keepFetching = true;

                    //if (data.DueDate != model.DueDate)
                    //{
                    //    List<TblNoticeOfPaymentDetail> nopDetailDelete = _context.TblNoticeOfPaymentDetails.Where(m => m.NopId == model.Id).ToList();
                    //    if (nopDetailDelete != null || nopDetailDelete.Count() != 0)
                    //    {
                    //        foreach (var item in nopDetailDelete)
                    //        {
                    //            TblNoticeOfPaymentDetail nopDetailDeleteItem = _context.TblNoticeOfPaymentDetails.Where(m => m.Id == item.Id).FirstOrDefault();
                    //            nopDetailDeleteItem.IsActive = 1;
                    //            nopDetailDeleteItem.IsDeleted = 1;
                    //            _context.Entry(nopDetailDeleteItem).State = EntityState.Modified;
                    //        }
                    //    }

                    //    while (keepFetching)
                    //    {
                    //        var url = GetConfig.AppSetting["ApiDeva:BaseApi"]
                    //                  + GetConfig.AppSetting["ApiDeva:CreditorRefDetail"]
                    //                  + "top=" + batchSize
                    //                  + "&skip=" + skip
                    //                  + "&count=true&orderby=CreditorRef asc&filter=DueDate eq "
                    //                  + FinalDueDate;

                    //        (bool resultApi, string result) = RequestToAPI.GetJsonStringWebApi(url, null);

                    //        if (resultApi && !string.IsNullOrEmpty(result))
                    //        {
                    //            var jsonParseReturn = JsonConvert.DeserializeObject<ResultStatusDataInt<CreditorRefDetail_ViewModels>>(result);

                    //            if (jsonParseReturn.Value.Count() != 0)
                    //            {
                    //                foreach (var item in jsonParseReturn.Value)
                    //                {
                    //                    TblNoticeOfPaymentDetail nopDetail = new TblNoticeOfPaymentDetail
                    //                    {
                    //                        NopId = model.Id,
                    //                        CreditorRef = item.CreditorRef,
                    //                        Outstanding = item.Outstanding,
                    //                        Principal = item.Principal,
                    //                        Interest = item.Interest,
                    //                        Fee = item.Fee,
                    //                        CreatedTime = DateTime.Now,
                    //                        CreatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId)),
                    //                        IsActive = 1,
                    //                        IsDeleted = 0
                    //                    };
                    //                    _context.TblNoticeOfPaymentDetails.Add(nopDetail);
                    //                }
                    //                _context.SaveChanges();
                    //            }

                    //            if (jsonParseReturn.Value.Count() < batchSize)
                    //            {
                    //                keepFetching = false;
                    //            }
                    //            else
                    //            {
                    //                skip += batchSize;
                    //            }
                    //        }
                    //        else
                    //        {
                    //            keepFetching = false;
                    //        }
                    //    }
                    //}
                    //else {
                    //    while (keepFetching)
                    //    {
                    //        var url = GetConfig.AppSetting["ApiDeva:BaseApi"]
                    //                  + GetConfig.AppSetting["ApiDeva:CreditorRefDetail"]
                    //                  + "top=" + batchSize
                    //                  + "&skip=" + skip
                    //                  + "&count=true&orderby=CreditorRef asc&filter=DueDate eq "
                    //                  + FinalDueDate;

                    //        (bool resultApi, string result) = RequestToAPI.GetJsonStringWebApi(url, null);

                    //        if (resultApi && !string.IsNullOrEmpty(result))
                    //        {
                    //            var jsonParseReturn = JsonConvert.DeserializeObject<ResultStatusDataInt<CreditorRefDetail_ViewModels>>(result);

                    //            if (jsonParseReturn.Value.Count() != 0)
                    //            {
                    //                foreach (var item in jsonParseReturn.Value)
                    //                {
                    //                    TblNoticeOfPaymentDetail nopDetailUpdate = _context.TblNoticeOfPaymentDetails.Where(m => m.NopId == model.Id && m.CreditorRef == item.CreditorRef && m.IsActive == 1 && m.IsDeleted == 0).FirstOrDefault();
                    //                    if (nopDetailUpdate != null)
                    //                    {
                    //                        nopDetailUpdate.NopId = model.Id;
                    //                        nopDetailUpdate.CreditorRef = item.CreditorRef;
                    //                        nopDetailUpdate.Outstanding = item.Outstanding;
                    //                        nopDetailUpdate.Principal = item.Principal;
                    //                        nopDetailUpdate.Interest = item.Interest;
                    //                        nopDetailUpdate.Fee = item.Fee;
                    //                        nopDetailUpdate.UpdatedTime = DateTime.Now;
                    //                        nopDetailUpdate.UpdatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                    //                        nopDetailUpdate.IsActive = 1;
                    //                        nopDetailUpdate.IsDeleted = 0;
                    //                        _context.Entry(nopDetailUpdate).State = EntityState.Modified;
                    //                    }
                    //                    else
                    //                    {
                    //                        TblNoticeOfPaymentDetail nopDetailCreate = new TblNoticeOfPaymentDetail();
                    //                        nopDetailCreate.NopId = model.Id;
                    //                        nopDetailCreate.CreditorRef = item.CreditorRef;
                    //                        nopDetailCreate.Outstanding = item.Outstanding;
                    //                        nopDetailCreate.Principal = item.Principal;
                    //                        nopDetailCreate.Interest = item.Interest;
                    //                        nopDetailCreate.Fee = item.Fee;
                    //                        nopDetailCreate.CreatedTime = DateTime.Now;
                    //                        nopDetailCreate.CreatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                    //                        nopDetailCreate.IsActive = 1;
                    //                        nopDetailCreate.IsDeleted = 0;
                    //                        _context.TblNoticeOfPaymentDetails.Add(nopDetailUpdate);
                    //                    };
                    //                }
                    //                _context.SaveChanges();
                    //            }

                    //            if (jsonParseReturn.Value.Count() < batchSize)
                    //            {
                    //                keepFetching = false;
                    //            }
                    //            else
                    //            {
                    //                skip += batchSize;
                    //            }
                    //        }
                    //        else
                    //        {
                    //            keepFetching = false;
                    //        }
                    //    }
                    //}

                    

                    trx.Complete();

                }
                return Content("");

            }
            catch (Exception ex)
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

            try
            {
                TblNoticeOfPayment nop = _context.TblNoticeOfPayments.Where(m => m.Id == id).FirstOrDefault();

                if (nop.RekId != null)
                {
                    ViewBag.RekId = new SelectList(Utility.SelectDataRekId((int)nop.RekId), "id", "text", nop.RekId);
                }
                else
                {
                    ViewBag.RekId = new SelectList("", "");
                }

                var data = new TblNoticeOfPaymentVM
                {
                    Id = nop.Id,
                    NopNo = nop.NopNo,
                    Cur = _context.TblMasterLookups.Where(m => m.Name == nop.Cur && m.Type == "Currency").Select(m => m.Value).FirstOrDefault().ToString(),
                    DueDate = nop.DueDate,
                    InterestDays = nop.InterestDays.ToString().Replace(",00", ""),
                    InterestRate = nop.InterestRate.ToString().Replace(",00", ""),
                    AccountNo = nop.AccountNo,
                    RekId = nop.RekId,
                    AccountName = nop.AccountName,
                    IsActive = nop.IsActive,
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
                var data = new NoticeOfPayment_ViewModel();

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

                List<TblNoticeOfPayment> Transaksis = _context.TblNoticeOfPayments.Where(x => confirmedDeleteId.Contains(x.Id)).ToList(); //Ambil data sesuai dengan ID
                for (int i = 0; i < confirmedDeleteId.Length; i++)
                {
                    if (Transaksis[i].IdNopFromApi != null)
                    {
                        var jsonParseReturnCheck = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNop>>("");
                        var urlCheck = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:Nop:GetDataById"] + Transaksis[i].IdNopFromApi;
                        (bool resultApiCheck, string resultCheck) = RequestToAPI.GetJsonStringWebApi(urlCheck, null);
                        if (resultApiCheck && !string.IsNullOrEmpty(resultCheck))
                        {
                            jsonParseReturnCheck = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNop>>(resultCheck);
                        }

                        if (jsonParseReturnCheck.StatusCode == 200 || jsonParseReturnCheck.StatusCode == 201)
                        {
                            if (jsonParseReturnCheck.Data.Status == "Unverified")
                            {
                                var jsonParseReturnDelete = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNop>>("");
                                var urlDelete = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:Nop:Delete"] + Transaksis[i].IdNopFromApi;
                                (bool resultApiDelete, string resultDelete) = RequestToAPI.DeleteRequestToWebApi(urlDelete, null);
                                if (resultApiDelete && !string.IsNullOrEmpty(resultDelete))
                                {
                                    jsonParseReturnDelete = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNop>>(resultDelete);
                                }

                                if (jsonParseReturnDelete.StatusCode == 200 || jsonParseReturnDelete.StatusCode == 201)
                                {
                                    TblNoticeOfPayment data = _context.TblNoticeOfPayments.Find(Transaksis[i].Id);
                                    data.IsDeleted = 1;
                                    _context.Entry(data).State = EntityState.Modified;
                                    _context.SaveChanges();
                                }
                            }
                        }
                    }
                    else
                    {
                        TblNoticeOfPayment data = _context.TblNoticeOfPayments.Find(Transaksis[i].Id);
                        data.IsDeleted = 1;
                        _context.Entry(data).State = EntityState.Modified;
                        _context.SaveChanges();
                    }

                    List<TblNoticeOfPaymentDetail> NopDetailList = _context.TblNoticeOfPaymentDetails.Where(n => n.NopId == Transaksis[i].Id && n.IsDeleted != 1).ToList();

                    if (NopDetailList.Count != 0)
                    {
                        foreach (var detail in NopDetailList)
                        {
                            detail.IsDeleted = 1;
                            _context.Entry(detail).State = EntityState.Modified;
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
        public ActionResult DeleteDetailNopTemp(string Ids)
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

                List<TblNoticeOfPaymentDetailTemp> Transaksis = _context.TblNoticeOfPaymentDetailTemps.Where(x => confirmedDeleteId.Contains(x.Id)).ToList(); //Ambil data sesuai dengan ID
                for (int i = 0; i < confirmedDeleteId.Length; i++)
                {
                    TblNoticeOfPaymentDetailTemp data = _context.TblNoticeOfPaymentDetailTemps.Find(Transaksis[i].Id);
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
        public ActionResult DeleteDetail(string Ids)
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

                List<TblNoticeOfPaymentDetail> Transaksis = _context.TblNoticeOfPaymentDetails.Where(x => confirmedDeleteId.Contains(x.Id)).ToList(); //Ambil data sesuai dengan ID
                for (int i = 0; i < confirmedDeleteId.Length; i++)
                {
                    TblNoticeOfPaymentDetail data = _context.TblNoticeOfPaymentDetails.Find(Transaksis[i].Id);
                    data.IsDeleted = 1; //Jika true data tidak akan ditampilkan dan data masih tersimpan di dalam database
                    //data.DeletedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId)); ;
                    //data.DeletedTime = DateTime.Now;
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
        public ActionResult DeleteDetailNop(string Ids)
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

                List<TblNoticeOfPaymentDetail> Transaksis = _context.TblNoticeOfPaymentDetails.Where(x => confirmedDeleteId.Contains(x.Id)).ToList(); //Ambil data sesuai dengan ID
                for (int i = 0; i < confirmedDeleteId.Length; i++)
                {
                    TblNoticeOfPaymentDetail data = _context.TblNoticeOfPaymentDetails.Find(Transaksis[i].Id);
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
        public ActionResult DeleteFileNopTemp(string Ids)
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

                List<TblFileUploadNopTemp> Transaksis = _context.TblFileUploadNopTemps.Where(x => confirmedDeleteId.Contains(x.Id)).ToList(); //Ambil data sesuai dengan ID
                for (int i = 0; i < confirmedDeleteId.Length; i++)
                {
                    TblFileUploadNopTemp data = _context.TblFileUploadNopTemps.Find(Transaksis[i].Id);
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
        public ActionResult DeleteFileNop(string Ids)
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

                List<TblFileUploadNop> Transaksis = _context.TblFileUploadNops.Where(x => confirmedDeleteId.Contains(x.Id)).ToList(); //Ambil data sesuai dengan ID
                for (int i = 0; i < confirmedDeleteId.Length; i++)
                {
                    if (Transaksis[i].IdFileFromApi != null)
                    {
                        TblNoticeOfPayment checkParent = _context.TblNoticeOfPayments.Where(x => x.Id == Transaksis[i].IdNop).FirstOrDefault();

                        var jsonParseReturnCheck = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNop>>("");
                        var urlCheck = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:Nop:GetDataById"] + checkParent.IdNopFromApi;
                        (bool resultApiCheck, string resultCheck) = RequestToAPI.GetJsonStringWebApi(urlCheck, null);
                        if (resultApiCheck && !string.IsNullOrEmpty(resultCheck))
                        {
                            jsonParseReturnCheck = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNop>>(resultCheck);
                            if (jsonParseReturnCheck.StatusCode == 200 || jsonParseReturnCheck.StatusCode == 201)
                            {
                                if (jsonParseReturnCheck.Data.Status == "Unverified")
                                {
                                    var jsonParseReturnDelete = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNop>>("");
                                    var urlDelete = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:File:Delete"] + Transaksis[i].IdFileFromApi;
                                    (bool resultApiDelete, string resultDelete) = RequestToAPI.DeleteRequestToWebApi(urlDelete, null);
                                    if (resultApiDelete && !string.IsNullOrEmpty(resultDelete))
                                    {
                                        jsonParseReturnDelete = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNop>>(resultDelete);
                                        if (jsonParseReturnDelete.StatusCode == 200 || jsonParseReturnDelete.StatusCode == 201)
                                        {
                                            TblFileUploadNop data = _context.TblFileUploadNops.Find(Transaksis[i].Id);
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
                                        TblFileUploadNop data = _context.TblFileUploadNops.Find(Transaksis[i].Id);
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
                                    var jsonParseReturnDelete = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNop>>("");
                                    var urlDelete = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:File:Delete"] + Transaksis[i].IdFileFromApi;
                                    (bool resultApiDelete, string resultDelete) = RequestToAPI.DeleteRequestToWebApi(urlDelete, null);
                                    if (resultApiDelete && !string.IsNullOrEmpty(resultDelete))
                                    {
                                        jsonParseReturnDelete = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNop>>(resultDelete);
                                        if (jsonParseReturnDelete.StatusCode == 200 || jsonParseReturnDelete.StatusCode == 201)
                                        {
                                            TblFileUploadNop data = _context.TblFileUploadNops.Find(Transaksis[i].Id);
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
                                        TblFileUploadNop data = _context.TblFileUploadNops.Find(Transaksis[i].Id);
                                        data.IsActive = 0;
                                        data.IsDeleted = 1; //Jika true data tidak akan ditampilkan dan data masih tersimpan di dalam database
                                        if (System.IO.File.Exists(Transaksis[i].FilePath))
                                        {
                                            System.IO.File.Delete(Transaksis[i].FilePath);
                                        }
                                        _context.Entry(data).State = EntityState.Modified;
                                        _context.SaveChanges();
                                    }

                                    //TblNoticeOfPayment data = _context.TblNoticeOfPayments.Find(checkParent.IdNopFromApi);
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
                        TblFileUploadNop data = _context.TblFileUploadNops.Find(Transaksis[i].Id);
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
                Task.Run(() => send.SendToDevaNop(Ids));
                return Content("Data sedang di kirim ke Deva, Mohon Cek list Notice Of Payment secara berkala!");
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
            string Path = location.AbsolutePath.Replace(this.ControllerContext.RouteData.Values["action"].ToString(), "Index");
            if (!accessSecurity.IsGetAccess(".." + Path))
            {
                return RedirectToAction("NotAccess", "Error");
            }

            try
            {
                    string[] ArrayIds = Ids.Split(',');

                    foreach (var item in ArrayIds)
                    {
                        TblNoticeOfPayment dataAssign = _context.TblNoticeOfPayments.Where(m => m.Id == int.Parse(item) && m.IsDeleted == 0).FirstOrDefault();

                        NoticeOfPaymentToAPI_ViewModels model = new NoticeOfPaymentToAPI_ViewModels();
                        model.NopNo = dataAssign.NopNo;
                        model.DueDate = dataAssign.DueDate;
                        model.RekId = dataAssign.RekId;
                        model.InterestRate = dataAssign.InterestRate;
                        model.InterestDays = dataAssign.InterestDays;
                        model.Cur = dataAssign.Cur;
                        model.NopDetail = _context.TblNoticeOfPaymentDetails.Where(m => m.NopId == int.Parse(item) && m.IsDeleted == 0).ToList();

                        //Hit API Deva
                        if (dataAssign.IdNopFromApi != null)
                        {
                            var jsonParseReturnCheck = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNop>>("");
                            var urlCheck = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:Nop:GetDataById"] + dataAssign.IdNopFromApi;
                            (bool resultApiCheck, string resultCheck) = RequestToAPI.GetJsonStringWebApi(urlCheck, null);
                            if (resultApiCheck && !string.IsNullOrEmpty(resultCheck))
                            {
                                jsonParseReturnCheck = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNop>>(resultCheck);
                            }

                            if (jsonParseReturnCheck.StatusCode == 200 || jsonParseReturnCheck.StatusCode == 201)
                            {
                                if (jsonParseReturnCheck.Data.Status == "Unverified")
                                {
                                    //Send Detail NOP
                                    List<TblNoticeOfPaymentDetail> dataAssignDetail = _context.TblNoticeOfPaymentDetails.Where(m => m.NopId == int.Parse(item) && m.IsActive == 1 && m.IsDeleted == 0).ToList();
                                    List<TblNoticeOfPaymentDetail> dataAssignDetailDeleted = _context.TblNoticeOfPaymentDetails.Where(m => m.NopId == int.Parse(item) && m.IdNopDetailFromApi != null && m.IsDeleted == 1 && m.IsActive == 1).ToList();
                                    //Delete Detail NOP
                                    foreach (var itemDetailDelete in dataAssignDetailDeleted)
                                    {
                                        if (itemDetailDelete.IdNopDetailFromApi != null)
                                        {
                                            var jsonParseReturnUpdateDetail = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNop>>("");
                                            var urlUpdateDetail = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:DetailNop:Delete"] + itemDetailDelete.IdNopDetailFromApi;
                                            (bool resultApiUpdateDetail, string resultUpdateDetail) = RequestToAPI.DeleteRequestToWebApi(urlUpdateDetail, null);
                                            if (resultApiUpdateDetail && !string.IsNullOrEmpty(resultUpdateDetail))
                                            {
                                                jsonParseReturnUpdateDetail = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNop>>(resultUpdateDetail);
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
                                    //Update Detail NOP
                                    foreach (var itemDetail in dataAssignDetail)
                                        {
                                            //CHECK DetailNOP Registered or No
                                            if (itemDetail.IdNopDetailFromApi != null)
                                            {
                                                var jsonParseReturnUpdateDetail = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNop>>("");
                                                var urlUpdateDetail = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:DetailNop:Update"] + itemDetail.IdNopDetailFromApi;
                                                (bool resultApiUpdateDetail, string resultUpdateDetail) = RequestToAPI.PutRequestToWebApi(urlUpdateDetail, new
                                                {
                                                    NopId = dataAssign.IdNopFromApi,
                                                    CreditorRef = itemDetail.CreditorRef,
                                                    Outstanding = itemDetail.Outstanding,
                                                    Principal = itemDetail.Principal,
                                                    Interest = itemDetail.Interest,
                                                    Fee = itemDetail.Fee
                                                }, null);
                                                if (resultApiUpdateDetail && !string.IsNullOrEmpty(resultUpdateDetail))
                                                {
                                                    jsonParseReturnUpdateDetail = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNop>>(resultUpdateDetail);
                                                    if (jsonParseReturnUpdateDetail.StatusCode == 200 || jsonParseReturnUpdateDetail.StatusCode == 201)
                                                    {
                                                        itemDetail.IdNopDetailFromApi = jsonParseReturnUpdateDetail.Data.Id;
                                                        _context.Entry(itemDetail).State = EntityState.Modified;
                                                        _context.SaveChanges();
                                                    }
                                                }


                                            }
                                            else
                                            {
                                                var jsonParseReturnAddDetail = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNop>>("");
                                                var urlAddDetail = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:DetailNop:Add"];
                                                (bool resultApiAddDetail, string resultAddDetail) = RequestToAPI.PostRequestToWebApi(urlAddDetail, new
                                                {
                                                    NopId = dataAssign.IdNopFromApi,
                                                    CreditorRef = itemDetail.CreditorRef,
                                                    Outstanding = itemDetail.Outstanding,
                                                    Principal = itemDetail.Principal,
                                                    Interest = itemDetail.Interest,
                                                    Fee = itemDetail.Fee
                                                }, null);
                                                if (resultApiAddDetail && !string.IsNullOrEmpty(resultAddDetail))
                                                {
                                                    jsonParseReturnAddDetail = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNop>>(resultAddDetail);
                                                    if (jsonParseReturnAddDetail.StatusCode == 200 || jsonParseReturnAddDetail.StatusCode == 201)
                                                    {
                                                        itemDetail.IdNopDetailFromApi = jsonParseReturnAddDetail.Data.Id;
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

                                    //Send File NOP
                                    List<TblFileUploadNop> dataAssignFile = _context.TblFileUploadNops.Where(m => m.IdNop == int.Parse(item) && m.IsDeleted == 0).ToList();
                                    List<TblFileUploadNop> dataAssignFileDeleted = _context.TblFileUploadNops.Where(m => m.IdNop == int.Parse(item) && m.IdFileFromApi != null && m.IsDeleted == 1 && m.IsActive == 1).ToList();
                                    //Delete File NOP
                                    foreach (var itemFileDelete in dataAssignFileDeleted)
                                    {
                                        if (itemFileDelete.IdFileFromApi != null)
                                        {
                                            var jsonParseReturnUpdateFile = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNop>>("");
                                            var urlUpdateFile = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:File:Delete"] + itemFileDelete.IdFileFromApi;
                                            (bool resultApiUpdateFile, string resultUpdateFile) = RequestToAPI.DeleteRequestToWebApi(urlUpdateFile, null);
                                            if (resultApiUpdateFile && !string.IsNullOrEmpty(resultUpdateFile))
                                            {
                                                jsonParseReturnUpdateFile = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNop>>(resultUpdateFile);
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
                                    //Update Detail NOP
                                    foreach (var itemFile in dataAssignFile)
                                        {
                                            var data = new object();
                                            var check = await _context.TblFileUploadNops.Where(x => x.Id == itemFile.Id).FirstOrDefaultAsync();
                                            if (System.IO.File.Exists(check.FilePath))
                                            {
                                                byte[] fileBytes = System.IO.File.ReadAllBytes(check.FilePath);

                                                // Convert the byte array to a Base64 string
                                                string base64String = Convert.ToBase64String(fileBytes);

                                                var jsonParseReturnUploadBase64 = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNop>>("");
                                                var urlUploadBase64 = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:Nop:UploadBase64"] + dataAssign.IdNopFromApi;
                                                (bool resultApiUploadBase64, string resultUploadBase64) = RequestToAPI.PostRequestToWebApi(urlUploadBase64, new
                                                {
                                                    FileName = itemFile.FileName,
                                                    FileContent = base64String,
                                                }, null);
                                                if (resultApiUploadBase64 && !string.IsNullOrEmpty(resultUploadBase64))
                                                {
                                                    jsonParseReturnUploadBase64 = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNop>>(resultUploadBase64);
                                                    if (jsonParseReturnUploadBase64.StatusCode == 200 || jsonParseReturnUploadBase64.StatusCode == 201)
                                                    {
                                                        itemFile.IdFileFromApi = jsonParseReturnUploadBase64.Data.Key;
                                                        _context.Entry(itemFile).State = EntityState.Modified;
                                                        _context.SaveChanges();
                                                    }
                                                }
                                            }
                                        }
                                    

                                    //Send Update Nop
                                    var jsonParseReturnUpdate = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNop>>("");
                                    var urlUpdate = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:Nop:Update"] + dataAssign.IdNopFromApi;
                                    (bool resultApiUpdate, string resultUpdate) = RequestToAPI.PutRequestToWebApi(urlUpdate, model, null);
                                    if (resultApiUpdate && !string.IsNullOrEmpty(resultUpdate))
                                    {
                                        jsonParseReturnUpdate = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNop>>(resultUpdate);
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

                        else
                        {
                            var jsonParseReturnAdd = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNop>>("");
                            var urlAdd = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:Nop:Add"];
                            (bool resultApiAdd, string resultAdd) = RequestToAPI.PostRequestToWebApi(urlAdd, model, null);
                            if (resultApiAdd && !string.IsNullOrEmpty(resultAdd))
                            {
                                jsonParseReturnAdd = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNop>>(resultAdd);
                                if (jsonParseReturnAdd.StatusCode == 200 || jsonParseReturnAdd.StatusCode == 201)
                                {
                                    dataAssign.IdNopFromApi = jsonParseReturnAdd.Data.Id;
                                    dataAssign.Status = jsonParseReturnAdd.Data.Status;
                                    dataAssign.LastSentDate = DateTime.Now;
                                    _context.Entry(dataAssign).State = EntityState.Modified;
                                    _context.SaveChanges();

                                    //Nop Detail Send
                                    List<TblNoticeOfPaymentDetail> dataAssignDetail = _context.TblNoticeOfPaymentDetails.Where(m => m.NopId == int.Parse(item) && m.IsDeleted == 0).ToList();
                                    foreach (var itemDetail in dataAssignDetail)
                                    {
                                        //CHECK DetailNop Registered or No
                                        var jsonParseReturnAddDetail = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNop>>("");
                                        var urlAddDetail = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:DetailNop:Add"];
                                        (bool resultApiAddDetail, string resultAddDetail) = RequestToAPI.PostRequestToWebApi(urlAddDetail, new
                                        {
                                            NopId = dataAssign.IdNopFromApi,
                                            CreditorRef = itemDetail.CreditorRef,
                                            Outstanding = itemDetail.Outstanding,
                                            Principal = itemDetail.Principal,
                                            Interest = itemDetail.Interest,
                                            Fee = itemDetail.Fee
                                        }, null);
                                        if (resultApiAddDetail && !string.IsNullOrEmpty(resultAddDetail))
                                        {
                                            jsonParseReturnAddDetail = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNop>>(resultAddDetail);
                                            if (jsonParseReturnAddDetail.StatusCode == 200 || jsonParseReturnAddDetail.StatusCode == 201)
                                            {
                                                itemDetail.IdNopDetailFromApi = jsonParseReturnAddDetail.Data.Id;
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

                                    //Nop File Send
                                    List<TblFileUploadNop> dataAssignFile = _context.TblFileUploadNops.Where(m => m.IdNop == int.Parse(item) && m.IsDeleted == 0).ToList();
                                    foreach (var itemFile in dataAssignFile)
                                    {
                                        var data = new object();
                                        var check = await _context.TblFileUploadNops.Where(x => x.Id == itemFile.Id).FirstOrDefaultAsync();
                                        if (System.IO.File.Exists(check.FilePath))
                                        {
                                            byte[] fileBytes = System.IO.File.ReadAllBytes(check.FilePath);

                                            //Convert the byte array to a Base64 string
                                            string base64String = Convert.ToBase64String(fileBytes);

                                            //Base64
                                            var jsonParseReturnUploadBase64 = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNop>>("");
                                            var urlUploadBase64 = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:Nop:UploadBase64"] + jsonParseReturnAdd.Data.Id;
                                            (bool resultApiUploadBase64, string resultUploadBase64) = RequestToAPI.PostRequestToWebApi(urlUploadBase64, new
                                            {
                                                FileName = itemFile.FileName,
                                                FileContent = base64String,
                                            }, null);
                                            if (resultApiUploadBase64 && !string.IsNullOrEmpty(resultUploadBase64))
                                            {
                                                jsonParseReturnUploadBase64 = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNop>>(resultUploadBase64);
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
                                            //        var fullUrl = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:Nop:UploadFile"] + jsonParseReturnAdd.Data.Id;

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
                List<TblNoticeOfPayment> data = _context.TblNoticeOfPayments.Where(m => m.Status == "Unverified").ToList();

                List<string> dataId = _context.TblNoticeOfPayments.Where(m => m.Status == "Unverified").Select(m => m.IdNopFromApi).ToList();

                string formattedData = $"('{string.Join("','", dataId)}')";

                var jsonParseReturnCheck = JsonConvert.DeserializeObject<ResultStatusDataInt<NoticeOfPaymentJOB_ViewModel>>("");
                var urlCheck = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:Nop:GetData"] + "filter=Id in" + formattedData;
                (bool resultApiCheck, string resultCheck) = RequestToAPI.GetJsonStringWebApi(urlCheck, null);
                if (resultApiCheck && !string.IsNullOrEmpty(resultCheck))
                {
                    jsonParseReturnCheck = JsonConvert.DeserializeObject<ResultStatusDataInt<NoticeOfPaymentJOB_ViewModel>>(resultCheck);

                    if (jsonParseReturnCheck.Value != null)
                    {
                        foreach (var item in data)
                        {
                            var updatedStatus = jsonParseReturnCheck.Value.FirstOrDefault(m => m.Id == item.IdNopFromApi);
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
                List<TblNoticeOfPayment> data = _context.TblNoticeOfPayments.Where(m => m.Status == "Unverified").ToList();
                foreach (var item in data)
                {
                    //Hit API Deva
                    if (item.IdNopFromApi != null)
                    {
                        var dataAssign = await _context.TblNoticeOfPayments.Where(m => m.Id == item.Id && m.IsDeleted == 0).FirstOrDefaultAsync();
                        var jsonParseReturnCheck = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNop>>("");
                        var urlCheck = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:Nop:GetDataById"] + dataAssign.IdNopFromApi;
                        (bool resultApiCheck, string resultCheck) = RequestToAPI.GetJsonStringWebApi(urlCheck, null);
                        if (resultApiCheck && !string.IsNullOrEmpty(resultCheck))
                        {
                            jsonParseReturnCheck = JsonConvert.DeserializeObject<ResultStatusDataInt<ResultStatusDataNop>>(resultCheck);
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
        public async Task<IActionResult> PrintExcelNoP(string Tanggal,string TypeExcel)
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

                insertData.Path = GetConfig.AppSetting["Path"] + "Export_Excel_NoP_" + DateTime.Now.ToString("ddMMyyyyHHmmssfff") + "." + TypeExcel;
                insertData.FileName = "Export_Excel_NoP_" + DateTime.Now.ToString("ddMMyyyyHHmmssfff");
                insertData.StatusDownload = 0;
                insertData.FileExt = "xlsx";
                insertData.CreatedTime = DateTime.Now;
                insertData.CreatedById = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_UserId));
                await _context.TblDownloadBigFiles.AddAsync(insertData);
                await _context.SaveChangesAsync();

                var print = new PrintExport(_converter);
                Task.Run(() => print.PrintNoPExcel(TypeExcel, insertData.FileName, insertData.Id, Tanggal));
                return Content("Data Berhasil di Request untuk Download, Mohon Cek di Menu Download List File!");
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task<IActionResult> PrintPDFNoP(string Tanggal)
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
                var req = new { tanggal = Tanggal };

                var regex = await RegexRequest.RegexValidation(req);
                if (!regex)
                {
                    return Content("Bad Request!");
                }



                insertData.Path = GetConfig.AppSetting["Path"] + "Export_Pdf_NoP_" + DateTime.Now.ToString("ddMMyyyyHHmmssfff") + ".pdf";
                insertData.FileName = "Export_Pdf_NoP_" + DateTime.Now.ToString("ddMMyyyyHHmmssfff");
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
                Task.Run(() => print.PrintNoPPDF(Tanggal, namaPegawai, requestScheme, requestHost, insertData.Id));
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
                var check = await _context.TblFileUploadNopTemps.Where(x => x.Id == id).FirstOrDefaultAsync();
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
                var check = await _context.TblFileUploadNops.Where(x => x.Id == id).FirstOrDefaultAsync();
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
                else
                {
                    return false;
                }
            }
            catch (Exception Ex)
            {
                return false;
            }
        }
        public DropdownServerSideIntVM SelectDataRekId(string rekName)
        {
            DropdownServerSideIntVM dataReturn = new DropdownServerSideIntVM();

            var url = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:RekId"] + "?$top=1&$skip=0&$orderby=Acc asc&$filter=contains(Acc ,'" + rekName + "')";
            (bool resultApi, string result) = RequestToAPI.GetJsonStringWebApi(url, null);

            if (resultApi && !string.IsNullOrEmpty(result))
            {
                var jsonParseReturn = JsonConvert.DeserializeObject<ResultStatusDataInt<DataDropdownServerSideDeva>>(result);

                if (jsonParseReturn.Value != null)
                {
                    dataReturn.id = jsonParseReturn.Value[0].RekId;
                    dataReturn.text = jsonParseReturn.Value[0].acc;
                }
                else {
                    dataReturn.id = null;
                    dataReturn.text = null;

                }
            }
            return dataReturn;
        }
        #endregion

    }
}