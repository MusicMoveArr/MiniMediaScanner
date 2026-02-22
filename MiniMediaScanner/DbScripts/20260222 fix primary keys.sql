DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_index
        WHERE indrelid = 'tidal_track'::regclass
          AND indisprimary
    ) THEN
        ALTER TABLE tidal_track ADD PRIMARY KEY (TrackId, AlbumId);
        ALTER TABLE tidal_track DROP CONSTRAINT tidal_track_unique;
    END IF;
END
$$;

    
          
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_index
        WHERE indrelid = 'musicbrainz_artist'::regclass
          AND indisprimary
    ) THEN
        ALTER TABLE musicbrainz_artist ADD PRIMARY KEY (ArtistId);
        ALTER TABLE musicbrainz_artist DROP CONSTRAINT musicbrainzartist_musicbrainzremoteid_key;
    END IF;
END
$$;
      
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_index
        WHERE indrelid = 'musicbrainz_release'::regclass
          AND indisprimary
    ) THEN
        ALTER TABLE musicbrainz_release ADD PRIMARY KEY (ReleaseId, ArtistId);
        ALTER TABLE musicbrainz_release DROP CONSTRAINT musicbrainz_release_artist_release;
    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_index
        WHERE indrelid = 'spotify_album'::regclass
          AND indisprimary
    ) THEN
        ALTER TABLE spotify_album ADD PRIMARY KEY (AlbumId, ArtistId);
        ALTER TABLE spotify_album DROP CONSTRAINT spotify_album_pkey; --somehow already called pkey...
    END IF;
END
$$;