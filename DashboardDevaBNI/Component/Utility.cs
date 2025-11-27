using Amazon.Auth.AccessControlPolicy;
using DashboardDevaBNI.Models;
using DashboardDevaBNI.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using NPOI.XWPF.UserModel;

namespace DashboardDevaBNI.Component
{
    public class Utility
    {

        #region Select Data Lookup
        public static IEnumerable<DropdownServerSideIntVM> SelectDataLookupById(int? Id, String type, DbDashboardDevaBniContext _context)
        {
            List<DropdownServerSideIntVM> data = new List<DropdownServerSideIntVM>();

            data = StoredProcedureExecutor.ExecuteSPList<DropdownServerSideIntVM>(_context, "[sp_Dropdown_Lookup_ById]", new SqlParameter[]{
                        new SqlParameter("@Id", Id),
                        new SqlParameter("@Type", type)

                    });
            data = data != null ? data : new List<DropdownServerSideIntVM>();

            return data;
        }
        #endregion

        #region Select Data ParentMenu
        public static IEnumerable<DataDropdownServerSide> SelectDataParentMenu(long? Id, DbDashboardDevaBniContext _context)
        {
            List<DataDropdownServerSide> data = new List<DataDropdownServerSide>();

            data = StoredProcedureExecutor.ExecuteSPList<DataDropdownServerSide>(_context, "[sp_Dropdown_ParentMenu_ById]", new SqlParameter[]{
                        new SqlParameter("@Id", Id)

                    });
            data = data != null ? data : new List<DataDropdownServerSide>();

            return data;
        }
        #endregion

        #region Select Data Master Role
        public static IEnumerable<DataDropdownServerSide> SelectDataMasterRole(DbDashboardDevaBniContext _context)
        {
            List<DataDropdownServerSide> data = new List<DataDropdownServerSide>();

            data = StoredProcedureExecutor.ExecuteSPList<DataDropdownServerSide>(_context, "[sp_Dropdown_MasterRole]", new SqlParameter[]{

                    });
            data = data != null ? data : new List<DataDropdownServerSide>();

            return data;
        }
        public static IEnumerable<DataDropdownServerSide> SelectDataRole(int? Id, DbDashboardDevaBniContext _context)
        {
            List<DataDropdownServerSide> data = new List<DataDropdownServerSide>();

            data = StoredProcedureExecutor.ExecuteSPList<DataDropdownServerSide>(_context, "[sp_Dropdown_Role_ById]", new SqlParameter[]{
                new SqlParameter("@Id", Id)
                    });
            data = data != null ? data : new List<DataDropdownServerSide>();

            return data;
        }
        #endregion
        public static IEnumerable<DropdownServerSideIntVM> SelectDataRekId(int rekId)
		{
			List<DropdownServerSideIntVM> dataReturn = new List<DropdownServerSideIntVM>();

			var url = GetConfig.AppSetting["ApiDeva:BaseApi"] + GetConfig.AppSetting["ApiDeva:RekId"] + "?$top=1&$skip=0&$orderby=Acc asc&$filter=RekId eq " + rekId;
            (bool resultApi, string result) = RequestToAPI.GetJsonStringWebApi(url, null);
            
            if (resultApi && !string.IsNullOrEmpty(result))
            {
                var jsonParseReturn = JsonConvert.DeserializeObject<ResultStatusDataInt<DataDropdownServerSideDeva>>(result);

				foreach (var item in jsonParseReturn.Value)
				{
					DropdownServerSideIntVM newItem = new DropdownServerSideIntVM
					{
						id = item.RekId,
						text = item.acc
					};

					dataReturn.Add(newItem);
				}
			}
            return dataReturn;
        }
    }
}
