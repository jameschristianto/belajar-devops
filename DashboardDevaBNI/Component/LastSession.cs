using System.Net;
using UAParser;
using Microsoft.AspNetCore.Http.Extensions;
using DashboardDevaBNI.Models;

namespace DashboardDevaBNI.Component
{
    public class LastSessionLog
    {
        IHttpContextAccessor _httpContextAccessor;
        private readonly DbDashboardDevaBniContext _context;
        private readonly IConfiguration _configuration;
        public LastSessionLog(IHttpContextAccessor httpContextAccessor, DbDashboardDevaBniContext context, IConfiguration config)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
            _configuration = config;
        }

        public bool Update()
        {
            bool isSession = false;
            IPHostEntry heserver = Dns.GetHostEntry(Dns.GetHostName());
            // get user ip info
            var ipAddress = heserver.AddressList.ToList().Where(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).FirstOrDefault().ToString();
            var userAgent = _httpContextAccessor.HttpContext.Request.Headers["User-Agent"];
            string uaString = Convert.ToString(userAgent[0]);
            var uaParser = Parser.GetDefault();
            ClientInfo c = uaParser.Parse(uaString);

            var url = _httpContextAccessor.HttpContext.Request.GetDisplayUrl();

            TblUserSession UserSession = new TblUserSession();

            if (_httpContextAccessor.HttpContext.Session.GetString(SessionConstan.Session_UserId) == null)
            {
                //AccountController cont = new AccountController(_configuration, _context, _httpContextAccessor);
                //cont.CekSession();
                isSession = false;
            }
            else
            {
                var UserId = int.Parse(_httpContextAccessor.HttpContext.Session.GetString(SessionConstan.Session_UserId));
                //var Npp = _httpContextAccessor.HttpContext.Session.GetString(SessionConstan.Session_NPP_Pegawai);

                TblUserSession ds = _context.TblUserSessions.Where(f => f.UserId == UserId).FirstOrDefault();
                if (ds != null)
                {
                    ds.SessionId = _httpContextAccessor.HttpContext.Session.Id;
                    ds.LastActive = DateTime.Now;
                    ds.RoleId = int.Parse(_httpContextAccessor.HttpContext.Session.GetString(SessionConstan.Session_RoleKode));
                    ds.Info = ipAddress;

                    _context.TblUserSessions.Update(ds);
                    _context.SaveChanges();
                }
                else
                {
                    UserSession.UserId = int.Parse(_httpContextAccessor.HttpContext.Session.GetString(SessionConstan.Session_UserId));
                    UserSession.SessionId = _httpContextAccessor.HttpContext.Session.Id;
                    UserSession.LastActive = DateTime.Now;
                    UserSession.Info = ipAddress;
                    UserSession.RoleId = int.Parse(_httpContextAccessor.HttpContext.Session.GetString(SessionConstan.Session_RoleKode));

                    _context.TblUserSessions.Add(UserSession);
                    _context.SaveChanges();
                }

                //Masukkan ke dalam table Log Activity
                TblLogActivity dataLog = new TblLogActivity();
                dataLog.UserId = UserId;
                //dataLog.Npp = Npp;
                dataLog.Url = url;
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
                _context.TblLogActivities.Add(dataLog);
                _context.SaveChanges();
                isSession = true;
            }
            return isSession;
        }
    }
}
