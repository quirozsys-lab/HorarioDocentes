namespace HorarioDocentes.Services;

public static class PeruTime
{
    // Perú está en UTC-5 todo el año (no usa horario de verano),
    // así que un offset fijo es más confiable que depender de la
    // zona horaria configurada en el sistema operativo del contenedor
    // (Railway/Docker corren en UTC por defecto).
    private static readonly TimeSpan Offset = TimeSpan.FromHours(-5);

    public static DateTime Ahora() => DateTime.UtcNow + Offset;
}
