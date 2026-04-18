using Npgsql;
using Dapper;
using DapperBulkQueries.Common;
using DapperBulkQueries.Npgsql;
using MiniMediaScanner.Enums;
using MiniMediaScanner.Helpers;
using MiniMediaScanner.Models;

namespace MiniMediaScanner.Repositories;

public class ArtistRepository
{
    private readonly string _connectionString;
    public ArtistRepository(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public async Task<List<string>> GetAllArtistNamesAsync()
    {
        string query = @"SELECT name FROM artists order by name asc";
        await using var conn = new NpgsqlConnection(_connectionString);

        return (await conn
            .QueryAsync<string>(query))
            .ToList();
    }
    public async Task<List<string>> GetAllArtistNamesLowercaseUniqueAsync()
    {
        string query = @"SELECT distinct lower(name) FROM artists order by lower(name) asc";
        await using var conn = new NpgsqlConnection(_connectionString);

        return (await conn
                .QueryAsync<string>(query))
            .ToList();
    }
    
    public async Task<List<ArtistModel>> GetMatchingArtistAsync(string artistName)
    {
        string query = @"SET LOCAL pg_trgm.similarity_threshold = 0.95;
                         SELECT a.*, ext.*
                         FROM artists a
                         LEFT JOIN artists_ext ext ON ext.ArtistId = a.ArtistId
                         where LOWER(a.name) % lower(@artistName)
                         order by similarity(LOWER(a.name), lower(@artistName)) desc";
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        var transaction = await conn.BeginTransactionAsync();
        var result = new List<ArtistModel>();

        try
        {
            result = (await conn
                    .QueryAsync<ArtistModel, ArtistExtModel, ArtistModel>(query,
                        (artist, ext) =>
                        {
                            if (ext?.ArtistId.HasValue == true && ext.ArtistId != Guid.Empty)
                            {
                                artist.ExtArtists.Add(ext);
                            }
                            return artist;
                        },
                        splitOn: "ArtistId, ArtistId",
                        param: new
                        {
                            artistName
                        }))
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message + "\r\n" + ex.StackTrace);
        }
        finally
        {
            await transaction.CommitAsync();
        }
        
        return result
            .GroupBy(artist => artist.ArtistId)
            .Select(group =>
            {
                var artist = group.First();
                artist.ExtArtists = group
                    .SelectMany(a => a.ExtArtists)
                    .ToList();
                return artist;
            })
            .ToList();
    }


    public async Task BulkInsertArtistExtAsync(List<ArtistExtModel> extArtists)
    {
        if (extArtists.Count == 0)
        {
            return;
        }
        
        List<string> columns = new()
        {
            "ArtistId", 
            "ExtArtistId", 
            "Provider"
        };
        
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.ExecuteBulkInsertAsync(
            "artists_ext",
            extArtists,
            columns,
            onConflict: OnConflict.DoNothing);
    }
    
    public async Task<Guid?> GetArtistIdByNameAsync(string artistName)
    {
        string query = @"SET LOCAL pg_trgm.similarity_threshold = 0.95;
                         SELECT ArtistId 
                         FROM Artists a
                         where LOWER(a.name) % lower(@artistName)
                         order by similarity(LOWER(a.name), lower(@artistName)) desc";
        
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var transaction = await conn.BeginTransactionAsync();
        Guid? result = null;

        try
        {
            result = await conn.ExecuteScalarAsync<Guid>(query, new
            {
                artistName
            }, transaction: transaction);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message + "\r\n" + e.StackTrace);
        }
        finally
        {
            await transaction.CommitAsync();
        }
        
        return result;
    }
    
    public async Task<Guid> InsertOrFindArtistAsync(string artistName)
    {
        Guid? foundArtistId = await GetArtistIdByNameAsync(artistName);

        if (GuidHelper.GuidHasValue(foundArtistId))
        {
            return foundArtistId!.Value;
        }
        return await InsertArtistAsync(artistName);
    }
    
    public async Task<Guid> InsertArtistAsync(string artistName)
    {
        string query = @"INSERT INTO Artists (ArtistId, Name)
                         VALUES (@id, @name)";
        
        Guid artistId = Guid.NewGuid();
        await using var conn = new NpgsqlConnection(_connectionString);

        await conn.ExecuteAsync(query, new
        {
            id = artistId,
            name = artistName
        });
        return artistId;
    }
    
    public async Task<List<ArtistModel>> GetArtistByProviderIdAsync(string artistName, List<ArtistExtModel> artistExtModels)
    {
        string query = @"select a.*, ext_deezer.*
                         from artists a
                         join artists_ext ext_deezer on 
	                         ext_deezer.ArtistId = a.artistid 
	                         and ext_deezer.Provider = 'Deezer' 
	                         and ext_deezer.ExtArtistId = @deezerId
	                         
                         union
 
                         select a.*, ext_musicbrainz.*
                         from artists a
                         join artists_ext ext_musicbrainz on 
	                         ext_musicbrainz.ArtistId = a.artistid 
	                         and ext_musicbrainz.Provider = 'MusicBrainz' 
	                         and ext_musicbrainz.ExtArtistId = @musicBrainzId
	                         
                         union
 
                         select a.*, ext_spotify.*
                         from artists a
                         join artists_ext ext_spotify on 
	                         ext_spotify.ArtistId = a.artistid 
	                         and ext_spotify.Provider = 'Spotify' 
	                         and ext_spotify.ExtArtistId = @spotifyId
                         
                         union
 
                         select a.*, ext_discogs.*
                         from artists a
                         join artists_ext ext_discogs on 
	                         ext_discogs.ArtistId = a.artistid 
	                         and ext_discogs.Provider = 'Discogs' 
	                         and ext_discogs.ExtArtistId = @discogsId
                         
                         union
 
                         select a.*, ext_soundcloud.*
                         from artists a
                         join artists_ext ext_soundcloud on 
	                         ext_soundcloud.ArtistId = a.artistid 
	                         and ext_soundcloud.Provider = 'Soundcloud' 
	                         and ext_soundcloud.ExtArtistId = @soundcloudId
	                         
                         union
 
                         select a.*, ext_tidal.*
                         from artists a
                         join artists_ext ext_tidal on 
	                         ext_tidal.ArtistId = a.artistid 
	                         and ext_tidal.Provider = 'Tidal' 
	                         and ext_tidal.ExtArtistId = @tidalId";
        
        await using var conn = new NpgsqlConnection(_connectionString);

        return (await conn
                .QueryAsync<ArtistModel, ArtistExtModel, ArtistModel>(query,
                    (artist, ext) =>
                    {
                        if (ext?.ArtistId.HasValue == true && ext.ArtistId != Guid.Empty)
                        {
                            artist.ExtArtists.Add(ext);
                        }

                        return artist;
                    },
                    splitOn: "ArtistId, ArtistId",
                    param: new
                    {
                        deezerId = artistExtModels.FirstOrDefault(ext => ext.Provider == nameof(ProviderName.Deezer))?.ExtArtistId,
                        musicBrainzId = artistExtModels.FirstOrDefault(ext => ext.Provider == nameof(ProviderName.MusicBrainz))?.ExtArtistId,
                        spotifyId = artistExtModels.FirstOrDefault(ext => ext.Provider == nameof(ProviderName.Spotify))?.ExtArtistId,
                        discogsId = artistExtModels.FirstOrDefault(ext => ext.Provider == nameof(ProviderName.Discogs))?.ExtArtistId,
                        soundcloudId = artistExtModels.FirstOrDefault(ext => ext.Provider == nameof(ProviderName.Soundcloud))?.ExtArtistId,
                        tidalId = artistExtModels.FirstOrDefault(ext => ext.Provider == nameof(ProviderName.Tidal))?.ExtArtistId
                    }))
            .ToList();

    }
}