using DashboardDevaBNI.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using DashboardDevaBNI.Component;
using System.Globalization;
using DashboardDevaBNI.Models;
using Microsoft.AspNetCore.Mvc;
using RazorLight;
using DinkToPdf.Contracts;
using DinkToPdf;
using Org.BouncyCastle.Ocsp;
using System;
using static NPOI.HSSF.Util.HSSFColor;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Geom;
using Newtonsoft.Json.Linq;
using NPOI.POIFS.Crypt.Dsig;
using iText.Commons.Bouncycastle.Cert.Ocsp;
using System.Collections.Generic;

namespace DashboardDevaBNI.Component
{
    public class PrintExport
    {
        private readonly IConverter _converter;

        public PrintExport(IConverter converter)
        {
            _converter = converter;
        }

        public async Task PrintNoDExcel(string TypeExcel, string FileName, int IdFile, string Tanggal)
        {
            DbDashboardDevaBniContext _context = new DbDashboardDevaBniContext();

            try
            {
                CultureInfo culture = new CultureInfo("id-ID");
                var dateArray = new string[] { };

                if (!string.IsNullOrEmpty(Tanggal))
                {
                    dateArray = Tanggal.Split(" to ");
                }

                var data = StoredProcedureExecutor.ExecuteSPList<TblNoticeOfDisbursement>(_context, "sp_Load_NoticeOfDisburesement_View", new SqlParameter[]{
                    new SqlParameter("@NodDateFrom", dateArray.Count() == 0 ? "" : DateTime.Parse(dateArray[0], culture)),
                    new SqlParameter("@NodDateTo", dateArray.Count() == 0 ? "" : DateTime.Parse(dateArray[1], culture)),
                    new SqlParameter("@sortColumn", "Id"),
                    new SqlParameter("@sortColumnDir", "desc"),
                    new SqlParameter("@PageNumber", 1),
                    new SqlParameter("@RowsPage", 10000)
                });

                var updateFile = await _context.TblDownloadBigFiles.Where(x => x.Id == IdFile).FirstOrDefaultAsync();

                //Local Only
                if (!Directory.Exists(GetConfig.AppSetting["Path"]))
                {
                    Directory.CreateDirectory(GetConfig.AppSetting["Path"]);
                }

                //Local & Minio
                //string filePath = Path.Combine(GetConfig.AppSetting["Path"], FileName);

                //if (!File.Exists(filePath))
                //{
                //    var log = new TblLogErrorPrint();
                //    log.IdFile = IdFile;
                //    log.ErrorMessage = "File Not Found";
                //    log.CreatedTime = DateTime.Now;
                //    await _context.TblLogErrorPrints.AddAsync(log);
                //    await _context.SaveChangesAsync();
                //}

                //var fileNameReplaceSpacePendukung = FileName.Replace(" ", "_");
                //using (var stream = new MemoryStream())
                //{
                //    using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                //    {
                //        await fileStream.CopyToAsync(stream);
                //    }

                //    if (stream.Length == 0)
                //    {
                //        var log = new TblLogErrorPrint();
                //        log.IdFile = IdFile;
                //        log.ErrorMessage = "File Corrupt!";
                //        log.CreatedTime = DateTime.Now;
                //        await _context.TblLogErrorPrints.AddAsync(log);
                //        await _context.SaveChangesAsync();
                //    }

                //    var upload = await ExternalAPI.UploadMinio(stream, fileNameReplaceSpacePendukung);
                //    if (!upload)
                //    {
                //        var log = new TblLogErrorPrint();
                //        log.IdFile = IdFile;
                //        log.ErrorMessage = "Upload Failed!";
                //        log.CreatedTime = DateTime.Now;
                //        await _context.TblLogErrorPrints.AddAsync(log);
                //        await _context.SaveChangesAsync();
                //    }
                //    else {
                //        File.Delete(filePath);
                //    }
                //    stream.Position = 0;
                //}

                if (data != null)
                {
                    IWorkbook workbook;

                    if (TypeExcel == "xlsx")
                    {
                        workbook = new XSSFWorkbook();
                    }
                    else if (TypeExcel == "xls")
                    {
                        workbook = new HSSFWorkbook();
                    }
                    else
                    {
                        throw new Exception("This format is not supported");
                    }

                    //var workbook = new HSSFWorkbook();
                    ReportExcel reportExcel = new ReportExcel();
                    ISheet sheet = workbook.CreateSheet("Data");
                    sheet = reportExcel.standartSheet(sheet);
                    ICellStyle HeaderCellStyle = reportExcel.getJudulStyle1(workbook, TypeExcel);
                    HeaderCellStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
                    HeaderCellStyle.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
                    HeaderCellStyle.FillForegroundColor = IndexedColors.White.Index;
                    int rowExcel = 0;

                    reportExcel.setColoumWidth(sheet, new int[]
                    { 7000, 7000, 7000, 7000, 7000, 7000 }, 0);

                    reportExcel.setDataRow(sheet, new object[]
                    { "NoD No", "NoD Date", "Value Date","Currency", "LastSentDate", "Status"}, rowExcel++);

                    foreach (var cell in sheet.GetRow(rowExcel - 1).Cells)
                    {
                        cell.CellStyle = HeaderCellStyle;
                    }

                    //foreach (var cell in sheet.GetRow(rowExcel - 1).Cells)
                    //{
                    //    cell.CellStyle = HeaderCellStyle;
                    //    cell.CellStyle.Alignment = HorizontalAlignment.Center;
                    //}

                    foreach (var cell in sheet.GetRow(rowExcel - 1).Cells)
                    {
                        cell.CellStyle = HeaderCellStyle;
                        cell.CellStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
                    }

                    int row = 2;
                    foreach (var vw in data)
                    {
                        reportExcel.setDataRow(sheet, new object[]
                        {
                            vw.NodNo,
                            vw.NodDate?.ToString("dd/MM/yyyy"),
                            vw.ValueDate?.ToString("dd/MM/yyyy"),
                            vw.Cur,
                            vw.LastSentDate?.ToString("dd/MM/yyyy"),
                            vw.Status,
                        }, rowExcel++);
                    }


                    MemoryStream output = new MemoryStream();
                    workbook.Write(output, true);

                    using (FileStream fileStream = new FileStream(updateFile.Path, FileMode.Create, FileAccess.Write))
                    {
                        //SAVE FILE
                        output.WriteTo(fileStream);
                    }

                    updateFile.StatusDownload = 1;
                    _context.TblDownloadBigFiles.Update(updateFile);
                    _context.SaveChanges();
                }
                else
                {
                    updateFile.StatusDownload = 2;
                    _context.TblDownloadBigFiles.Update(updateFile);
                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                var log = new TblLogErrorPrint();
                log.IdFile = IdFile;
                log.ErrorMessage = ex.Message + " - " + ex.InnerException;
                log.CreatedTime = DateTime.Now;
                await _context.TblLogErrorPrints.AddAsync(log);
                await _context.SaveChangesAsync();
            }
        }

        public async Task PrintNoDPDF(string Tanggal, string namaPegawai, string Scheme, string Host, int IdFile)
        {
            DbDashboardDevaBniContext _context = new DbDashboardDevaBniContext();
            try
            {
                var updateFile = await _context.TblDownloadBigFiles.Where(x => x.Id == IdFile).FirstOrDefaultAsync();

                if (!Directory.Exists(GetConfig.AppSetting["Path"]))
                {
                    Directory.CreateDirectory(GetConfig.AppSetting["Path"]);
                }

                CultureInfo culture = new CultureInfo("id-ID");
                var dateArray = new string[] { };

                if (!string.IsNullOrEmpty(Tanggal))
                {
                    dateArray = Tanggal.Split(" to ");
                }

                var headerToExport = StoredProcedureExecutor.ExecuteSPList<TblNoticeOfDisbursement>(_context, "sp_Load_NoticeOfDisburesement_View", new SqlParameter[]
                {
                        new SqlParameter("@NodDateFrom", dateArray.Count() == 0 ? "" : DateTime.Parse(dateArray[0], culture)),
                        new SqlParameter("@NodDateTo", dateArray.Count() == 0 ? "" : DateTime.Parse(dateArray[1], culture)),
                        new SqlParameter("@sortColumn", "id"),
                        new SqlParameter("@sortColumnDir", "DESC"),
                        new SqlParameter("@PageNumber", 1),
                        new SqlParameter("@RowsPage", 10000)
                });

                if (headerToExport != null /*&& finalData.Count <= 10000*/)
                {
                    var locationnow = new Uri($"{Scheme}://{Host}");
                    string imagePath = "wwwroot/lib/images/logoBNI.png";
                    byte[] imageBytes = System.IO.File.ReadAllBytes(imagePath);
                    string base64String = Convert.ToBase64String(imageBytes);

                    ExportToPDFNoD_ViewModel model = new ExportToPDFNoD_ViewModel();
                    model.Domain = locationnow.AbsoluteUri;
                    model.UserLogin = namaPegawai;
                    model.TanggalCetak = DateTime.Now.ToString("dd/MM/yyyy HH:mm", culture);
                    model.Logo = base64String;
                    model.ListData = headerToExport;

                    var engine = new RazorLightEngineBuilder()
                    .UseMemoryCachingProvider()
                    .Build();

                    string templateFilePath = "Views/NoticeOfDisbursement/_PdfExportNoD.cshtml"; // Replace with your CSHTML file path
                    string template = File.ReadAllText(templateFilePath);
                    string result = string.Empty;

                    try
                    {
                        result = await engine.CompileRenderStringAsync("templateKey", template, model);
                    }
                    catch (Exception ex)
                    {
                        var log = new TblLogErrorPrint();
                        log.IdFile = IdFile;
                        log.ErrorMessage = ex.Message + " - " + ex.InnerException;
                        log.CreatedTime = DateTime.Now;
                        await _context.TblLogErrorPrints.AddAsync(log);
                        await _context.SaveChangesAsync();
                    }
                    var globalSettings = new GlobalSettings
                    {
                        ColorMode = ColorMode.Color,
                        Orientation = Orientation.Landscape,
                        PaperSize = PaperKind.A4,
                        Margins = new MarginSettings { Top = 5, Right = 5, Left = 5, Bottom = 5 },

                    };

                    var objectSettings = new ObjectSettings
                    {
                        PagesCount = true,
                        HtmlContent = result, // Replace this with your HTML content
                        FooterSettings = new FooterSettings
                        {
                            FontSize = 7,
                            FontName = "Helvetica Light",
                            Left = "Hal [page]/[toPage]",
                            Center = "Sudah Divalidasi dan Disahkan Secara Digital",
                            Right = $"https://deva.bni.co.id, {model.UserLogin}, {model.TanggalCetak}"
                        }
                    };

                    var pdf = new HtmlToPdfDocument()
                    {
                        GlobalSettings = globalSettings,
                        Objects = { objectSettings }
                    };

                    //byte[] file = _converter.Convert(pdf);
                    using (var pdfTools = new PdfTools())
                    {
                        var converter = new SynchronizedConverter(pdfTools);
                        byte[] file = converter.Convert(pdf);

                        using (MemoryStream inputMemoryStream = new MemoryStream(file))
                        using (MemoryStream outputMemoryStream = new MemoryStream())
                        {
                            PdfReader pdfReader = new PdfReader(inputMemoryStream);
                            PdfWriter pdfWriter = new PdfWriter(outputMemoryStream);
                            using (PdfDocument pdfDocument = new PdfDocument(pdfReader, pdfWriter))
                            {
                                Document document = new Document(pdfDocument);

                                // Create a watermark element
                                Paragraph watermark = new Paragraph($"{model.UserLogin} - {model.TanggalCetak.Replace(".", ":")} WIB")
                                    .SetFontColor(iText.Kernel.Colors.ColorConstants.LIGHT_GRAY)
                                    .SetFontSize(30)
                                    .SetOpacity(0.3f)
                                    .SetWidth(2000)
                                    .SetRotationAngle(-0.5);

                                // Add the watermark to each page
                                for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
                                {
                                    float centerX = document.GetPdfDocument().GetDefaultPageSize().GetWidth() / 2;
                                    float centerY = document.GetPdfDocument().GetDefaultPageSize().GetHeight() / 2;
                                    document.ShowTextAligned(watermark, 425, 300, i, TextAlignment.CENTER, iText.Layout.Properties.VerticalAlignment.MIDDLE, 45);
                                }

                            }
                            //SAVE FILE
                            File.WriteAllBytes(updateFile.Path, outputMemoryStream.ToArray());
                        }
                        updateFile.StatusDownload = 1;
                        _context.TblDownloadBigFiles.Update(updateFile);
                        _context.SaveChanges();
                    }
                }
                else
                {
                    updateFile.StatusDownload = 2;
                    _context.TblDownloadBigFiles.Update(updateFile);
                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                var log = new TblLogErrorPrint();
                log.IdFile = IdFile;
                log.ErrorMessage = ex.Message + " - " + ex.InnerException;
                log.CreatedTime = DateTime.Now;
                await _context.TblLogErrorPrints.AddAsync(log);
                await _context.SaveChangesAsync();
            }
        }

        public async Task PrintNoPExcel(string TypeExcel, string FileName, int IdFile, string Tanggal)
        {
            DbDashboardDevaBniContext _context = new DbDashboardDevaBniContext();

            try
            {
                CultureInfo culture = new CultureInfo("id-ID");
                var dateArray = new string[] { };

                if (!string.IsNullOrEmpty(Tanggal))
                {
                    dateArray = Tanggal.Split(" to ");
                }

                var data = StoredProcedureExecutor.ExecuteSPList<TblNoticeOfPayment>(_context, "sp_Load_NoticeOfPayment_View", new SqlParameter[]{
                    new SqlParameter("@DueDateFrom", dateArray.Count() == 0 ? "" : DateTime.Parse(dateArray[0], culture)),
                    new SqlParameter("@DueDateTo", dateArray.Count() == 0 ? "" : DateTime.Parse(dateArray[1], culture)),
                    new SqlParameter("@sortColumn", "Id"),
                    new SqlParameter("@sortColumnDir", "desc"),
                    new SqlParameter("@PageNumber", 1),
                    new SqlParameter("@RowsPage", 10000)
                });

                var updateFile = await _context.TblDownloadBigFiles.Where(x => x.Id == IdFile).FirstOrDefaultAsync();

                //Local Only
                if (!Directory.Exists(GetConfig.AppSetting["Path"]))
                {
                    Directory.CreateDirectory(GetConfig.AppSetting["Path"]);
                }

                //Local & Minio
                //string filePath = Path.Combine(GetConfig.AppSetting["Path"], FileName);

                //if (!File.Exists(filePath))
                //{
                //    var log = new TblLogErrorPrint();
                //    log.IdFile = IdFile;
                //    log.ErrorMessage = "File Not Found";
                //    log.CreatedTime = DateTime.Now;
                //    await _context.TblLogErrorPrints.AddAsync(log);
                //    await _context.SaveChangesAsync();
                //}

                //var fileNameReplaceSpacePendukung = FileName.Replace(" ", "_");
                //using (var stream = new MemoryStream())
                //{
                //    using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                //    {
                //        await fileStream.CopyToAsync(stream);
                //    }

                //    if (stream.Length == 0)
                //    {
                //        var log = new TblLogErrorPrint();
                //        log.IdFile = IdFile;
                //        log.ErrorMessage = "File Corrupt!";
                //        log.CreatedTime = DateTime.Now;
                //        await _context.TblLogErrorPrints.AddAsync(log);
                //        await _context.SaveChangesAsync();
                //    }

                //    var upload = await ExternalAPI.UploadMinio(stream, fileNameReplaceSpacePendukung);
                //    if (!upload)
                //    {
                //        var log = new TblLogErrorPrint();
                //        log.IdFile = IdFile;
                //        log.ErrorMessage = "Upload Failed!";
                //        log.CreatedTime = DateTime.Now;
                //        await _context.TblLogErrorPrints.AddAsync(log);
                //        await _context.SaveChangesAsync();
                //    }
                //    else {
                //        File.Delete(filePath);
                //    }
                //    stream.Position = 0;
                //}

                if (data != null)
                {
                    IWorkbook workbook;

                    if (TypeExcel == "xlsx")
                    {
                        workbook = new XSSFWorkbook();
                    }
                    else if (TypeExcel == "xls")
                    {
                        workbook = new HSSFWorkbook();
                    }
                    else
                    {
                        throw new Exception("This format is not supported");
                    }

                    //var workbook = new HSSFWorkbook();
                    ReportExcel reportExcel = new ReportExcel();
                    ISheet sheet = workbook.CreateSheet("Data");
                    sheet = reportExcel.standartSheet(sheet);
                    ICellStyle HeaderCellStyle = reportExcel.getJudulStyle1(workbook, TypeExcel);
                    HeaderCellStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
                    HeaderCellStyle.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
                    HeaderCellStyle.FillForegroundColor = IndexedColors.White.Index;
                    int rowExcel = 0;

                    reportExcel.setColoumWidth(sheet, new int[]
                    { 7000, 7000, 7000, 7000, 7000, 7000}, 0);

                    reportExcel.setDataRow(sheet, new object[]
                    { "NoP No", "Rek Name", "Due Date","Interest Days", "Interest Rate", "Currency", "Last Sent", "Status"}, rowExcel++);

                    foreach (var cell in sheet.GetRow(rowExcel - 1).Cells)
                    {
                        cell.CellStyle = HeaderCellStyle;
                    }

                    //foreach (var cell in sheet.GetRow(rowExcel - 1).Cells)
                    //{
                    //    cell.CellStyle = HeaderCellStyle;
                    //    cell.CellStyle.Alignment = HorizontalAlignment.Center;
                    //}

                    foreach (var cell in sheet.GetRow(rowExcel - 1).Cells)
                    {
                        cell.CellStyle = HeaderCellStyle;
                        cell.CellStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
                    }

                    int row = 2;
                    foreach (var vw in data)
                    {
                        reportExcel.setDataRow(sheet, new object[]
                        {
                            vw.NopNo,
                            vw.RekNameAcc,
                            vw.DueDate?.ToString("dd/MM/yyyy"),
                            String.Format(new CultureInfo("id-ID"), "{0:C2}", decimal.Parse(vw.InterestDays == null? "0.00" : vw.InterestDays.ToString())).Replace("Rp","").Replace(",00",""),
                            String.Format(new CultureInfo("id-ID"), "{0:C2}", decimal.Parse(vw.InterestRate == null? "0.00" : vw.InterestRate.ToString())).Replace("Rp","").Replace(",00",""),
                            //vw.InterestDays.ToString(), // Konversi InterestDays menjadi string
                            //vw.InterestRate.ToString(),
                            vw.Cur,
                            vw.LastSentDate?.ToString("dd/MM/yyyy"),
                            vw.Status,
                        }, rowExcel++);
                    }


                    MemoryStream output = new MemoryStream();
                    workbook.Write(output, true);

                    using (FileStream fileStream = new FileStream(updateFile.Path, FileMode.Create, FileAccess.Write))
                    {
                        output.WriteTo(fileStream);
                    }

                    updateFile.StatusDownload = 1;
                    _context.TblDownloadBigFiles.Update(updateFile);
                    _context.SaveChanges();
                }
                else
                {
                    updateFile.StatusDownload = 2;
                    _context.TblDownloadBigFiles.Update(updateFile);
                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                var log = new TblLogErrorPrint();
                log.IdFile = IdFile;
                log.ErrorMessage = ex.Message + " - " + ex.InnerException;
                log.CreatedTime = DateTime.Now;
                await _context.TblLogErrorPrints.AddAsync(log);
                await _context.SaveChangesAsync();
            }
        }

        public async Task PrintNoPPDF(string Tanggal, string namaPegawai, string Scheme, string Host, int IdFile)
        {
            DbDashboardDevaBniContext _context = new DbDashboardDevaBniContext();

            try
            {
                var updateFile = await _context.TblDownloadBigFiles.Where(x => x.Id == IdFile).FirstOrDefaultAsync();

                if (!Directory.Exists(GetConfig.AppSetting["Path"]))
                {
                    Directory.CreateDirectory(GetConfig.AppSetting["Path"]);
                }

                CultureInfo culture = new CultureInfo("id-ID");
                var dateArray = new string[] { };

                if (!string.IsNullOrEmpty(Tanggal))
                {
                    dateArray = Tanggal.Split(" to ");
                }

                var headerToExport = StoredProcedureExecutor.ExecuteSPList<TblNoticeOfPayment>(_context, "sp_Load_NoticeOfPayment_View", new SqlParameter[]
                {
                    new SqlParameter("@DueDateFrom", dateArray.Count() == 0 ? "" : DateTime.Parse(dateArray[0], culture)),
                    new SqlParameter("@DueDateTo", dateArray.Count() == 0 ? "" : DateTime.Parse(dateArray[1], culture)),
                    new SqlParameter("@sortColumn", "id"),
                    new SqlParameter("@sortColumnDir", "DESC"),
                    new SqlParameter("@PageNumber", 1),
                    new SqlParameter("@RowsPage", 10000)
                });

                if (headerToExport != null /*&& finalData.Count <= 10000*/)
                {
                    var locationnow = new Uri($"{Scheme}://{Host}");
                    string imagePath = "wwwroot/lib/images/logoBNI.png";
                    byte[] imageBytes = System.IO.File.ReadAllBytes(imagePath);
                    string base64String = Convert.ToBase64String(imageBytes);

                    ExportToPDFNoP_ViewModel model = new ExportToPDFNoP_ViewModel();
                    model.Domain = locationnow.AbsoluteUri;
                    model.UserLogin = namaPegawai;
                    model.TanggalCetak = DateTime.Now.ToString("dd/MM/yyyy HH:mm", culture);
                    model.Logo = base64String;
                    model.ListData = headerToExport;

                    var engine = new RazorLightEngineBuilder()
                    .UseMemoryCachingProvider()
                    .Build();

                    string templateFilePath = "Views/NoticeOfPayment/_PdfExportNoP.cshtml"; // Replace with your CSHTML file path
                    string template = File.ReadAllText(templateFilePath);
                    string result = string.Empty;

                    try
                    {
                        result = await engine.CompileRenderStringAsync("templateKey", template, model);
                    }
                    catch (Exception ex)
                    {
                        var log = new TblLogErrorPrint();
                        log.IdFile = IdFile;
                        log.ErrorMessage = ex.Message + " - " + ex.InnerException;
                        log.CreatedTime = DateTime.Now;
                        await _context.TblLogErrorPrints.AddAsync(log);
                        await _context.SaveChangesAsync();
                    }
                    var globalSettings = new GlobalSettings
                    {
                        ColorMode = ColorMode.Color,
                        Orientation = Orientation.Landscape,
                        PaperSize = PaperKind.A4,
                        Margins = new MarginSettings { Top = 5, Right = 5, Left = 5, Bottom = 5 },

                    };

                    var objectSettings = new ObjectSettings
                    {
                        PagesCount = true,
                        HtmlContent = result, // Replace this with your HTML content
                        FooterSettings = new FooterSettings
                        {
                            FontSize = 7,
                            FontName = "Helvetica Light",
                            Left = "Hal [page]/[toPage]",
                            Center = "Sudah Divalidasi dan Disahkan Secara Digital",
                            Right = $"https://deva.bni.co.id, {model.UserLogin}, {model.TanggalCetak}"
                        }
                    };

                    var pdf = new HtmlToPdfDocument()
                    {
                        GlobalSettings = globalSettings,
                        Objects = { objectSettings }
                    };

                    //byte[] file = _converter.Convert(pdf);
                    using (var pdfTools = new PdfTools())
                    {
                        var converter = new SynchronizedConverter(pdfTools);
                        byte[] file = converter.Convert(pdf);

                        using (MemoryStream inputMemoryStream = new MemoryStream(file))
                        using (MemoryStream outputMemoryStream = new MemoryStream())
                        {
                            PdfReader pdfReader = new PdfReader(inputMemoryStream);
                            PdfWriter pdfWriter = new PdfWriter(outputMemoryStream);
                            using (PdfDocument pdfDocument = new PdfDocument(pdfReader, pdfWriter))
                            {
                                Document document = new Document(pdfDocument);

                                // Create a watermark element
                                Paragraph watermark = new Paragraph($"{model.UserLogin} - {model.TanggalCetak.Replace(".", ":")} WIB")
                                    .SetFontColor(iText.Kernel.Colors.ColorConstants.LIGHT_GRAY)
                                    .SetFontSize(30)
                                    .SetOpacity(0.3f)
                                    .SetWidth(2000)
                                    .SetRotationAngle(-0.5);

                                // Add the watermark to each page
                                for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
                                {
                                    float centerX = document.GetPdfDocument().GetDefaultPageSize().GetWidth() / 2;
                                    float centerY = document.GetPdfDocument().GetDefaultPageSize().GetHeight() / 2;
                                    document.ShowTextAligned(watermark, 425, 300, i, TextAlignment.CENTER, iText.Layout.Properties.VerticalAlignment.MIDDLE, 45);
                                }

                            }
                            //SAVE FILE
                            File.WriteAllBytes(updateFile.Path, outputMemoryStream.ToArray());
                        }
                        updateFile.StatusDownload = 1;
                        _context.TblDownloadBigFiles.Update(updateFile);
                        _context.SaveChanges();
                    }
                }
                else
                {
                    updateFile.StatusDownload = 2;
                    _context.TblDownloadBigFiles.Update(updateFile);
                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                var log = new TblLogErrorPrint();
                log.IdFile = IdFile;
                log.ErrorMessage = ex.Message + " - " + ex.InnerException;
                log.CreatedTime = DateTime.Now;
                await _context.TblLogErrorPrints.AddAsync(log);
                await _context.SaveChangesAsync();
            }
        }
    }
}
