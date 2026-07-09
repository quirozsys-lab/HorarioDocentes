namespace HorarioDocentes.Data;

public class Docente
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Turno { get; set; } = string.Empty; // MAÑANA / TARDE
}
