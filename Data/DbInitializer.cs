using Npgsql;

namespace HorarioDocentes.Data;

public static class DbInitializer
{
    public static void Initialize(string connectionString)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS ""Docentes"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""NombreCompleto"" VARCHAR(200) NOT NULL,
                ""Turno"" VARCHAR(20) NOT NULL
            );

            CREATE TABLE IF NOT EXISTS ""HorarioClases"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""DocenteId"" INT NOT NULL REFERENCES ""Docentes""(""Id"") ON DELETE CASCADE,
                ""Dia"" VARCHAR(20) NOT NULL,
                ""HoraInicio"" VARCHAR(5) NOT NULL,
                ""HoraFin"" VARCHAR(5) NOT NULL,
                ""Seccion"" VARCHAR(50) NOT NULL,
                ""Area"" VARCHAR(150) NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_horarioclases_seccion ON ""HorarioClases"" (""Seccion"", ""Dia"");
            CREATE INDEX IF NOT EXISTS idx_horarioclases_docente ON ""HorarioClases"" (""DocenteId"", ""Dia"");

            CREATE TABLE IF NOT EXISTS ""CargasExcel"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""NombreArchivo"" VARCHAR(255) NOT NULL,
                ""FechaCarga"" TIMESTAMP NOT NULL DEFAULT NOW(),
                ""TotalDocentes"" INT NOT NULL,
                ""TotalClases"" INT NOT NULL
            );";
        cmd.ExecuteNonQuery();
    }
}
