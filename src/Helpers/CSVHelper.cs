/// Name: CSVHelper.cs
/// Description:
/// Authors: Multiple.
/// Copyright: Garry Sotnik

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace SOSIEL.Helpers
{
    class EnumerableConverter<T> : DefaultTypeConverter
    {
        public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        {
            IEnumerable<T> values = value as IEnumerable<T>;

            return string.Join(CultureInfo.CurrentCulture.TextInfo.ListSeparator, values);
        }
    }

    /// <summary>
    /// Helper class for working with CSV files.
    /// </summary>
    public static class CSVHelper
    {
        private static CsvHelper.Configuration.CsvConfiguration configuration;

        static CSVHelper()
        {
            configuration = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture);
            configuration.HasHeaderRecord = false;
            configuration.TypeConverterCache.AddConverter<string[]>(new EnumerableConverter<string>());
        }

        /// <summary>
        /// Reads all records in file.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath">The file path.</param>
        /// <param name="headerRecortExist">if set to <c>true</c> [header recort exist].</param>
        /// <param name="mappingClass">The mapping class.</param>
        /// <returns></returns>
        public static List<T> ReadAllRecords<T>(string filePath, bool headerRecortExist = false, Type mappingClass = null)
        {
            if (mappingClass != null)
            {
                configuration.RegisterClassMap(mappingClass);
            }

            using (FileStream fs = File.Open(filePath, FileMode.Open))
            using (StreamReader sr = new StreamReader(fs))
            using (CsvReader csv = new CsvReader(sr, configuration))
            {
                csv.Read();

                var records = csv.GetRecords<T>();
                return records.ToList();
            }
        }

        /// <summary>
        /// Appends record to the file end or creates new file and writes record there.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <param name="record"></param>
        public static void AppendTo<T>(string filePath, T record)
        {
            var isFileExist = File.Exists(filePath);

            using (FileStream fs = File.Open(filePath, isFileExist ? FileMode.Append : FileMode.Create))
            using (StreamWriter sw = new StreamWriter(fs))
            using (CsvWriter csv = new CsvWriter(sw, configuration))
            {
                if (!isFileExist)
                {
                    configuration.HasHeaderRecord = true;
                    csv.WriteHeader<T>();
                    csv.NextRecord();
                    configuration.HasHeaderRecord = false;
                }

                csv.WriteRecord(record);
                csv.NextRecord();
            }
        }


        /// <summary>
        /// Appends records to the file end or creates new file and writes records there.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <param name="records"></param>
        public static void AppendTo<T>(string filePath, IEnumerable<T> records)
        {
            var isFileExist = File.Exists(filePath);

            using (FileStream fs = File.Open(filePath, isFileExist ? FileMode.Append : FileMode.Create))
            using (StreamWriter sw = new StreamWriter(fs))
            using (CsvWriter csv = new CsvWriter(sw, configuration))
            {
                //it writes header by default
                if (!isFileExist)
                {
                    configuration.HasHeaderRecord = true;
                }
                else
                {
                    configuration.HasHeaderRecord = false;
                }

                csv.WriteRecords(records);
            }
        }


    }
}
