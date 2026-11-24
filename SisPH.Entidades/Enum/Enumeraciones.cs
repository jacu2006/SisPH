namespace SisPH.Entidades.Enums;

public enum Rol
{
    Administrador = 1,
    Operador = 2,
    Residente = 3
}

public enum TipoOcupacion
{
    Propietario,
    Inquilino
}

public enum TipoMudanza
{
    Entrada,
    Salida
}

public enum EstadoCuota
{
    Pendiente,
    Pagada,
    Vencida,
    Morosa
}

public enum MetodoPago
{
    Efectivo,
    ACH,
    Yappy,
    Tarjeta
}

public enum EstadoIncidencia
{
    Abierta,
    EnProceso,
    Resuelta,
    Cerrada
}

public enum EstadoReserva
{
    Solicitada,
    Aprobada,
    Rechazada,
    Usada,
    Cancelada
}

public enum TipoCuota
{
    Ordinaria,
    Extraordinaria
}