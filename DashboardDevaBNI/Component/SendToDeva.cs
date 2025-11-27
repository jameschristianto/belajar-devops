using DashboardDevaBNI.Models;
using DashboardDevaBNI.ViewModels;
using DinkToPdf.Contracts;
using iText.Commons.Actions.Contexts;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace DashboardDevaBNI.Component
{
    public class SendToDeva
    {
        private readonly IConverter _converter;

        public SendToDeva(IConverter converter)
        {
            _converter = converter;
        }

        public async Task SendToDevaNod(string Ids)
        {
            DbDashboardDevaBniContext _context = new DbDashboardDevaBniContext();

            try
            {
                string[] ArrayIds = Ids.Split(',');

                foreach (var item in ArrayIds)
                {
                    var dataAssign = await _context.TblNoticeOfDisbursements.Where(m => m.Id == int.Parse(item) && m.IsDeleted == 0).FirstOrDefaultAsync();

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
                                    }
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

        public async Task SendToDevaNop(string Ids)
        {
            DbDashboardDevaBniContext _context = new DbDashboardDevaBniContext();

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
                                    }
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
    }
}


