namespace HorarioDocentes.Data;

public class HorarioClase
{
    public int Id { get; set; }
    public int DocenteId { get; set; }
    public string Dia { get; set; } = string.Empty;       // LUNES..DOMINGO
    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFin { get; set; }
    public string Seccion { get; set; } = string.Empty;   // Ej: "TERCERO - C"
    public string Area { get; set; } = string.Empty;      // Ej: "MATEMÁTICA"

    // Se guardan en Postgres como texto "HH:mm" (varchar) para evitar el mapeo
    // TimeSpan -> interval de Npgsql, que no compara bien contra columnas "time".
    public string HoraInicioTexto => HoraInicio.ToString(@"hh\:mm");
    public string HoraFinTexto => HoraFin.ToString(@"hh\:mm");

    // Campos que llegan por JOIN en las consultas (no se guardan directo)
    public string? NombreDocente { get; set; }
    public string? Turno { get; set; }
}

// Resultado de las consultas "en este momento" / "en este dia y hora"
public class ResultadoConsulta
{
    public bool Encontrado { get; set; }
    public string? Docente { get; set; }
    public string? Seccion { get; set; }
    public string? Area { get; set; }
    public string? Turno { get; set; }
    public string? HoraInicio { get; set; }
    public string? HoraFin { get; set; }
}
