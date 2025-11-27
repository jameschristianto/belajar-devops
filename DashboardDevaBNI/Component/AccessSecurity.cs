using DashboardDevaBNI.Models;
using Microsoft.Data.SqlClient;
using DashboardDevaBNI.Models;

namespace DashboardDevaBNI.Component
{
    public class AccessSecurity
    {
        IHttpContextAccessor _httpContextAccessor;
        private readonly DbDashboardDevaBniContext _context;
        private readonly IConfiguration _configuration;
        public AccessSecurity(IHttpContextAccessor httpContextAccessor, DbDashboardDevaBniContext context, IConfiguration config)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
            _configuration = config;
        }
        public bool IsGetAccess(string fullpath)
        {
            bool getAccess = false;
            var Role_Id = _httpContextAccessor.HttpContext.Session.GetString(SessionConstan.Session_RoleKode);
            var Username = _httpContextAccessor.HttpContext.Session.GetString(SessionConstan.Session_Username_Pegawai);
            var CheckVerifEmail = _context.TblMasterUserVerifs.Where(m => m.Username == Username && m.IsActive == true && (m.IsDeleted == false || m.IsDeleted == null)).Select(m => m.IsEmailVerif).FirstOrDefault();
            var CheckVerifWhatsapp = _context.TblMasterUserVerifs.Where(m => m.Username == Username && m.IsActive == true && (m.IsDeleted == false || m.IsDeleted == null)).Select(m => m.IsNoTelpVerif).FirstOrDefault();

            var CheckVerifEmailMaster = _context.TblMasterSystemParameters.Where(m => m.Key == "OtpBlastEmail" && m.IsActive == true && (m.IsDeleted == false || m.IsDeleted == null)).Select(m => m.Value).FirstOrDefault();
            var CheckVerifWhatsappMaster = _context.TblMasterSystemParameters.Where(m => m.Key == "OtpBlastWa" && m.IsActive == true && (m.IsDeleted == false || m.IsDeleted == null)).Select(m => m.Value).FirstOrDefault();

            if (CheckVerifEmailMaster == "0" && CheckVerifWhatsappMaster == "0")
            {
                getAccess = StoredProcedureExecutor.ExecuteScalarBool(_context, "sp_CheckAccessMenu", new SqlParameter[]{
                        new SqlParameter("@RoleId", Role_Id ),
                        new SqlParameter("@Url", fullpath)});
            }
            else if (CheckVerifEmail == false && CheckVerifWhatsapp == false)
            {
                getAccess = false;
            }
            else
            {
                getAccess = StoredProcedureExecutor.ExecuteScalarBool(_context, "sp_CheckAccessMenu", new SqlParameter[]{
                        new SqlParameter("@RoleId", Role_Id ),
                        new SqlParameter("@Url", fullpath)});
            }
            //string fullpath = "../" + controller + "/Index";

            //foreach(var item in data)
            //{
            //    if(item.HasAccess==1 || item.HasAccess1 == 1)
            //    {
            //        getAccess = true;
            //    }
            //}
            return getAccess;
        }
    }
}
