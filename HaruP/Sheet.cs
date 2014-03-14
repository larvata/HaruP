﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HaruP.Common;
using HaruP.Mixins;
using NPOI.SS.UserModel;

namespace HaruP
{
    public class Sheet:Excel
    {

        public Sheet(ISheet sheet)
        {
            this.sheet = sheet;
            this.sheetMeta = new SheetMeta();
        }

        public Sheet PutData(object data)
        {
            // get tags from template
            ParseTemplateMeta();

            // determine type of [object data]
            if (data is IEnumerable)
            {
                // fill data
                var index = 0;
                foreach (var d in (data as IEnumerable))
                {
                    WriteSingle(d, index);
                    index++;
                }
            }
            else
            {
                // fill data
                WriteSingle(data, 0);
            }

            // reset namespace
            this.sheetMeta.Namespace = string.Empty;

            return this;
        }

        public Sheet PutData(object data, SheetMeta meta)
        {
            this.sheetMeta = meta ?? this.sheetMeta;
            // fill data
            return PutData(data);
        }

        private void ParseTemplateMeta()
        {

            for (var rowNum = 0; rowNum <= sheet.LastRowNum; rowNum++)
            {
                var row = sheet.GetRow(rowNum);

                // each cell in this row was empty 
                if (row == null) continue;

                for (var cellNum = 0; cellNum <= row.LastCellNum; cellNum++)
                {
                    var cell = row.GetCell(cellNum);

                    if (cell == null || cell.CellType != CellType.String) continue;

                    var strVal = cell.StringCellValue;

                    var tagId = Regex.Match(strVal, @"#{([\w\.]+)}").Groups[1].Value;

                    if (string.IsNullOrEmpty(tagId)) continue;

                    sheetMeta.Tags.Add(new TagMeta
                    {
                        Cell = cell,
                        TagId = tagId,
                        TemplateText = strVal
                    });

                }
            }
        }

        private void WriteSingle(Object singleData, int offset)
        {

            var offsetRow = sheetMeta.Orientation == Orientation.Horizontal ? offset : 0;
            var offsetColumn = sheetMeta.Orientation == Orientation.Vertical ? offset : 0;

            var isFirst = true;
            var matchedTags = string.IsNullOrEmpty(sheetMeta.Namespace)
                ? sheetMeta.Tags.Where(t => !t.TagId.Contains(".")).ToList()
                : sheetMeta.Tags.Where(t => t.TagId.StartsWith(sheetMeta.Namespace)).ToList();


            foreach (var t in matchedTags)
            {
                object cellValue;

                try
                {
                    cellValue = Utils.GetPropValue(singleData, t.TagId.Remove(0, sheetMeta.Namespace.Length));
                }
                catch (Exception)
                {
                    // property not in current data object 
                    continue;
                }

                var row = isFirst && (offsetRow > 0)
                    //                    ? sheet.GetRow(t.Cell.RowIndex).CopyRowTo(t.Cell.RowIndex + offsetRow).CellFormulaShift(1)
                    ? sheet.GetRow(t.Cell.RowIndex)
                        .CopyRowToAdvance(t.Cell.RowIndex + offsetRow, sheetMeta.RowHeight)
                        .CellFormulaShift(1)
                    : sheet.GetRow(t.Cell.RowIndex + offsetRow);



                var cell = isFirst && (offsetColumn > 0)
                    ? row.GetCell(t.Cell.ColumnIndex).CopyCellTo(t.Cell.ColumnIndex + offsetColumn)
                    : row.GetCell(t.Cell.ColumnIndex + offsetColumn);

                isFirst = false;

                // fill value
                if (cellValue == null)
                {
                    cell.SetCellValue(string.Empty);
                }
                else if (cellValue is DateTime)
                {
                    cell.SetCellValue((DateTime)cellValue);
                }
                else if (cellValue is double || cellValue is float || cellValue is int)
                {
                    cell.SetCellValue(Convert.ToDouble(cellValue));
                }
                else
                {
                    cell.SetCellValue(cellValue.ToString());
                }

                // copy cell format to new created
                cell.CellStyle = t.Cell.CellStyle;

                sheet.SetColumnWidth(cell.ColumnIndex, sheet.GetColumnWidth(t.Cell.ColumnIndex));
            }
        }

    }
}