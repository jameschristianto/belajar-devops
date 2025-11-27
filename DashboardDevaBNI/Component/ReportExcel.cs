using NPOI.HSSF.UserModel;
using NPOI.HSSF.Util;
using NPOI.SS.UserModel;

namespace DashboardDevaBNI.Component
{
    public class ReportExcel
    {
        public ISheet standartSheet(ISheet sheet)
        {
            sheet.DefaultColumnWidth = 15;
            sheet.DisplayGridlines = true;
            sheet.DisplayRowColHeadings = true;

            /*
            IWorkbook workbook=sheet.Workbook;
            IFont HeaderFont = workbook.CreateFont();
            HeaderFont.FontName = "Calibri";
            HeaderFont.FontHeightInPoints = 12;
            ICellStyle HeaderCellStyle = workbook.CreateCellStyle();
            HeaderCellStyle.SetFont(HeaderFont);
            sheet.SetDefaultColumnStyle(0, HeaderCellStyle);
            */

            return sheet;
        }

        public ICellStyle getHeaderStyle1(IWorkbook workbook, string TypeExcel)
        {
            IFont HeaderFont = workbook.CreateFont();
            HeaderFont.FontHeightInPoints = 12;
            HeaderFont.FontName = "Calibri";
            ICellStyle HeaderCellStyle = workbook.CreateCellStyle();
            if (TypeExcel == "xlsx")
            {
                HeaderFont.Color = IndexedColors.Black.Index;
                HeaderCellStyle.FillForegroundColor = IndexedColors.LightOrange.Index;// HSSFColor.TEAL.index;
            }
            else if (TypeExcel == "xls")
            {
                HeaderFont.Color = HSSFColor.Black.Index;
                HeaderCellStyle.FillForegroundColor = HSSFColor.LightOrange.Index;// HSSFColor.TEAL.index;
            }

            HeaderCellStyle.FillPattern = FillPattern.SolidForeground;
            HeaderCellStyle.SetFont(HeaderFont);
            return HeaderCellStyle;
        }

        public ICellStyle getJudulStyle1(IWorkbook workbook, string TypeExcel)
        {
            IFont HeaderFont = workbook.CreateFont();
            HeaderFont.FontHeightInPoints = 12;
            HeaderFont.FontName = "Calibri";
            HeaderFont.Boldweight = (short)NPOI.SS.UserModel.FontBoldWeight.Bold;
            if (TypeExcel == "xlsx")
            {
                HeaderFont.Color = IndexedColors.Black.Index;
            }
            else if (TypeExcel == "xls")
            {
                HeaderFont.Color = HSSFColor.Black.Index;
            }

            ICellStyle HeaderCellStyle = workbook.CreateCellStyle();
            HeaderCellStyle.SetFont(HeaderFont);
            return HeaderCellStyle;
        }

        public ICellStyle GenerateDateStyle(IWorkbook workbook)
        {
            IFont HeaderFont = workbook.CreateFont();
            HeaderFont.FontHeightInPoints = 8;
            HeaderFont.FontName = "Calibri";
            HeaderFont.Boldweight = (short)NPOI.SS.UserModel.FontBoldWeight.None;
            HeaderFont.Color = HSSFColor.Grey80Percent.Index;
            ICellStyle HeaderCellStyle = workbook.CreateCellStyle();
            HeaderCellStyle.SetFont(HeaderFont);
            return HeaderCellStyle;
        }

        public ISheet setColoumWidth(ISheet sheet, int[] width, int startColoum)
        {

            foreach (int colwidth in width)
            {
                sheet.SetColumnWidth(startColoum, colwidth);
                startColoum++;
            }
            return sheet;
        }

        public ISheet setDataRow(ISheet sheet, object[] ArrObj, int indexRow, int indexCol)
        {
            var row = sheet.CreateRow(indexRow);

            foreach (object obj in ArrObj)
            {
                if (obj is byte)
                {
                    row.CreateCell(indexCol).SetCellValue((byte)obj);
                }
                if (obj is DateTime)
                {
                    row.CreateCell(indexCol).SetCellValue((DateTime)obj);
                }
                if (obj is System.DateTime)
                {
                    if (obj != null)
                        row.CreateCell(indexCol).SetCellValue((System.DateTime)obj);
                }
                if (obj is int)
                {
                    row.CreateCell(indexCol).SetCellValue((int)obj);
                }
                if (obj is double)
                {
                    row.CreateCell(indexCol).SetCellValue((double)obj);
                }

                if (obj is bool)
                {
                    row.CreateCell(indexCol).SetCellValue((bool)obj);
                }
                if (obj is string)
                {
                    row.CreateCell(indexCol).SetCellValue((string)obj);
                }
                indexCol++;
            }
            return sheet;
        }

        public ISheet setDataRow(ISheet sheet, object[] ArrObj, int indexRow)
        {
            return setDataRow(sheet, ArrObj, indexRow, 0);
        }

        public ICellStyle getHeaderStyle1(HSSFWorkbook workbook)
        {
            IFont HeaderFont = workbook.CreateFont();
            HeaderFont.FontHeightInPoints = 12;
            HeaderFont.Color = IndexedColors.Black.Index;
            HeaderFont.FontName = "Calibri";
            ICellStyle HeaderCellStyle = workbook.CreateCellStyle();
            HeaderCellStyle.FillForegroundColor = IndexedColors.Coral.Index;// HSSFColor.TEAL.index;
                                                                            //HeaderCellStyle.FillPattern = FillPatternType.SOLID_FOREGROUND;
            HeaderCellStyle.SetFont(HeaderFont);
            return HeaderCellStyle;
        }
    }
}
