using Contpaqi.Sdk;
using Contpaqi.Sdk.Extras.Ayudantes;
using Contpaqi.Sdk.Extras.Repositorios;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Text;
using Contpaqi.Sdk.Extras;
using Contpaqi.Sdk.Extras.Modelos;
using System.Collections.Generic;

namespace Gestii
{
    public struct Pagare
    {
        public string aCodConcepto;
        public string aSerie;
        public double aFolio;
        public double aImporte;
    }

    internal class ContpaqClient
    {
        public static int InitializeSDK()
        {
            int r = 0;
            Program.logger.Info("Inicializando Contpaq");
            if (Properties.Settings.Default.empresa != "")
            {

                r = InicializacionComercialSdk.InicializarSDK(Properties.Settings.Default.user, Properties.Settings.Default.password);
                PrintError(r, "Inicializando Comercial SDk");

                Program.logger.Info("Abriendo empresa {0}", Properties.Settings.Default.empresa);
                ComercialSdk.fAbreEmpresa(Properties.Settings.Default.empresa);                
                PrintError(r, "abriendo empresa");                
            } else
            {
                Program.logger.Info("No hay empresa configurada");
                r = -1;
            }

            return r;
        }

        public static void FinalizeSDK()
        {
            if (Properties.Settings.Default.empresa != "")
            {

                Program.logger.Info("Cerrando empresa");
                ComercialSdk.fCierraEmpresa();

                Program.logger.Info("Termina SDK");
                ComercialSdk.fTerminaSDK();            
            }
        }

        public static string InsertPayment(string Client, string Concepto, double amount, string Agent, string Referencia, string Fecha)
        {
            DateTime myDate = DateTime.ParseExact(Fecha, "MM/dd/yyyy", CultureInfo.InvariantCulture);
            Program.logger.Info("insertando abono de cliente {0}", Client);

            if (PaymentExists(myDate, Concepto, Client, Referencia))
            {
                Program.logger.Info("Ignorado por que ya existe");
                return "";
            }

            StringBuilder aSerie = new StringBuilder(11);
            double aFolio = 0;
            ComercialSdk.fSiguienteFolio(Concepto, aSerie, ref aFolio);
            tDocumento aDocumento = new tDocumento
            {
                aCodigoCteProv = Client,
                aImporte = amount,
                aCodConcepto = Concepto,
                aCodigoAgente = Agent,
                aReferencia = Referencia,
                aFecha = Fecha,
                aSerie = aSerie.ToString(),
                aFolio = aFolio
            };

            int r = ComercialSdk.fAltaDocumentoCargoAbono(ref aDocumento);
            if (!PrintError(r, "Alta de documento ", out string error))
            {
                tLlaveDoc aDoctoPago = new tLlaveDoc() { aCodConcepto = aDocumento.aCodConcepto, aFolio = aDocumento.aFolio, aSerie = aDocumento.aSerie };

                List<Pagare> pagares = GetPagares(myDate.AddMonths(-48), myDate.AddMonths(3), Client, amount);

                foreach (Pagare pagare in pagares)
                {
                    tLlaveDoc aDoctoaPagar = new tLlaveDoc
                    {
                        aCodConcepto = pagare.aCodConcepto,
                        aFolio = pagare.aFolio,
                        aSerie = pagare.aSerie
                    };

                    Program.logger.Info("saldando {0} con {1}", aDoctoaPagar.aFolio, pagare.aImporte);
                    r = ComercialSdk.fSaldarDocumento_Param(
                        aDoctoaPagar.aCodConcepto, aDoctoaPagar.aSerie, aDoctoaPagar.aFolio,
                        aDoctoPago.aCodConcepto, aDoctoPago.aSerie, aDoctoPago.aFolio,
                        pagare.aImporte, 1, Fecha
                    );
                    if (PrintError(r, "saldando documento", out error))
                    {
                        break;
                    }
                }
            }

            return error;
        }

        private static List<Pagare> GetPagares(DateTime from, DateTime to, string Client, double amount)
        {
            List<Pagare> lista = new List<Pagare>();
            int r = ComercialSdk.fSetFiltroDocumento(from.ToString("MM/dd/yyyy"), to.ToString("MM/dd/yyyy"), "16", Client);
            if (r == 0)
            {
                if (!PrintError(r, "filtrar documentos", out string error))
                {
                    r =  ComercialSdk.fPosPrimerDocumento();
                    while (r == 0 && ComercialSdk.fPosEOF() == 0 && amount > 0)
                    {
                        double pendiente = GetField<double>("CPENDIENTE");
                        if (pendiente > 0)
                        {
                            Pagare pagare = new Pagare
                            {
                                aCodConcepto = "16",
                                aFolio = GetField<double>("CFOLIO"),
                                aSerie = GetField("CSERIEDOCUMENTO"),
                                aImporte = pendiente
                            };

                            if (pendiente > amount)
                            {
                                pagare.aImporte = amount;
                            }

                            amount -= pagare.aImporte;
                            lista.Add(pagare);
                        }

                        r = ComercialSdk.fPosSiguienteDocumento();
                    }
                }
            }

            ComercialSdk.fCancelaFiltroDocumento();
            Program.logger.Info("Se filtraron {0} pagares", lista.Count);
            return lista;
        }

        private static bool PaymentExists(DateTime date, string Concepto, string client, string Referencia)
        {
            bool found = false;

            int r = ComercialSdk.fSetFiltroDocumento(date.ToString("MM/dd/yyyy"), date.ToString("MM/dd/yyyy"), Concepto, client);
            if (r == 0)
            {
                r = ComercialSdk.fPosPrimerDocumento();
                while (!found && r == 0 && ComercialSdk.fPosEOF() == 0)
                {
                    found = Referencia  == GetField<string>("CREFERENCIA");
                    r = ComercialSdk.fPosSiguienteDocumento();
                }
            }

            ComercialSdk.fCancelaFiltroDocumento();
            return found;
        }

        private static T GetField<T>(string field)
        {
            StringBuilder aValor = new StringBuilder(512);
            int r = ComercialSdk.fLeeDatoDocumento(field, aValor, 512);
            if (! PrintError(r, "leyendo campo "+field))             
            {
                return (T)Convert.ChangeType(aValor.ToString(), typeof(T));
            }

            return default(T);
        }

        internal static void SetDatoCliente(string llave, string valor)
        {
            
            if (! String.IsNullOrEmpty(valor))
            {
                Program.logger.Info($"{llave} => '{valor}'");
                ComercialSdk.fSetDatoCteProv(llave, valor);
            }
        }

        internal static void SetDatoDom(string llave, string valor)
        {            
            if (!String.IsNullOrEmpty(valor))
            {
                Program.logger.Info($"{llave} => '{valor}'");
                ComercialSdk.fSetDatoDireccion(llave, valor);
            }
        }

        internal static void UpdateClientInfo(GestiiVisit visit)
        {
            try
            {
                int r = ComercialSdk.fBuscaCteProv(visit.Folio);
                if (!PrintError(r, "buscando cliente"))
                {
                    Program.logger.Info($"Etiqueta {visit.Etiqueta}");
                    string valor = visit.Etiqueta == "GE" ? "99" : "28";
                    
                    ComercialSdk.fEditaCteProv();                    
                    SetDatoCliente("CDENCOMERCIAL", visit.Dia);                    
                    SetDatoCliente("CIDVALORCLASIFCLIENTE4", valor); 
                    ComercialSdk.fGuardaCteProv();
                    
                    r = ComercialSdk.fBuscaDireccionCteProv(visit.Folio, (byte)0);
                    if (!PrintError(r, "buscando direccion"))
                    {
                        ComercialSdk.fEditaDireccion();                        
                        SetDatoDom("CNOMBRECALLE", visit.Calle_1);
                        SetDatoDom("CNUMEROEXTERIOR", visit.Num_ext_1);                        
                        SetDatoDom("CCOLONIA", visit.Colonia_1);
                        SetDatoDom("CCODIGOPOSTAL", visit.Cp_1);
                        SetDatoDom("CMUNICIPIO", visit.Municipio_1);
                        SetDatoDom("CCIUDAD", visit.Ciudad_1);
                        SetDatoDom("CESTADO", visit.Estado_1);
                        SetDatoDom("CTELEFONO1", visit.Telefono);
                        SetDatoDom("CEMAIL", visit.Email);                        
                        r = ComercialSdk.fGuardaDireccion();                        
                        PrintError(r, "guarda direccion");                        
                    }
                }
            }
            catch (Exception ex)
            {
                Program.logger.Error(ex.ToString());
            }

        }

        private static string GetField(string field)
        {
            return GetField<string>(field);
        }

        private static bool PrintError(int r, string when)
        {
            if (r != 0)
            {
                Program.logger.Error("error {0}", when);
                StringBuilder aMensaje = new StringBuilder(512);
                ComercialSdk.fError(r, aMensaje, 512);
                Program.logger.Error(aMensaje);
            }

            return r != 0;
        }

        private static bool PrintError(int r, string when, out string error)
        {
            error = "";
            if (r != 0)
            {
                Program.logger.Error("error {0}", when);
                StringBuilder aMensaje = new StringBuilder(512);
                ComercialSdk.fError(r, aMensaje, 512);
                Program.logger.Error(aMensaje);
                error = aMensaje.ToString();
            }

            return r != 0;
        }
    }
}