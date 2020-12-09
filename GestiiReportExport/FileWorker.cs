using CsvHelper;
using CsvHelper.Configuration;
using Ionic.Zip;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Gestii
{
    internal class FileWorker
    {
        public static void ProcessFiles()
        {
            DirectoryInfo baseDir = Directory.CreateDirectory(Properties.Settings.Default.directory);
            string downloaded = baseDir.CreateSubdirectory("downloaded").FullName;
            string correct = baseDir.CreateSubdirectory("processed").FullName;
            string incorrect = baseDir.CreateSubdirectory("errors").FullName;

            string[] files = Directory.GetFiles(downloaded, "auto_*_cobranza_*.zip", SearchOption.TopDirectoryOnly);

            foreach (var file in files)
            {
                Program.logger.Info("Procesando archivo {0}", file);                
                List<GestiiVisit> errors = OpenReportAndReturnErrors(file);
                if (errors.Count > 0)
                {                                    
                    string ErrorsCsvPath = incorrect + "\\" + Path.ChangeExtension(Path.GetFileName(file), "csv");
                    SaveErrorsCsv(errors, ErrorsCsvPath);                    
                }
                MoveFile(file, correct);
            }     
        }

        private static void SaveErrorsCsv(List<GestiiVisit> errors, string errorsCsvPath)
        {
            Program.logger.Info("escribiendo archivo de errores");
            File.Delete(errorsCsvPath);
            using (TextWriter writer = new StreamWriter(errorsCsvPath))
            {
                var csv = new CsvWriter(writer);                
                csv.WriteRecords(errors);
            }
        }

        private static void MoveFile(string file, string directory)
        {
            string destination = directory + "\\" + Path.GetFileName(file);
            File.Delete(destination);
            File.Move(file, destination);

        }

        private static List<GestiiVisit> OpenReportAndReturnErrors(string file)
        {
            List<GestiiVisit> errors = new List<GestiiVisit>();
            Configuration conf = GetCsvConfiguration();

            try
            {                
                using (ZipFile zip = ZipFile.Read(file))
                foreach (ZipEntry e in zip)
                {
                    using (MemoryStream stream = new MemoryStream())
                    {
                        e.Extract(stream);
                        stream.Seek(0, SeekOrigin.Begin);

                        using (StreamReader reader = new StreamReader(stream, Encoding.GetEncoding("windows-1250")))
                        using (CsvReader csv = new CsvReader(reader, conf, false))
                        {
                            IEnumerable<GestiiVisit> records = csv.GetRecords<GestiiVisit>();
                            InsertPayments(records, errors);
                        }
                    }
                }
            }
            catch (IOException e)
            {
                Program.logger.Info("Exception opening report {0}: {1}", file, e.Message);                
            }

            return errors;
        }

        private static Configuration GetCsvConfiguration()
        {
            Configuration CsvConfiguration = new Configuration
            {
                MissingFieldFound = null,
                HeaderValidated = (isValid, headerNames, headerNameIndex, context) =>
                {
                    if (!isValid)
                    {
                        Program.logger.Info($"Header matching ['{string.Join("', '", headerNames)}'] names at index {headerNameIndex} was not found.");
                    }
                },
                ReadingExceptionOccurred = (ex) =>
                {
                    Program.logger.Error(ex.Message);
                }
            };
            CsvConfiguration.RegisterClassMap<GestiiVisitMap>();

            return CsvConfiguration;
        }

        private static bool InsertPayments(IEnumerable<GestiiVisit> records, List<GestiiVisit> errors)
        {
            bool noerrors = true;

            foreach (GestiiVisit record in records)
            {                

                if (record.Cuestionario == "Cobranza")
                {
                    if (record.Resultado == "Cobranza")
                    {
                        string concepto = record.Grupo.StartsWith("MONTERREY") ? "102" : "112";
                        string Fecha = record.Fecha.Substring(3, 2) + "/" + record.Fecha.Substring(0, 2) + "/" + record.Fecha.Substring(6, 4);

                        record.Error = ContpaqClient.InsertPayment(record.Folio, concepto, record.Abono, record.Usuario, $"Gestii {record.Id}", Fecha);
                        if (!String.IsNullOrEmpty(record.Error))
                        {
                            errors.Add(record);
                            noerrors = false;
                        }
                    }

                    
                    ContpaqClient.UpdateClientInfo(record);
                }
            }

            return noerrors;
        }


    }
}