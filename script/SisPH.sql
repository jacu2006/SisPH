/* ============================================================================
   SISTEMA DE ADMINISTRACIÓN DE PROPIEDAD HORIZONTAL (SisPH)
   Motor: Microsoft SQL Server 2022
   Estructura: 18 tablas, 123 columnas, vista, procedimientos, trigger y semillas
   ============================================================================ */

USE master;
GO

/* ============================================================================
   ADVERTENCIA DE SEGURIDAD:
   Este script está diseñado para ejecutarse únicamente sobre una base de datos
   nueva o limpia. No se ejecuta DROP DATABASE de forma automática para evitar
   la pérdida accidental de datos en entornos existentes.
   Si necesitas recrear la base de datos en desarrollo local, elimínala
   manualmente primero o descomenta el bloque siguiente bajo tu responsabilidad:

   IF DB_ID(N'SisPH') IS NOT NULL
   BEGIN
       ALTER DATABASE SisPH SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
       DROP DATABASE SisPH;
   END;
   ============================================================================ */

IF DB_ID(N'SisPH') IS NULL
BEGIN
    CREATE DATABASE [SisPH];
    ALTER DATABASE [SisPH] SET AUTO_CLOSE OFF;
END;
GO

USE SisPH;
GO

/* ============================================================================
   1. TABLAS DE SEGURIDAD Y AUDITORÍA (Rol, Usuario, Bitacora, LogError)
   ============================================================================ */

CREATE TABLE Rol (
    Id      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Rol PRIMARY KEY,
    Nombre  NVARCHAR(30)      NOT NULL CONSTRAINT UQ_Rol_Nombre UNIQUE
);
GO

CREATE TABLE Usuario (
    Id               INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Usuario PRIMARY KEY,
    RolId            INT               NOT NULL CONSTRAINT FK_Usuario_Rol REFERENCES Rol(Id),
    Nombre           NVARCHAR(100)     NOT NULL,
    Correo           NVARCHAR(120)     NOT NULL CONSTRAINT UQ_Usuario_Correo UNIQUE,
    PasswordHash     NVARCHAR(100)     NOT NULL,
    Activo           BIT               NOT NULL CONSTRAINT DF_Usuario_Activo DEFAULT 1,
    IntentosFallidos INT               NOT NULL CONSTRAINT DF_Usuario_IntentosFallidos DEFAULT 0
                                       CONSTRAINT CK_Usuario_IntentosFallidos CHECK (IntentosFallidos >= 0),
    BloqueadoHasta   DATETIME2         NULL,
    FechaCreacion    DATETIME2         NOT NULL CONSTRAINT DF_Usuario_FechaCreacion DEFAULT SYSDATETIME()
);
GO

CREATE TABLE Bitacora (
    Id        BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Bitacora PRIMARY KEY,
    UsuarioId INT                  NULL     CONSTRAINT FK_Bitacora_Usuario REFERENCES Usuario(Id),
    Fecha     DATETIME2            NOT NULL CONSTRAINT DF_Bitacora_Fecha DEFAULT SYSDATETIME(),
    Accion    NVARCHAR(50)         NOT NULL,
    Entidad   NVARCHAR(50)         NOT NULL,
    EntidadId NVARCHAR(50)         NULL,
    Detalle   NVARCHAR(MAX)        NULL,
    Firma     NVARCHAR(100)        NOT NULL
);
GO

CREATE TABLE LogError (
    Id         BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LogError PRIMARY KEY,
    UsuarioId  INT                  NULL     CONSTRAINT FK_LogError_Usuario REFERENCES Usuario(Id),
    Fecha      DATETIME2            NOT NULL CONSTRAINT DF_LogError_Fecha DEFAULT SYSDATETIME(),
    Origen     NVARCHAR(200)        NOT NULL,
    Mensaje    NVARCHAR(1000)       NOT NULL,
    StackTrace NVARCHAR(MAX)        NULL,
    Ruta       NVARCHAR(300)        NULL,
    Ip         NVARCHAR(45)         NULL
);
GO

/* ============================================================================
   2. TABLAS DEL NÚCLEO DEL PH (Torre, Unidad, Residente, Ocupacion, Mudanza)
   ============================================================================ */

CREATE TABLE Torre (
    Id     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Torre PRIMARY KEY,
    Nombre NVARCHAR(50)      NOT NULL CONSTRAINT UQ_Torre_Nombre UNIQUE,
    Pisos  INT               NOT NULL CONSTRAINT CK_Torre_Pisos CHECK (Pisos > 0)
);
GO

CREATE TABLE Unidad (
    Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Unidad PRIMARY KEY,
    TorreId     INT               NOT NULL CONSTRAINT FK_Unidad_Torre REFERENCES Torre(Id),
    Numero      NVARCHAR(10)      NOT NULL,
    Piso        INT               NOT NULL,
    Metros2     DECIMAL(8,2)      NOT NULL CONSTRAINT CK_Unidad_Metros2 CHECK (Metros2 > 0),
    Coeficiente DECIMAL(7,4)      NOT NULL CONSTRAINT CK_Unidad_Coeficiente CHECK (Coeficiente > 0 AND Coeficiente <= 100),
    Activo      BIT               NOT NULL CONSTRAINT DF_Unidad_Activo DEFAULT 1,
    CONSTRAINT UQ_Unidad_Torre_Numero UNIQUE (TorreId, Numero)
);
GO

CREATE TABLE Residente (
    Id        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Residente PRIMARY KEY,
    UsuarioId INT               NULL     CONSTRAINT FK_Residente_Usuario REFERENCES Usuario(Id),
    Cedula    NVARCHAR(20)      NOT NULL CONSTRAINT UQ_Residente_Cedula UNIQUE,
    Nombre    NVARCHAR(60)      NOT NULL,
    Apellido  NVARCHAR(60)      NOT NULL,
    Foto      NVARCHAR(300)     NULL,
    Telefono  NVARCHAR(20)      NULL,
    Correo    NVARCHAR(120)     NOT NULL CONSTRAINT UQ_Residente_Correo UNIQUE,
    Activo    BIT               NOT NULL CONSTRAINT DF_Residente_Activo DEFAULT 1
);
GO

CREATE TABLE Ocupacion (
    Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Ocupacion PRIMARY KEY,
    UnidadId    INT               NOT NULL CONSTRAINT FK_Ocupacion_Unidad REFERENCES Unidad(Id),
    ResidenteId INT               NOT NULL CONSTRAINT FK_Ocupacion_Residente REFERENCES Residente(Id),
    Tipo        NVARCHAR(15)      NOT NULL CONSTRAINT CK_Ocupacion_Tipo CHECK (Tipo IN ('Propietario', 'Inquilino')),
    FechaInicio DATE              NOT NULL,
    FechaFin    DATE              NULL,
    CONSTRAINT CK_Ocupacion_Fechas CHECK (FechaFin IS NULL OR FechaFin >= FechaInicio)
);
GO

CREATE TABLE Mudanza (
    Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Mudanza PRIMARY KEY,
    UnidadId    INT               NOT NULL CONSTRAINT FK_Mudanza_Unidad REFERENCES Unidad(Id),
    UsuarioId   INT               NOT NULL CONSTRAINT FK_Mudanza_Usuario REFERENCES Usuario(Id),
    Tipo        NVARCHAR(10)      NOT NULL CONSTRAINT CK_Mudanza_Tipo CHECK (Tipo IN ('Entrada', 'Salida')),
    Fecha       DATETIME2         NOT NULL CONSTRAINT DF_Mudanza_Fecha DEFAULT SYSDATETIME(),
    Observacion NVARCHAR(500)     NULL
);
GO

/* ============================================================================
   3. TABLAS FINANCIERAS (Cuota, Pago, PagoDetalle, Presupuesto)
   ============================================================================ */

CREATE TABLE Cuota (
    Id       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Cuota PRIMARY KEY,
    UnidadId INT               NOT NULL CONSTRAINT FK_Cuota_Unidad REFERENCES Unidad(Id),
    Periodo  CHAR(7)           NOT NULL,
    Tipo     NVARCHAR(15)      NOT NULL CONSTRAINT DF_Cuota_Tipo DEFAULT 'Ordinaria'
                               CONSTRAINT CK_Cuota_Tipo CHECK (Tipo IN ('Ordinaria', 'Extraordinaria')),
    Monto    DECIMAL(10,2)     NOT NULL CONSTRAINT CK_Cuota_Monto CHECK (Monto >= 0),
    Recargo  DECIMAL(10,2)     NOT NULL CONSTRAINT DF_Cuota_Recargo DEFAULT 0
                               CONSTRAINT CK_Cuota_Recargo CHECK (Recargo >= 0),
    Vence    DATE              NOT NULL,
    Estado   NVARCHAR(10)      NOT NULL CONSTRAINT DF_Cuota_Estado DEFAULT 'Pendiente'
                               CONSTRAINT CK_Cuota_Estado CHECK (Estado IN ('Pendiente', 'Pagada', 'Vencida', 'Morosa')),
    CONSTRAINT UQ_Cuota_Unidad_Periodo_Tipo UNIQUE (UnidadId, Periodo, Tipo)
);
GO

CREATE TABLE Pago (
    Id         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Pago PRIMARY KEY,
    UnidadId   INT               NOT NULL CONSTRAINT FK_Pago_Unidad REFERENCES Unidad(Id),
    UsuarioId  INT               NOT NULL CONSTRAINT FK_Pago_Usuario REFERENCES Usuario(Id),
    Fecha      DATETIME2         NOT NULL CONSTRAINT DF_Pago_Fecha DEFAULT SYSDATETIME(),
    Monto      DECIMAL(10,2)     NOT NULL CONSTRAINT CK_Pago_Monto CHECK (Monto > 0),
    Metodo     NVARCHAR(15)      NOT NULL CONSTRAINT CK_Pago_Metodo CHECK (Metodo IN ('Efectivo', 'ACH', 'Yappy', 'Tarjeta')),
    Referencia NVARCHAR(50)      NULL,
    Firma      NVARCHAR(100)     NOT NULL
);
GO

CREATE TABLE PagoDetalle (
    Id            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PagoDetalle PRIMARY KEY,
    PagoId        INT               NOT NULL CONSTRAINT FK_PagoDetalle_Pago REFERENCES Pago(Id),
    CuotaId       INT               NOT NULL CONSTRAINT FK_PagoDetalle_Cuota REFERENCES Cuota(Id),
    MontoAplicado DECIMAL(10,2)     NOT NULL CONSTRAINT CK_PagoDetalle_MontoAplicado CHECK (MontoAplicado > 0)
);
GO

CREATE TABLE Presupuesto (
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Presupuesto PRIMARY KEY,
    Anio            INT               NOT NULL CONSTRAINT CK_Presupuesto_Anio CHECK (Anio >= 2000),
    Concepto        NVARCHAR(100)     NOT NULL,
    Tipo            NVARCHAR(7)       NOT NULL CONSTRAINT CK_Presupuesto_Tipo CHECK (Tipo IN ('Ingreso', 'Gasto')),
    MontoProyectado DECIMAL(12,2)     NOT NULL CONSTRAINT CK_Presupuesto_Monto CHECK (MontoProyectado >= 0)
);
GO

/* ============================================================================
   4. TABLAS DE SERVICIOS COMUNES Y GARITA
      (AreaSocial, ImagenArea, Reserva, Incidencia, PaseVisita)
   ============================================================================ */

CREATE TABLE AreaSocial (
    Id           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AreaSocial PRIMARY KEY,
    Nombre       NVARCHAR(50)      NOT NULL CONSTRAINT UQ_AreaSocial_Nombre UNIQUE,
    Descripcion  NVARCHAR(500)     NULL,
    Capacidad    INT               NOT NULL CONSTRAINT CK_AreaSocial_Capacidad CHECK (Capacidad > 0),
    Costo        DECIMAL(8,2)      NOT NULL CONSTRAINT DF_AreaSocial_Costo DEFAULT 0
                                   CONSTRAINT CK_AreaSocial_Costo CHECK (Costo >= 0),
    Deposito     DECIMAL(8,2)      NOT NULL CONSTRAINT DF_AreaSocial_Deposito DEFAULT 0
                                   CONSTRAINT CK_AreaSocial_Deposito CHECK (Deposito >= 0),
    HoraApertura TIME              NOT NULL,
    HoraCierre   TIME              NOT NULL,
    Activo       BIT               NOT NULL CONSTRAINT DF_AreaSocial_Activo DEFAULT 1,
    CONSTRAINT CK_AreaSocial_Horario CHECK (HoraCierre > HoraApertura)
);
GO

CREATE TABLE ImagenArea (
    Id            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ImagenArea PRIMARY KEY,
    AreaId        INT               NOT NULL CONSTRAINT FK_ImagenArea_AreaSocial REFERENCES AreaSocial(Id),
    Ruta          NVARCHAR(300)     NOT NULL,
    RutaThumbnail NVARCHAR(300)     NOT NULL,
    EsPrincipal   BIT               NOT NULL CONSTRAINT DF_ImagenArea_EsPrincipal DEFAULT 0
);
GO

-- Índice único filtrado: garantiza máximo una imagen principal por área social
CREATE UNIQUE INDEX UX_ImagenArea_Principal
    ON ImagenArea(AreaId)
    WHERE EsPrincipal = 1;
GO

CREATE TABLE Reserva (
    Id             INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Reserva PRIMARY KEY,
    AreaId         INT               NOT NULL CONSTRAINT FK_Reserva_AreaSocial REFERENCES AreaSocial(Id),
    ResidenteId    INT               NOT NULL CONSTRAINT FK_Reserva_Residente REFERENCES Residente(Id),
    AprobadaPor    INT               NULL     CONSTRAINT FK_Reserva_Usuario REFERENCES Usuario(Id),
    Fecha          DATE              NOT NULL,
    HoraInicio     TIME              NOT NULL,
    HoraFin        TIME              NOT NULL,
    Estado         NVARCHAR(10)      NOT NULL CONSTRAINT DF_Reserva_Estado DEFAULT 'Solicitada'
                                     CONSTRAINT CK_Reserva_Estado CHECK (Estado IN ('Solicitada', 'Aprobada', 'Rechazada', 'Usada', 'Cancelada')),
    MotivoRechazo  NVARCHAR(300)     NULL,
    FechaSolicitud DATETIME2         NOT NULL CONSTRAINT DF_Reserva_FechaSolicitud DEFAULT SYSDATETIME(),
    CONSTRAINT CK_Reserva_Horario CHECK (HoraFin > HoraInicio)
);
GO

CREATE TABLE Incidencia (
    Id                 INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Incidencia PRIMARY KEY,
    ResidenteId        INT               NOT NULL CONSTRAINT FK_Incidencia_Residente REFERENCES Residente(Id),
    Titulo             NVARCHAR(100)     NOT NULL,
    Descripcion        NVARCHAR(1000)    NOT NULL,
    Foto               NVARCHAR(300)     NULL,
    Estado             NVARCHAR(12)      NOT NULL CONSTRAINT DF_Incidencia_Estado DEFAULT 'Abierta'
                                         CONSTRAINT CK_Incidencia_Estado CHECK (Estado IN ('Abierta', 'EnProceso', 'Resuelta', 'Cerrada')),
    ObservacionTecnica NVARCHAR(1000)    NULL,
    Fecha              DATETIME2         NOT NULL CONSTRAINT DF_Incidencia_Fecha DEFAULT SYSDATETIME()
);
GO

CREATE TABLE PaseVisita (
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PaseVisita PRIMARY KEY,
    ResidenteId     INT               NOT NULL CONSTRAINT FK_PaseVisita_Residente REFERENCES Residente(Id),
    NombreVisitante NVARCHAR(120)     NOT NULL,
    CedulaVisitante NVARCHAR(20)      NULL,
    TokenQR         CHAR(32)          NOT NULL CONSTRAINT UQ_PaseVisita_TokenQR UNIQUE,
    VigenteHasta    DATETIME2         NOT NULL,
    Entrada         DATETIME2         NULL,
    Salida          DATETIME2         NULL
);
GO

/* ============================================================================
   5. ÍNDICES DE RENDIMIENTO (RNF-07: consultas < 2s con 500 unidades / 5 años)
   ============================================================================ */

CREATE INDEX IX_Cuota_Unidad_Estado      ON Cuota(UnidadId, Estado);
CREATE INDEX IX_Pago_Unidad_Fecha        ON Pago(UnidadId, Fecha DESC);
CREATE INDEX IX_Reserva_Area_Fecha       ON Reserva(AreaId, Fecha, Estado);
CREATE INDEX IX_Ocupacion_Unidad_Vigente ON Ocupacion(UnidadId, FechaFin);
CREATE INDEX IX_Bitacora_Fecha           ON Bitacora(Fecha DESC);
CREATE INDEX IX_LogError_Fecha           ON LogError(Fecha DESC);
GO

/* ============================================================================
   6. TRIGGER DE INMUTABILIDAD DE BITÁCORA (RF-03 / OWASP A08)
   ============================================================================ */

CREATE TRIGGER TR_Bitacora_SoloInsertar ON Bitacora
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    RAISERROR('La bitácora es inmutable: solo se permiten inserciones.', 16, 1);
    ROLLBACK TRANSACTION;
END;
GO

/* ============================================================================
   7. VISTA DE MOROSIDAD (RF-09, RF-16 y validación de Paz y Salvo)
   ============================================================================ */

CREATE VIEW vw_Morosidad
AS
SELECT
    u.Id AS UnidadId,
    t.Id AS TorreId,
    t.Nombre AS Torre,
    u.Numero AS NumeroUnidad,
    u.Piso,
    COALESCE(r.Nombre + N' ' + r.Apellido, N'Sin residente asignado') AS Residente,
    r.Cedula,
    r.Correo,
    r.Telefono,
    SUM(CASE WHEN c.Estado = 'Vencida' THEN 1 ELSE 0 END) AS CuotasVencidas,
    SUM(CASE WHEN c.Estado = 'Morosa'  THEN 1 ELSE 0 END) AS CuotasMorosas,
    COUNT(c.Id) AS TotalCuotasAtrasadas,
    SUM((c.Monto + c.Recargo) - ISNULL(pagado.TotalAplicado, 0)) AS SaldoAdeudado,
    MIN(c.Vence) AS VencimientoMasAntiguo
FROM Unidad u
INNER JOIN Torre t ON t.Id = u.TorreId
INNER JOIN Cuota c ON c.UnidadId = u.Id
    AND c.Estado IN ('Vencida', 'Morosa')
LEFT JOIN (
    SELECT CuotaId, SUM(MontoAplicado) AS TotalAplicado
    FROM PagoDetalle
    GROUP BY CuotaId
) pagado ON pagado.CuotaId = c.Id
OUTER APPLY (
    SELECT TOP 1 res.Nombre, res.Apellido, res.Cedula, res.Correo, res.Telefono
    FROM Ocupacion oc
    INNER JOIN Residente res ON res.Id = oc.ResidenteId
    WHERE oc.UnidadId = u.Id
      AND oc.FechaFin IS NULL
      AND res.Activo = 1
    ORDER BY CASE WHEN oc.Tipo = 'Propietario' THEN 1 ELSE 2 END, oc.FechaInicio DESC
) r
WHERE u.Activo = 1
GROUP BY
    u.Id, t.Id, t.Nombre, u.Numero, u.Piso,
    r.Nombre, r.Apellido, r.Cedula, r.Correo, r.Telefono;
GO

/* ============================================================================
   8. PROCEDIMIENTOS ALMACENADOS (sp_GenerarCuotas y sp_ActualizarEstadosCuota)
   ============================================================================ */

-- Genera las cuotas mensuales (o extraordinarias) según el coeficiente de cada unidad activa
CREATE PROCEDURE sp_GenerarCuotas
    @Periodo            CHAR(7),
    @PresupuestoMensual DECIMAL(12,2),
    @Tipo               NVARCHAR(15) = 'Ordinaria',
    @Vence              DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @Periodo NOT LIKE '[0-9][0-9][0-9][0-9]-[0-1][0-9]'
    BEGIN
        RAISERROR('El período debe tener el formato AAAA-MM.', 16, 1);
        RETURN;
    END;

    IF @PresupuestoMensual <= 0
    BEGIN
        RAISERROR('El presupuesto debe ser mayor que cero.', 16, 1);
        RETURN;
    END;

    IF EXISTS (SELECT 1 FROM Cuota WHERE Periodo = @Periodo AND Tipo = @Tipo)
    BEGIN
        RAISERROR('Ya existen cuotas generadas para ese período y tipo.', 16, 1);
        RETURN;
    END;

    DECLARE @SumaCoeficienteGlobal DECIMAL(9,4);
    SELECT @SumaCoeficienteGlobal = ISNULL(SUM(Coeficiente), 0)
    FROM Unidad
    WHERE Activo = 1;

    IF @SumaCoeficienteGlobal <= 0
    BEGIN
        RAISERROR('No existen unidades activas con coeficiente válido.', 16, 1);
        RETURN;
    END;

    IF @SumaCoeficienteGlobal > 100.0000
    BEGIN
        RAISERROR('La suma global de coeficientes activos de todo el PH supera el 100 %%. Ajuste los coeficientes antes de generar cuotas.', 16, 1);
        RETURN;
    END;

    IF @Vence IS NULL
    BEGIN
        SET @Vence = EOMONTH(CAST(@Periodo + '-01' AS DATE));
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO Cuota (UnidadId, Periodo, Tipo, Monto, Recargo, Vence, Estado)
        SELECT
            Id,
            @Periodo,
            @Tipo,
            ROUND(@PresupuestoMensual * (Coeficiente / 100.0), 2),
            0,
            @Vence,
            'Pendiente'
        FROM Unidad
        WHERE Activo = 1;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- Actualiza diariamente las cuotas: Pendiente -> Vencida (con recargo) y Vencida -> Morosa (2+ meses)
CREATE PROCEDURE sp_ActualizarEstadosCuota
    @PorcentajeRecargo DECIMAL(5,2) = 10.00,
    @FechaCorte        DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @FechaCorte IS NULL
        SET @FechaCorte = CAST(SYSDATETIME() AS DATE);

    BEGIN TRY
        BEGIN TRANSACTION;

        -- 1. Pasar a Vencida las cuotas Pendientes cuya fecha de vencimiento ya pasó y aplicar recargo
        UPDATE Cuota
        SET
            Estado  = 'Vencida',
            Recargo = CASE
                        WHEN Recargo = 0 THEN ROUND(Monto * (@PorcentajeRecargo / 100.0), 2)
                        ELSE Recargo
                      END
        WHERE Estado = 'Pendiente'
          AND Vence < @FechaCorte;

        -- 2. Pasar a Morosa las cuotas Vencidas con 2 o más meses de atraso
        UPDATE Cuota
        SET Estado = 'Morosa'
        WHERE Estado = 'Vencida'
          AND DATEADD(MONTH, 2, Vence) <= @FechaCorte;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

/* ============================================================================
   9. DATOS INICIALES (Roles, Administrador inicial, Torres y Áreas Sociales)
   ============================================================================ */

-- Roles del sistema
INSERT INTO Rol (Nombre) VALUES
    (N'Administrador'),
    (N'Operador'),
    (N'Residente');
GO

-- Usuario Administrador inicial (Contraseña temporal: Admin123* — hash BCrypt factor 12)
INSERT INTO Usuario (RolId, Nombre, Correo, PasswordHash, Activo)
VALUES (
    1,
    N'Administrador SisPH',
    N'admin@sisph.com',
    N'$2a$12$Zccp4NdHZE9to2ih5rDU/eID5ir8ElC1lKMcC7dNbt7O6JAnSCu5K',
    1
);
GO

-- Torres iniciales
INSERT INTO Torre (Nombre, Pisos) VALUES
    (N'Torre A', 15),
    (N'Torre B', 15);
GO

-- Áreas sociales iniciales
INSERT INTO AreaSocial (Nombre, Descripcion, Capacidad, Costo, Deposito, HoraApertura, HoraCierre, Activo) VALUES
    (N'Salón de Eventos', N'Salón climatizado con mobiliario básico, cocina auxiliar y baños.', 80, 150.00, 100.00, '09:00', '23:00', 1),
    (N'Área de BBQ y Terraza', N'Terraza techada con dos parrillas a gas, mesas y área de estar.', 25, 50.00, 50.00, '10:00', '22:00', 1),
    (N'Piscina y Gazebos', N'Área recreativa de piscina familiar con tres gazebos compartidos.', 40, 0.00, 25.00, '08:00', '20:00', 1);
GO

-- Verificación rápida de objetos creados
SELECT COUNT(*) AS TotalTablas FROM sys.tables;
SELECT * FROM Rol;
SELECT Id, RolId, Nombre, Correo, Activo, FechaCreacion FROM Usuario;
GO