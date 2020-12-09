using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gestii
{
    public class GestiiVisit
    {
        public string Id { get; set; }
        public string Folio { get; set; }
        public string Estado { get; set; }
        public string Municipio { get; set; }
        public string Cp { get; set; }
        public string Colonia { get; set; }
        public string Calle { get; set; }        
        public string Resultado { get; set; }
        public string Cuestionario { get; set; }
        public double Abono { get; set; }
        public string Usuario { get; set; }
        public string Fecha { get; set; }
        public string Grupo { get; set; }
        public string Calle_1 { get; set; }
        public string Num_ext_1 { get; set; }
        public string Num_int_1 { get; set; }
        public string Colonia_1 { get; set; }
        public string Municipio_1 { get; set; }
        public string Ciudad_1 { get; set; }
        public string Estado_1 { get; set; }
        public string Cp_1 { get; set; }        
        public string Telefono { get; internal set; }
        public string Email { get; internal set; }
        public string Dia { get; internal set; }
        public string Etiqueta { get; internal set; }
        public string Error { get; set; }

        public bool AddressChanged()
        {
            Calle.StartsWith(Calle_1);

            string calle = Calle_1;
            if (Num_ext_1 != "") calle = calle + " " + Num_ext_1;
            if (Num_int_1 != "") calle = calle + " " + Num_int_1;

            return Estado != Estado_1 && Estado_1 != "" &&
                Cp != Cp_1 && Cp_1 != "" &&
                Municipio != Municipio_1 && Municipio_1 != "" && 
                Colonia != Colonia_1 && Colonia_1 != "" &&
                Calle != calle && calle != "" &&
                Telefono != "" && Email != "" && 
                Dia != "" && Etiqueta != "";
        }
    }

    public sealed class GestiiVisitMap: ClassMap<GestiiVisit>
    {
        public GestiiVisitMap()
        {
            Map(m => m.Id).Name("id");
            Map(m => m.Folio).Name("Folio");
            Map(m => m.Estado).Name("Estado");
            Map(m => m.Municipio).Name("Municipio");
            Map(m => m.Cp).Name("CP");
            Map(m => m.Colonia).Name("Colonia");
            Map(m => m.Calle).Name("Calle");
            Map(m => m.Resultado).Name("Resultado");
            Map(m => m.Cuestionario).Name("Cuestionario");
            Map(m => m.Abono).ConvertUsing(row =>
            {
                double abono = 0;
                var v = row.GetField<string>("abono");
                if (v != "")
                {
                    Double.TryParse(v, out abono);
                }

                return abono;
            });

            Map(m => m.Usuario).Name("Usuario");
            Map(m => m.Fecha).Name("Fecha Fin");
            Map(m => m.Grupo).Name("Grupo");
            Map(m => m.Resultado).Name("resultado");
            Map(m => m.Calle_1).Name("calle_1");
            Map(m => m.Num_ext_1).Name("num_ext_1");            
            Map(m => m.Colonia_1).Name("colonia_1");
            Map(m => m.Municipio_1).Name("municipio_1");
            Map(m => m.Ciudad_1).Name("ciudad_1");
            Map(m => m.Estado_1).Name("estado_1");
            Map(m => m.Cp_1).Name("cp_1");
            Map(m => m.Telefono).Name("telefono");
            Map(m => m.Email).Name("correo");
            Map(m => m.Dia).Name("dia_de_pago");
            Map(m => m.Etiqueta).Name("etiqueta");
        }
    }
}
