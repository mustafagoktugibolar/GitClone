namespace GitClone.Cli;

internal static class CommandErrorMapper
{
    public static int MapExitCode(Exception ex)
    {
        return ex switch
        {
            ArgumentException => 2,
            FileNotFoundException => 2,
            InvalidOperationException => 3,
            _ => 1
        };
    }
}
