using DashboardDevaBNI.Component;
using DashboardDevaBNI.Models;
using DashboardDevaBNI.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NPOI.POIFS.Crypt.Dsig;
using NPOI.SS.Formula.Functions;
using NPOI.XWPF.UserModel;

namespace DashboardDevaBNI.Controllers
{
    public class UtilityController : Controller
    {
        private readonly DbDashboardDevaBniContext _context;
        private readonly IConfiguration _configuration;
        IHttpContextAccessor _accessor;
        private readonly LastSessionLog lastSession;

        public UtilityController(IConfiguration config, DbDashboardDevaBniContext context, IHttpContextAccessor accessor)
        {
            _context = context;
            _configuration = config;
            _accessor = accessor;
            lastSession = new LastSessionLog(accessor, context, config);
        }
        
        public JsonResult GetAllLookup(string q, string type, string page, int rowPerPage)
        {
            ListDataDropdownServerSide source = new ListDataDropdownServerSide();
            //var RoleCode = HttpContext.Session.GetString(SessionConstan.Session_Role_Id);
            //if (RoleCode == GetConfig.AppSetting["AppSettings:Roles:SuperAdmin"] || RoleCode == GetConfig.AppSetting["AppSettings:Roles:Admin"])
            //{
            source.items = new List<DataDropdownServerSide>();

            source.items = StoredProcedureExecutor.ExecuteSPList<DataDropdownServerSide>(_context, "[SP_Dropdown_Lookup]", new SqlParameter[]{
                        new SqlParameter("@Parameter", q),
                        new SqlParameter("@Type", type)
                });

            source.total_count = StoredProcedureExecutor.ExecuteScalarInt(_context, "[SP_Dropdown_Lookup_Count]", new SqlParameter[]{
                        new SqlParameter("@Parameter", q),
                        new SqlParameter("@Type", type),
                });
            //}

            return Json(source);
        }
        public JsonResult GetAllDataRole(string search, int page, int rowrowPerPage)
        {
            List<DropDownRole> dropDownRole = new List<DropDownRole>();
            dropDownRole = StoredProcedureExecutor.ExecuteSPListNoParam<DropDownRole>(_context, "sp_Dropdown_MasterRole");

            var RoleKode = int.Parse(HttpContext.Session.GetString(SessionConstan.Session_RoleKode));

            if (RoleKode == 1)
            {
                return Json(dropDownRole);
            }
            else
            {
                return Json(dropDownRole.Where(m => m.id != 1));
            }
        }
        public JsonResult GetAllDataParentMenu(string search, int page, int rowPerPage)
        {
            DropDownParentList source = new DropDownParentList();
            source.items = new List<DropDownParent>();

            source.items = StoredProcedureExecutor.ExecuteSPList<DropDownParent>(_context, "[SP_Dropdown_ParentMenu]", new SqlParameter[]
            {
                new SqlParameter("@Parameter", search),
                new SqlParameter("@PageNumber", page),
                new SqlParameter("@RowsPage", rowPerPage)
            });

            source.total_count = StoredProcedureExecutor.ExecuteScalarInt(_context, "[SP_Dropdown_ParentMenu_Count]", new SqlParameter[]
            {
                new SqlParameter("@Parameter", search)
            });

            return Json(source);
        }
        public JsonResult GetDropdownRolesByMenuId(string Roles)
        {
            lastSession.Update();
            List<DataDropdownServerSide> data = new List<DataDropdownServerSide>();

            data = StoredProcedureExecutor.ExecuteSPList<DataDropdownServerSide>(_context, "[SP_Dropdown_Menu_GetDataRolesById]", new SqlParameter[]{
                        new SqlParameter("@Id", Roles)});

            return Json(data);
        }

        //Dropdown API Deva
        #region Get All Dropdown Rekening Giro
        public JsonResult GetAllDataDropdownCreditorRef(string q, string page, int rowPerPage)
        {
            //lastSession.Update();
            ListDataDropdownServerSideDeva source = new ListDataDropdownServerSideDeva();

            var Rows = rowPerPage > 50 ? 50: rowPerPage;
            var Search = q == null ? "" : q;
            try
            {
                var url = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:CreditorRef"] + "top=" + Rows + "&skip=" + int.Parse(page) + "&count=true&orderby=CreditorRef asc&filter=contains(CreditorRef , '" + Search + "')";
                (bool resultApi, string result) = RequestToAPI.GetJsonStringWebApi(url, null);
                if (resultApi && !string.IsNullOrEmpty(result))
                {
                    var jsonParseReturn = JsonConvert.DeserializeObject<ResultStatusDataInt<DataDropdownServerSideDeva>>(result);

                    source.items = jsonParseReturn.Value;
                    foreach (var item in jsonParseReturn.Value) {
                        item.id = item.CreditorRef;
                        item.text = item.CreditorRef;
                    }
                    source.total_count = jsonParseReturn.ODataCount;
                }
            }
            catch (Exception Ex)
            {
                throw;
            }

            return Json(source);
        }
        public JsonResult GetAllDataDropdownRekId(string q, string page, int rowPerPage)
        {
            //lastSession.Update();
            ListDataDropdownServerSideDeva source = new ListDataDropdownServerSideDeva();

            var Rows = rowPerPage > 50 ? 50 : rowPerPage;
            var Search = q == null ? "" : q;
            try
            {
                var url = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:RekId"] + "top=" + Rows + "&skip=" + int.Parse(page) + "&count=true&orderby=RekId asc&filter=contains(Acc , '" + Search + "')";
                (bool resultApi, string result) = RequestToAPI.GetJsonStringWebApi(url, null);
                if (resultApi && !string.IsNullOrEmpty(result))
                {
                    var jsonParseReturn = JsonConvert.DeserializeObject<ResultStatusDataInt<DataDropdownServerSideDeva>>(result);

                    source.items = jsonParseReturn.Value;
                    foreach (var item in jsonParseReturn.Value)
                    {
                        item.id = item.RekId.ToString();
                        item.text = item.acc;
                    }
                    source.total_count = jsonParseReturn.ODataCount;
                }
            }
            catch (Exception Ex)
            {
                throw;
            }

            return Json(source);
        }
        public JsonResult GetDataDetailCreditorRef(string CreditorRef, string DueDate)
        {
            DateTime dateTime = DateTime.ParseExact(DueDate, "yyyy-MM-dd", null);
            DateTimeOffset dateTimeOffset = new DateTimeOffset(dateTime, TimeSpan.Zero);
            string FinalDueDate = dateTimeOffset.ToString("yyyy-MM-ddTHH:mm:ssZ");
            //var FinalDueDate = DateTime.Parse(DueDate);

            CreditorRefDetail_ViewModels source = new CreditorRefDetail_ViewModels();

            try
            {
                var url = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:CreditorRefDetail"] + "top=" + 50 + "&skip=" + 0 + "&orderby=CreditorRef asc&filter=contains(CreditorRef , '" + CreditorRef + "') and DueDate eq " + FinalDueDate;
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
                    else {
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
    }
}
