using System.Data;
using Npgsql;

namespace MiniMediaScanner.Repositories;

public abstract class BaseUpdateRepository
{
    private readonly string _connectionString;
    protected NpgsqlConnection Connection { get; private set; }
    protected NpgsqlTransaction Transaction { get; private set; }
    
    public BaseUpdateRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task SetConnectionAsync()
    {
        if (this.Connection?.State != ConnectionState.Open)
        {
            NpgsqlConnection connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            this.Connection = connection;
        }
        
        this.Transaction = await this.Connection.BeginTransactionAsync();
    }
    
    public async Task CommitAsync()
    {
        try
        {
            await this.Transaction.CommitAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
        finally
        {
            if (this.Connection?.State == ConnectionState.Open)
            {
                await this.Connection.CloseAsync();
            }
        }
    }

    public async Task RollbackAsync()
    {
        try
        {
            await this.Transaction.RollbackAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
        finally
        {
            if (this.Connection?.State == ConnectionState.Open)
            {
                await this.Connection.CloseAsync();
            }
        }
    }
}