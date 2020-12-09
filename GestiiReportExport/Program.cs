using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Contpaqi.Sdk;
using Contpaqi.Sdk.Extras.Ayudantes;

namespace Gestii
{
    class Program
    {
        public static Logger logger;

        static void Main(string[] args)
        {
            var parser = new Parser(with => with.EnableDashDash = true);
            parser.ParseArguments<Options>(args)
                .WithParsed<Options>(opts => RunOptionsAndReturnExitCode(opts))
                .WithNotParsed<Options>((errs) => HandleParseError(errs));
        }

        private static void HandleParseError(IEnumerable<Error> errs)
        {
            foreach (var e in errs)
            {
                Program.logger.Error(e);
            }            
        }

        private static int RunOptionsAndReturnExitCode(Options opts)
        {                        

            Properties.Settings.Default.client = opts.Client ?? Properties.Settings.Default.client;
            Properties.Settings.Default.apikey = opts.ApiKey ?? Properties.Settings.Default.apikey;
            Properties.Settings.Default.directory = opts.Directory ?? Properties.Settings.Default.directory;

            if (Properties.Settings.Default.client == "")
            {
                Console.WriteLine("set client:");
                Properties.Settings.Default.client = Console.ReadLine();
            }

            if (Properties.Settings.Default.apikey == "")
            {
                Console.WriteLine("set apikey:");
                Properties.Settings.Default.apikey = Console.ReadLine();
            }

            if (Properties.Settings.Default.empresa == "")
            {
                Console.WriteLine("set contpaq Empresa:");
                Properties.Settings.Default.empresa = Console.ReadLine();
            }

            if (Properties.Settings.Default.user == "")
            {
                Console.WriteLine("set contpaq user:");
                Properties.Settings.Default.user = Console.ReadLine();
            }

            if (Properties.Settings.Default.password == "")
            {
                Console.WriteLine("set contpaq password:");
                Properties.Settings.Default.password = Console.ReadLine();
            }

            if (Properties.Settings.Default.directory == "")
            {
                Properties.Settings.Default.directory = AppDomain.CurrentDomain.BaseDirectory;
            }

            int lastId = opts.LastId > 0 ? opts.LastId : Properties.Settings.Default.lastid;

            logger = new Logger("GestiiExport", Logger.Targets.File | Logger.Targets.Console);
            logger.Start();
            //inicializa sdk de contpaq y abre la empresa
            ContpaqClient.InitializeSDK();

            /*GestiiVisit visit = new GestiiVisit
            {
                Id = "test",
                Folio = "MM1709061",
                Usuario = "MMJAR",
                Fecha = "13/06/2018 14:06",
                Grupo = "MONTERREY/ANALISTA1",
                Calle_1 = "Jose Rodríguez",
                Num_ext_1 = "1100",
                Colonia_1 = "Universidad",
                Municipio_1 = "Saltillo",
                Ciudad_1 = "Saltillo",
                Estado_1 = "Coahuila",
                Cp_1 = "25260",
                Telefono = "14881889",
                Email = "ejemplo@gmail.com",
                Dia = "J",
                Etiqueta = "GE"
            };
            ContpaqClient.UpdateClientInfo(visit);
            */
            
            //Descarga archivos de Gestii 
            lastId = GestiiClient.DownloadReports(lastId);

            //Procesa archivos decargados            
            FileWorker.ProcessFiles();
            
            ContpaqClient.FinalizeSDK();

            if (opts.Save)
            {
                Properties.Settings.Default.lastid = lastId;
                Properties.Settings.Default.Save();
            }

            logger.Stop();            

            return 0;
        }

        
    }
}
