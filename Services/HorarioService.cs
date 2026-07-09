using Dapper;
using HorarioDocentes.Data;
using Npgsql;

namespace HorarioDocentes.Services;

public class HorarioService
{
    private readonly string _connectionString;

    public HorarioService(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'DefaultConnection'.");
    }

    private NpgsqlConnection GetConnection() => new(_connectionString);

    /// <summary>
    /// Reemplaza todo el horario existente por el contenido recién parseado del Excel.
    /// </summary>
    public async Task<(int totalDocentes, int totalClases)> GuardarCargaAsync(ParseResult parseResult, string nombreArchivo)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();

        try
        {
            // Reemplazo total: borra todo lo anterior (y reinicia los IDs) antes de insertar lo nuevo.
            await conn.ExecuteAsync(
                @"TRUNCATE TABLE ""HorarioClases"", ""Docentes"" RESTART IDENTITY CASCADE;",
                transaction: tx);

            int totalClases = 0;

            foreach (var docente in parseResult.Docentes)
            {
                var docenteId = await conn.ExecuteScalarAsync<int>(
                    @"INSERT INTO ""Docentes"" (""NombreCompleto"", ""Turno"")
                      VALUES (@NombreCompleto, @Turno)
                      RETURNING ""Id"";",
                    new { docente.NombreCompleto, docente.Turno },
                    transaction: tx);

                foreach (var clase in docente.Clases)
                {
                    await conn.ExecuteAsync(
                        @"INSERT INTO ""HorarioClases"" (""DocenteId"", ""Dia"", ""HoraInicio"", ""HoraFin"", ""Seccion"", ""Area"")
                          VALUES (@DocenteId, @Dia, @HoraInicio, @HoraFin, @Seccion, @Area);",
                        new
                        {
                            DocenteId = docenteId,
                            clase.Dia,
                            HoraInicio = clase.HoraInicioTexto,
                            HoraFin = clase.HoraFinTexto,
                            clase.Seccion,
                            clase.Area
                        },
                        transaction: tx);
                    totalClases++;
                }
            }

            await conn.ExecuteAsync(
                @"INSERT INTO ""CargasExcel"" (""NombreArchivo"", ""TotalDocentes"", ""TotalClases"")
                  VALUES (@NombreArchivo, @TotalDocentes, @TotalClases);",
                new { NombreArchivo = nombreArchivo, TotalDocentes = parseResult.Docentes.Count, TotalClases = totalClases },
                transaction: tx);

            await tx.CommitAsync();
            return (parseResult.Docentes.Count, totalClases);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<List<string>> ObtenerSeccionesAsync()
    {
        using var conn = GetConnection();
        var secciones = await conn.QueryAsync<string>(
            @"SELECT DISTINCT ""Seccion"" FROM ""HorarioClases"" ORDER BY ""Seccion"";");
        return secciones.ToList();
    }

    public async Task<List<Docente>> ObtenerDocentesAsync()
    {
        using var conn = GetConnection();
        var docentes = await conn.QueryAsync<Docente>(
            @"SELECT ""Id"", ""NombreCompleto"", ""Turno"" FROM ""Docentes"" ORDER BY ""NombreCompleto"";");
        return docentes.ToList();
    }

    public async Task<bool> HayDatosCargadosAsync()
    {
        using var conn = GetConnection();
        var count = await conn.ExecuteScalarAsync<int>(@"SELECT COUNT(*) FROM ""Docentes"";");
        return count > 0;
    }

    /// <summary>
    /// Consulta 1: dado una sección, día y hora -> qué docente y qué área tiene clase ahí.
    /// </summary>
    public async Task<ResultadoConsulta> ConsultarPorSeccionAsync(string seccion, string dia, TimeSpan hora)
    {
        var horaTexto = hora.ToString(@"hh\:mm");

        using var conn = GetConnection();
        var fila = await conn.QueryFirstOrDefaultAsync(
            @"SELECT d.""NombreCompleto"" AS ""Docente"", d.""Turno"" AS ""Turno"", h.""Area"" AS ""Area"",
                     h.""Seccion"" AS ""Seccion"", h.""HoraInicio"" AS ""HoraInicio"", h.""HoraFin"" AS ""HoraFin""
              FROM ""HorarioClases"" h
              INNER JOIN ""Docentes"" d ON d.""Id"" = h.""DocenteId""
              WHERE h.""Seccion"" = @seccion
                AND h.""Dia"" = @dia
                AND @horaTexto >= h.""HoraInicio""
                AND @horaTexto < h.""HoraFin""
              LIMIT 1;",
            new { seccion, dia, horaTexto });

        if (fila == null)
        {
            return new ResultadoConsulta { Encontrado = false };
        }

        return new ResultadoConsulta
        {
            Encontrado = true,
            Docente = fila.Docente,
            Turno = fila.Turno,
            Area = fila.Area,
            Seccion = fila.Seccion,
            HoraInicio = fila.HoraInicio,
            HoraFin = fila.HoraFin
        };
    }

    /// <summary>
    /// Consulta 2: dado un docente, día y hora -> en qué aula (sección) y qué área está dictando.
    /// </summary>
    public async Task<ResultadoConsulta> ConsultarPorDocenteAsync(int docenteId, string dia, TimeSpan hora)
    {
        var horaTexto = hora.ToString(@"hh\:mm");

        using var conn = GetConnection();
        var fila = await conn.QueryFirstOrDefaultAsync(
            @"SELECT d.""NombreCompleto"" AS ""Docente"", d.""Turno"" AS ""Turno"", h.""Area"" AS ""Area"",
                     h.""Seccion"" AS ""Seccion"", h.""HoraInicio"" AS ""HoraInicio"", h.""HoraFin"" AS ""HoraFin""
              FROM ""HorarioClases"" h
              INNER JOIN ""Docentes"" d ON d.""Id"" = h.""DocenteId""
              WHERE h.""DocenteId"" = @docenteId
                AND h.""Dia"" = @dia
                AND @horaTexto >= h.""HoraInicio""
                AND @horaTexto < h.""HoraFin""
              LIMIT 1;",
            new { docenteId, dia, horaTexto });

        if (fila == null)
        {
            return new ResultadoConsulta { Encontrado = false };
        }

        return new ResultadoConsulta
        {
            Encontrado = true,
            Docente = fila.Docente,
            Turno = fila.Turno,
            Area = fila.Area,
            Seccion = fila.Seccion,
            HoraInicio = fila.HoraInicio,
            HoraFin = fila.HoraFin
        };
    }
}
