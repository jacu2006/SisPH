# SisPH — Sistema de Administración de Propiedad Horizontal

> **📅 Fecha:** 24/11/2026

## 📋 Contenido del Repositorio

Este repositorio contiene la implementación integral de **SisPH**, una plataforma web de gestión administrativa, financiera, operativa y de seguridad para conjuntos residenciales bajo régimen de **Propiedad Horizontal**, desarrollada en **C# con ASP.NET Core 10.0 MVC** y **SQL Server 2022 Express**.

La solución está diseñada bajo una **arquitectura en 5 capas** (`SisPH.Entidades`, `SisPH.Seguridad`, `SisPH.Datos`, `SisPH.Negocio` y `SisPH.Web`) aplicando los principios **SOLID** y **DRY**, acceso a datos mediante **Entity Framework Core** y procedimientos almacenados, autenticación por cookies seguras con **BCrypt**, integridad criptográfica **HMAC-SHA256** en bitácora y recibos de pago, generación de códigos **QR**, exportación de reportes a **Excel (`.xlsx`)** y cumplimiento de controles **OWASP Top 10** y la **Ley 81 de 2019** de Protección de Datos Personales de Panamá.

### 🛠️ Tecnologías Utilizadas

![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![.NET](https://img.shields.io/badge/.NET%2010.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL%20Server%202022-CC2927?style=for-the-badge&logo=microsoft-sql-server&logoColor=white)
![Bootstrap](https://img.shields.io/badge/Bootstrap%205-7952B3?style=for-the-badge&logo=bootstrap&logoColor=white)
![Visual Studio](https://img.shields.io/badge/Visual%20Studio%202026-5C2D91?style=for-the-badge&logo=visual-studio&logoColor=white)
![Git](https://img.shields.io/badge/Git-F05032?style=for-the-badge&logo=git&logoColor=white)

* **Lenguaje / Framework:** C# / ASP.NET Core 10.0 MVC (Arquitectura modular de 5 proyectos)
* **Base de datos y ORM:** SQL Server 2022 Express + Entity Framework Core 10.0.15 (`Microsoft.EntityFrameworkCore.SqlServer`)
* **Seguridad y Criptografía:** `BCrypt.Net-Next` (factor de costo 12), `HMAC-SHA256` (`System.Security.Cryptography`), Cookies `HttpOnly` / `SameSite`, validación global `AutoValidateAntiforgeryToken`
* **Librerías Complementarias:** `QRCoder` (códigos QR en PNG/Base64), `ClosedXML` (exportación a `.xlsx`), `SixLabors.ImageSharp` (miniaturas de 200 px), `Serilog` (bitácora técnica diaria en `logs/`)
* **Frontend:** Razor Views, HTML5 Semántico, CSS3 (Modo claro minimalista con barra lateral colapsable), Bootstrap 5, JavaScript
* **Herramientas:** Microsoft Visual Studio 2026, SQL Server Management Studio 22 (SSMS), Git & GitHub

### 🧩 Componentes y Arquitectura Implementada

* **Capa de Entidades (`SisPH.Entidades`):** 18 entidades de dominio alineadas al modelo relacional, 8 enumeraciones tipadas, interfaz `IDesactivable` para baja lógica y DTO `MorosidadView` mapeado a la vista `vw_Morosidad`.
* **Capa de Seguridad (`SisPH.Seguridad`):** Contrato `ITransformacionSegura` con inyección por clave (`AddKeyedScoped`) implementando `HashBCrypt` (contraseñas) y `FirmaHmacSha256` (sellado de bitácora y recibos de pago).
* **Capa de Datos (`SisPH.Datos`):** `SisPHDbContext`, repositorio genérico `RepositorioBase<T>` (`IRepositorio<T>`), `RepositorioLogError` e invocación de procedimientos almacenados (`sp_GenerarCuotas`, `sp_ActualizarEstadosCuota`).
* **Capa de Negocio (`SisPH.Negocio`):** 17 servicios desacoplados (`ServicioBitacora`, `ServicioQR`, `ServicioArchivos`, `ExportadorExcel`, `ServicioAutenticacion`, `ServicioUsuarios`, `ServicioUnidades`, `ServicioResidentes`, `ServicioAreas`, `ServicioCuotas`, `ServicioPagos`, `ServicioReservas`, `ServicioIncidencias`, `ServicioPases`, `ServicioMudanzas`, `ServicioCuenta` y `ServicioReportes`).
* **Capa de Presentación (`SisPH.Web`):** 14 controladores MVC protegidos por rol (`Administrador`, `Operador`, `Residente`), `ManejoErroresMiddleware`, plantilla maestra `_Layout.cshtml` con secciones colapsables, visor lightbox para galerías y vistas parciales reutilizables (`_BadgeEstado`, `_GaleriaImagenes`).

---

## 📸 Capturas de Pantalla y Problemas Resueltos

### Arquitectura y Problemas Resueltos

* **Problema / Requisito #1 (Arquitectura en 5 Capas, SOLID y DRY):** Se desacopló la lógica de negocio y persistencia en cinco bibliotecas independientes (`Entidades`, `Seguridad`, `Datos`, `Negocio`, `Web`), centralizando las operaciones CRUD en `RepositorioBase<T>`, la exportación tabular en `ExportadorExcel` y la visualización de estados y galerías en vistas parciales reutilizables.
* **Problema / Requisito #2 (Auditoría Inmutable y Detección de Alteraciones con HMAC-SHA256):** Se protegió la tabla `Bitacora` mediante el disparador `TR_Bitacora_SoloInsertar` en SQL Server (bloqueando `UPDATE` y `DELETE`) y firmando cada registro con `HMAC-SHA256`. El módulo de Auditoría (`CU-18`) recalcula cada firma en tiempo real para alertar visualmente si una fila fue alterada directamente en la base de datos.
* **Problema / Requisito #3 (Transacciones Atómicas de Pagos y Recibos Verificables con QR):** El registro de pagos (`CU-07` / `CU-20`) soporta abonos parciales y cancelación múltiple de cuotas dentro de una transacción explícita con `Rollback` automático ante fallos, generando un recibo firmado digitalmente con código QR verificable públicamente sin necesidad de iniciar sesión.
* **Problema / Requisito #4 (Control de Acceso en Garita mediante Pases QR Temporales):** Los residentes generan desde su portal pases de visita con tokens únicos de 32 caracteres y vigencia máxima de 24 horas (`CU-12`); en garita (`CU-13`) el operador escanea el código para registrar entrada y salida en tiempo real, rechazando pases vencidos, inexistentes o reutilizados.
* **Problema / Requisito #5 (Reglas de Paz y Salvo, Multimedia y Control OWASP Top 10):** Se implementó validación automática de solvencia financiera (`CU-19`) antes de aprobar mudanzas o solicitar reservas de áreas sociales, bloqueo temporal por fuerza bruta (5 intentos fallidos / 15 minutos), validación de archivos de imagen ($\le 2\text{ MB}$) con miniaturas de `200 px` y captura global de excepciones sin exposición de trazas al usuario final.

---

### 1. Página Pública e Inicio de Sesión por Roles (`CU-17` y `CU-01`)

*Vista informativa pública y formulario de autenticación protegido contra fuerza bruta con redirección según rol:*

![Página Pública e Inicio de Sesión](./capturas/01_pagina_publica_login.png)

### 2. Gestión de Torres, Unidades y Residentes (`CU-03` y `CU-04`)

*Administración de apartamentos, control de coeficientes de participación por torre y registro de propietarios e inquilinos:*

![Torres, Unidades y Residentes](./capturas/02_admin_unidades_residentes.png)

### 3. Catálogo de Áreas Sociales y Galería Interactiva (`CU-05`)

*Gestión de espacios comunes, tarifas, horarios y visualización ampliada de fotografías:*

![Áreas Sociales y Galería](./capturas/03_admin_areas_sociales.png)

### 4. Generación de Cuotas y Recibo Digital Firmado con QR (`CU-06`, `CU-07` y `CU-20`)

*Cálculo proporcional por coeficiente y emisión de comprobante de pago con firma HMAC-SHA256 y código QR:*

![Cuotas y Recibo QR](./capturas/04_cuotas_pagos_recibo_qr.png)

### 5. Portal del Residente y Generación de Pases de Visita QR (`CU-15` y `CU-12`)

*Consulta de estado de cuenta (paz y salvo) y creación de pases QR temporales para visitantes:*

![Portal del Residente y Pase QR](./capturas/05_portal_residente_pase_qr.png)

### 6. Control de Garita y Validación de Visitantes (`CU-13`)

*Escaneo de token QR en garita con registro automático de entrada y salida en el historial del día:*

![Control de Garita QR](./capturas/06_garita_escaneo_qr.png)

### 7. Reportes Financieros, Exportación Excel y Proyección Quinquenal (`CU-16` y `CU-21`)

*Consulta de morosidad por torre, exportación a `.xlsx` y proyección presupuestaria a 5 años:*

![Reportes y Proyección Quinquenal](./capturas/07_reportes_presupuesto.png)

### 8. Auditoría Criptográfica y Base de Datos SQL Server 2022 (`CU-18`)

*Verificación de firmas HMAC-SHA256 en bitácora y estructura relacional de 18 tablas en SQL Server Management Studio:*

![Auditoría HMAC-SHA256 y SQL Server](./capturas/08_auditoria_hmac_sqlserver.png)

---

## 📂 Estructura de Carpetas o Directorios

```text
SisPH/
├── SisPH.Entidades/                # Capa 1: Entidades de dominio, Enums, Interfaces y DTOs
│   ├── DTOs/                       # MorosidadView (mapeo de vw_Morosidad)
│   ├── Entidades/                  # 18 clases del modelo relacional (Usuario, Unidad, Cuota, Pago, etc.)
│   ├── Enums/                      # Enumeraciones (Rol, EstadoCuota, MetodoPago, EstadoReserva, etc.)
│   └── Interfaces/                 # IDesactivable (baja lógica)
├── SisPH.Seguridad/                # Capa 2: Implementaciones criptográficas
│   ├── ITransformacionSegura.cs    # Contrato común de transformación y verificación
│   ├── HashBCrypt.cs               # Hashing de contraseñas con BCrypt (factor 12)
│   └── FirmaHmacSha256.cs          # Firma y verificación HMAC-SHA256
├── SisPH.Datos/                    # Capa 3: Acceso a datos con Entity Framework Core
│   ├── Contexto/                   # SisPHDbContext (mapeo de 18 tablas, vista y SPs)
│   ├── Interfaces/                 # IRepositorio<T>, IRepositorioLogError
│   └── Repositorios/               # RepositorioBase<T>, RepositorioLogError
├── SisPH.Negocio/                  # Capa 4: Lógica de negocio y servicios transversales
│   ├── Interfaces/                 # 17 contratos de servicios (SOLID)
│   ├── Modelos/                    # ResultadoOperacion y ResultadoOperacion<T>
│   └── Servicios/                  # 17 servicios de casos de uso, QR, Excel, Bitácora e Imágenes
├── SisPH.Web/                      # Capa 5: Aplicación web ASP.NET Core 10.0 MVC
│   ├── Controllers/                # 14 controladores MVC organizados por rol y módulo
│   ├── Extensions/                 # ClaimsPrincipalExtensions (lectura de sesión)
│   ├── Middleware/                 # ManejoErroresMiddleware (captura global RNF-06)
│   ├── Models/                     # ViewModels con DataAnnotations
│   ├── Views/                      # Vistas Razor, _Layout.cshtml y parciales DRY
│   ├── wwwroot/                    # CSS, Bootstrap 5 y carpeta uploads/ (areas, residentes, incidencias)
│   ├── appsettings.json            # Configuración base de conexión
│   └── Program.cs                  # Inyección de dependencias, Serilog, Cookies y Antiforgery
├── script/
│   ├── SisPH.sql                   # Script DDL/DML completo (18 tablas, vista, 2 SPs, trigger y datos semilla)
│   └── SisPH.bak                   # Respaldo completo de la base de datos SQL Server 2022 con datos de prueba
├── capturas/                       # Evidencias visuales del sistema para documentación
├── logs/                           # Archivos diarios de log técnico generados por Serilog
├── .gitignore                      # Exclusión de bin/, obj/, .vs/, logs/ y archivos sensibles
├── SisPH.slnx                      # Archivo de solución de Visual Studio
└── README.md                       # Documentación del proyecto
```

## 🚀 Instrucciones de Ejecución / Uso

Pasos para configurar y ejecutar el proyecto en un entorno local:

1. **Clonar el repositorio:**

   ```bash
   git clone https://github.com/jacu2006/SisPH.git
   cd SisPH
   ```

2. **Restaurar la base de datos en SQL Server 2022:**

   * **Opción A (Mediante respaldo `script/SisPH.bak`):** Abrir **SQL Server Management Studio (SSMS)** conectado a `localhost\SQLEXPRESS`, hacer clic derecho sobre **Bases de datos → Restaurar base de datos...**, seleccionar **Dispositivo**, elegir el archivo `script/SisPH.bak` incluido en este repositorio y confirmar la restauración con todos los datos de prueba cargados.
   * **Opción B (Mediante script SQL):** Abrir el archivo `script/SisPH.sql` en SSMS y ejecutarlo para crear desde cero la base de datos `SisPH`, las 18 tablas, la vista `vw_Morosidad`, los procedimientos almacenados, el trigger de bitácora y los datos iniciales.

3. **Configurar la cadena de conexión y clave criptográfica (`User Secrets`):**

   * Verificar que `SisPH.Web/appsettings.json` apunte a tu instancia local (`Server=localhost\SQLEXPRESS;Database=SisPH;Trusted_Connection=True;TrustServerCertificate=True;`).
   * Configurar la clave secreta `Crypto:FirmaKey` (Base64 de 32 bytes) en `SisPH.Web` ejecutando en terminal:

   ```bash
   dotnet user-secrets init --project SisPH.Web
   dotnet user-secrets set "Crypto:FirmaKey" "K7gNU3sdo+OL0wNhqoVWhr3g6s1xYv72ol/pe/Unols=" --project SisPH.Web
   ```

4. **Compilar y ejecutar en Visual Studio 2026:**

   * Abrir `SisPH.slnx` en **Visual Studio 2026**.
   * Establecer **`SisPH.Web`** como proyecto de inicio y presionar `F5` (o ejecutar `dotnet run --project SisPH.Web`).
   * **Credenciales de administrador por defecto:**
     * **Correo:** `admin@sisph.com`
     * **Contraseña:** `Admin123*`

## 👥 Autores y Contexto

* **Integrantes del Equipo:**
  * Elvis Li
  * Wilson Wu
  * Javier Acuña
* **Grupo:** `1IL133`
* **Asignatura:** Herramientas de la Programación Aplicada III (.Net)
* **Instructora:** Ing. Irina Fong
* **Institución:** Universidad Tecnológica de Panamá (UTP) — Facultad de Ingeniería de Sistemas Computacionales (FISC)
* **Fecha de Realización:** 24/11/2026

## 📚 Referencias

* [Documentación Oficial de ASP.NET Core 10.0 MVC](https://learn.microsoft.com/es-es/aspnet/core/mvc/overview)
* [Documentación Oficial de Entity Framework Core](https://learn.microsoft.com/es-es/ef/core/)
* [OWASP Top 10 — Riesgos de Seguridad en Aplicaciones Web](https://owasp.org/www-project-top-ten/)
* [Ley 81 de 26 de marzo de 2019 — Protección de Datos Personales (República de Panamá)](https://www.gacetaoficial.gob.pa/)
* [Librerías Open Source: QRCoder, ClosedXML, SixLabors.ImageSharp y BCrypt.Net-Next](https://www.nuget.org/)