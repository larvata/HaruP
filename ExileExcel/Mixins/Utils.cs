﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ExileExcel.Attribute;
using ExileExcel.Common;

namespace ExileExcel.Mixins
{
    internal static class Utils
    {
        /// <summary>
        /// Aquire HeaderText--Property mapping 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="headerList"></param>
        /// <returns></returns>
        internal static ExileDocumentMeta GetTypeMatched<T>(Dictionary<int, string> headerList)
        {
            var retVal = GetTypeMatched<T>();
            var currentKeyPair = new Dictionary<string, string>();

            var plist = currentKeyPair.Where(p => !headerList.ContainsValue(p.Value));
            // all matched
            if (!plist.Any())
            {
                return retVal;
            }

            return null;
        }

        /// <summary>
        /// Aquire HeaderText--Property mapping 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        internal static ExileDocumentMeta GetTypeMatched<T>()
        {
            var retVal = new ExileDocumentMeta();
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.GetCustomAttributes(typeof(ExileHeaderGeneralAttribute), true).Any());

            //if (!properties.Any()) throw new Exception("MATCHING TYPE WITHOUT ATTRIBUTE ExilePropertyAttribute");

            var sheetTitleAttr =
               typeof(T).GetCustomAttributes(typeof(ExileSheetTitleAttribute), true).FirstOrDefault() as ExileSheetTitleAttribute;

            retVal.MatchedType = typeof(T);

            if (sheetTitleAttr != null)
            {
                retVal.TitleFontHeight = sheetTitleAttr.FontHeight;
                retVal.TitleRowHeight = sheetTitleAttr.RowHeight;
                retVal.HideHeader = sheetTitleAttr.HideHeader;
            }

            var sheetDataArea =
                typeof(T).GetCustomAttributes(typeof(ExileSheetDataArea), true).FirstOrDefault() as ExileSheetDataArea;
            if (sheetDataArea != null)
            {
                retVal.StartRowNum = sheetDataArea.StartRowNum;
                retVal.StartColumnNum = sheetDataArea.StartColumnNum;
            }

            foreach (var property in properties)
            {


                var colDataFormatAttr =
                    property.GetCustomAttributes(typeof(ExileColumnDataFormatAttribute), true).FirstOrDefault() as ExileColumnDataFormatAttribute;

                var colDimAttr
                    = property.GetCustomAttributes(typeof(ExileColumnDimensionAttribute), true).FirstOrDefault() as ExileColumnDimensionAttribute;

                var headerGeneralAttr
                    = property.GetCustomAttributes(typeof(ExileHeaderGeneralAttribute), true).FirstOrDefault() as ExileHeaderGeneralAttribute;

                var headerMeta = new ExileHeaderMeta { PropertyName = property.Name };
                if (colDataFormatAttr != null)
                {
                    headerMeta.BuiltinFormat = colDataFormatAttr.ColumnBulitinDataFormat;
                    headerMeta.CustomDataFormat = colDataFormatAttr.ColumnCustomDataFormat;
                    headerMeta.ColumnType = colDataFormatAttr.ColumnType;
                }
                if (headerGeneralAttr != null)
                {
                    headerMeta.DisplaySequence = headerGeneralAttr.HeaderSequence;
                    headerMeta.PropertyDescription = headerGeneralAttr.HeaderText;
                }
                if (colDimAttr != null)
                {
                    headerMeta.Width = colDimAttr.ColumnWidth;
                    headerMeta.Height = colDimAttr.RowHeight;
                    headerMeta.AutoFit = colDimAttr.AutoFit;
                }

                retVal.Headers.Add(headerMeta);
            }


            return retVal;
        }

        /// <summary>
        /// get attribute value by attribute name
        /// </summary>
        /// <param name="src"></param>
        /// <param name="propName"></param>
        /// <returns></returns>
        public static System.Attribute GetTypeAttribute(Type src, string propName)
        {
            return src.GetProperty(propName).GetCustomAttributes(typeof(System.Attribute), true).First() as System.Attribute;
        }

        public static object GetPropValue(object src, string propName)
        {
            return src.GetType().GetProperty(propName).GetValue(src, null);
        }

        public static Type GetPropType(object src, string propName)
        {
            return src.GetType().GetProperty(propName).PropertyType;
        }

        public static IEnumerable<Type> GetTypesByAttribute(Type attributeType)
        {
            foreach (var type in AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()))
            {
                if (type.GetCustomAttributes(attributeType, true).Length > 0)
                {
                    yield return type;
                }
            }
        }

        public static bool CheckIfAnonymousType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            // HACK: The only way to detect anonymous types right now.
            return System.Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
                && type.IsGenericType && type.Name.Contains("AnonymousType")
                && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
                && (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
        }




    }
}
