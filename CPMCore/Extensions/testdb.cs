using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

public class SqlDebugInterceptor : DbCommandInterceptor
{
    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        Log(command);
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        Log(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        Log(command);
        return base.ScalarExecuting(command, eventData, result);
    }

    private void Log(DbCommand command)
    {
        if (command.CommandText.Contains("$"))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("🚨 SQL MET $ GEVONDEN:");
            Console.WriteLine(command.CommandText);
            Console.ResetColor();
        }
    }
}