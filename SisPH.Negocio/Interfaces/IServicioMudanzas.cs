using SisPH.Entidades.Entidades;
using SisPH.Entidades.Enums;
using SisPH.Negocio.Modelos;

namespace SisPH.Negocio.Interfaces;

public interface IServicioMudanzas
{
    Task<List<Mudanza>> ListarAsync(int? unidadId = null);
    Task<ResultadoOperacion> RegistrarMudanzaAsync(
        int unidadId,
        TipoMudanza tipo,
        int? residenteIdEntrada,
        TipoOcupacion tipoOcupacionEntrada,
        string? observacion,
        int operadorId);
}