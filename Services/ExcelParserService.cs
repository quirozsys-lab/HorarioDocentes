using ClosedXML.Excel;
using HorarioDocentes.Data;

namespace HorarioDocentes.Services;

public class ParseResult
{
    public List<DocenteParseado> Docentes { get; set; } = new();
    public List<string> Advertencias { get; set; } = new();
}

public class DocenteParseado
{
    public string NombreCompleto { get; set; } = string.Empty;
    public string Turno { get; set; } = string.Empty;
    public List<HorarioClase> Clases { get; set; } = new();
}

public class ExcelParserService
{
    private static readonly string[] Dias = { "LUNES", "MARTES", "MIERCOLES", "JUEVES", "VIERNES", "SABADO", "DOMINGO" };

    // Estructura esperada, repetida en bloques de 11 filas por docente:
    // Fila 1: [ , "DOCENTE", NombreCompleto, , , , "TURNO", Turno ]
    // Fila 2: (vacía)
    // Fila 3: [ "HORA", "LUNES", "MARTES", ... "DOMINGO" ]
    // Filas 4-11: [ "HH:MM - HH:MM", clase_lunes, clase_martes, ... ]  (8 franjas horarias)
    public ParseResult Parsear(Stream excelStream)
    {
        var resultado = new ParseResult();

        using var workbook = new XLWorkbook(excelStream);
        var ws = workbook.Worksheet(1);
        var filasUsadas = ws.LastRowUsed()?.RowNumber() ?? 0;

        int fila = 1;
        while (fila <= filasUsadas)
        {
            var celdaEtiqueta = ws.Cell(fila, 2).GetString().Trim(); // Columna B

            if (!celdaEtiqueta.Equals("DOCENTE", StringComparison.OrdinalIgnoreCase))
            {
                fila++;
                continue;
            }

            var nombreDocente = ws.Cell(fila, 3).GetString().Trim();      // Columna C
            var turno = ws.Cell(fila, 8).GetString().Trim();              // Columna H

            if (string.IsNullOrWhiteSpace(nombreDocente))
            {
                resultado.Advertencias.Add($"Fila {fila}: se encontró 'DOCENTE' pero sin nombre. Bloque omitido.");
                fila++;
                continue;
            }

            var docente = new DocenteParseado
            {
                NombreCompleto = nombreDocente,
                Turno = string.IsNullOrWhiteSpace(turno) ? "SIN TURNO" : turno.ToUpperInvariant()
            };

            // La fila de encabezado de días debería estar 2 filas más abajo (fila+2),
            // pero por seguridad la buscamos dentro de las 3 filas siguientes.
            int filaHeaderDias = -1;
            for (int f = fila + 1; f <= Math.Min(fila + 3, filasUsadas); f++)
            {
                if (ws.Cell(f, 1).GetString().Trim().Equals("HORA", StringComparison.OrdinalIgnoreCase))
                {
                    filaHeaderDias = f;
                    break;
                }
            }

            if (filaHeaderDias == -1)
            {
                resultado.Advertencias.Add($"Docente '{nombreDocente}' (fila {fila}): no se encontró encabezado 'HORA'. Bloque omitido.");
                fila++;
                continue;
            }

            // A partir de filaHeaderDias + 1, leemos filas de horas hasta encontrar
            // una fila vacía en columna A o el siguiente bloque "DOCENTE".
            int filaHora = filaHeaderDias + 1;
            while (filaHora <= filasUsadas)
            {
                var rangoHora = ws.Cell(filaHora, 1).GetString().Trim();
                var siguienteEtiqueta = ws.Cell(filaHora, 2).GetString().Trim();

                if (string.IsNullOrWhiteSpace(rangoHora) ||
                    siguienteEtiqueta.Equals("DOCENTE", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                if (TryParseRangoHora(rangoHora, out var horaInicio, out var horaFin))
                {
                    for (int d = 0; d < Dias.Length; d++)
                    {
                        var col = 2 + d; // Columna B=2 (LUNES) ... H=8 (DOMINGO)
                        var contenido = ws.Cell(filaHora, col).GetString().Trim();

                        if (string.IsNullOrWhiteSpace(contenido) ||
                            contenido.Equals("LIBRE", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var (seccion, area) = SepararSeccionYArea(contenido);
                        if (seccion == null || area == null)
                        {
                            resultado.Advertencias.Add(
                                $"Docente '{nombreDocente}', {Dias[d]} {rangoHora}: no se pudo interpretar la celda '{contenido.Replace("\n", " / ")}'.");
                            continue;
                        }

                        docente.Clases.Add(new HorarioClase
                        {
                            Dia = Dias[d],
                            HoraInicio = horaInicio,
                            HoraFin = horaFin,
                            Seccion = seccion,
                            Area = area
                        });
                    }
                }
                else
                {
                    resultado.Advertencias.Add($"Docente '{nombreDocente}', fila {filaHora}: rango de hora '{rangoHora}' no reconocido.");
                }

                filaHora++;
            }

            resultado.Docentes.Add(docente);
            fila = filaHora; // continuamos después del bloque de este docente
        }

        return resultado;
    }

    private static (string? seccion, string? area) SepararSeccionYArea(string contenido)
    {
        // El contenido viene como "TERCERO - C\nMATEMÁTICA" (a veces el área ocupa 2 líneas
        // por wrap, ej: "CIENCIA Y\nTECNOLOGÍA", así que unimos todo lo que va después
        // de la primera línea).
        var partes = contenido.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (partes.Length < 2)
        {
            return (null, null);
        }

        var seccion = partes[0].Trim();
        var area = string.Join(" ", partes.Skip(1)).Trim();

        return (seccion, area);
    }

    private static bool TryParseRangoHora(string rango, out TimeSpan inicio, out TimeSpan fin)
    {
        inicio = TimeSpan.Zero;
        fin = TimeSpan.Zero;

        var partes = rango.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (partes.Length != 2)
        {
            return false;
        }

        return TimeSpan.TryParse(partes[0], out inicio) && TimeSpan.TryParse(partes[1], out fin);
    }
}
