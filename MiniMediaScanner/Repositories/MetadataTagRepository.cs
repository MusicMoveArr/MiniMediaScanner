using System.Reflection.Metadata;
using System.Text;
using MiniMediaScanner.Models;
using Npgsql;
using Dapper;

namespace MiniMediaScanner.Repositories;

public class MetadataTagRepository
{
    //prevents double tag values that is already stored in Metadata table
    private readonly string[] _ignoreTags =
    [
        "Path"
    ];
    
    private readonly string _connectionString;

    public MetadataTagRepository(string connectionString)
    {
        _connectionString = connectionString;
    }


    public void InsertOrUpdateMetadataTag(MetadataInfo metadataInfo)
    {
        Dictionary<string, string>? allTags = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(metadataInfo.Tag_AllJsonTags);

        if (allTags == null)
        {
            return;
        }
        
        using var conn = new NpgsqlConnection(_connectionString);
        foreach (var tag in allTags.Where(tag => !_ignoreTags.Contains(tag.Key)))
        {
            try
            {
                InsertOrUpdateMetadataTag(conn, metadataInfo.MetadataId, tag.Key, tag.Value);
            }
            catch (Exception e)
            {
                //sometimes just 1 tag fails because of unicode characters...
                Console.WriteLine(e.Message);
            }
        }
    }
    
    public void InsertOrUpdateMetadataTag(MetadataModel metadataInfo)
    {
        Dictionary<string, string>? allTags = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(metadataInfo.Tag_AllJsonTags);

        if (allTags == null)
        {
            return;
        }
        
        using var conn = new NpgsqlConnection(_connectionString);
        foreach (var tag in allTags
                     .Where(tag => !_ignoreTags.Contains(tag.Key)))
        {
            try
            {
                InsertOrUpdateMetadataTag(conn, metadataInfo.MetadataId.Value, tag.Key, tag.Value);
            }
            catch (Exception e)
            {
                //sometimes just 1 tag fails because of unicode characters...
                Console.WriteLine(e.Message);
            }
        }
    }

    public void InsertOrUpdateMetadataTag(NpgsqlConnection conn, Guid metadataId, string name, string value)
    {
        string query = @"
            INSERT INTO Metadata_Tag (MetadataId, 
                                  Name, 
                                  Value)
            VALUES (@MetadataId, @Name, @Value)
            ON CONFLICT (MetadataId, Name)
            DO UPDATE SET
                Value = EXCLUDED.Value";

        conn.Execute(query, param: new
        {
            MetadataId = metadataId, 
            Name = name, 
            Value = value
        });
    }
}