CREATE TABLE IF NOT EXISTS public.lastfm_artist (
    ArtistId uuid not null,
    LastFmId text,
    Name text not null,
    OnTour bool NOT NULL default false,
    StatsListeners int NOT NULL default 0,
    MusicBrainzId uuid,
    BioContent text,
    BioSummary text,
    BioYearFormed int,
    BioPublished timestamp,
    Uri text not null default '',
    LastSyncTime timestamp DEFAULT current_timestamp,
    CONSTRAINT lastfm_artist_pkey PRIMARY KEY (Name)
);

CREATE TABLE IF NOT EXISTS public.lastfm_artist_tag (
    ArtistId uuid NOT NULL,
    Name text NOT NULL,
    Count int NOT NULL default 0,
    Reach int NOT NULL default 0,
    RelatedTo text NOT NULL default '',
    Streamable bool NOT NULL default false,
    Uri text NOT NULL default '',
    CONSTRAINT lastfm_artist_tag_pkey PRIMARY KEY (ArtistId, Name)
);
CREATE TABLE IF NOT EXISTS public.lastfm_artist_image (
    ArtistId uuid NOT NULL,
    Uri text NOT NULL default '',
    CONSTRAINT lastfm_artist_image_pkey PRIMARY KEY (ArtistId, Uri)
);
CREATE TABLE IF NOT EXISTS public.lastfm_artist_similar (
    ArtistId uuid NOT NULL,
    SimilarArtistId uuid NOT NULL,
    CONSTRAINT lastfm_artist_similar_pkey PRIMARY KEY (ArtistId, SimilarArtistId)
);

CREATE TABLE IF NOT EXISTS public.lastfm_album (
    ArtistId uuid NOT NULL,
    AlbumId uuid NOT NULL,
    LastFmAlbumId text,
    MusicBrainzId uuid,
    ListenerCount int NOT NULL default 0,
    Name text not null,
    PlayCount int not null,
    ReleaseDateUtc timestamp,
    Url text,
    CONSTRAINT lastfm_album_pkey PRIMARY KEY (ArtistId, Name)
);
CREATE TABLE IF NOT EXISTS public.lastfm_album_image (
    AlbumId uuid NOT NULL,
    Uri text NOT NULL default '',
    CONSTRAINT lastfm_album_image_pkey PRIMARY KEY (AlbumId, Uri)
);

CREATE TABLE IF NOT EXISTS public.lastfm_album_tag (
    AlbumId uuid NOT NULL,
    Name text NOT NULL,
    Count int NOT NULL default 0,
    Reach int NOT NULL default 0,
    RelatedTo text NOT NULL default '',
    Streamable bool NOT NULL default false,
    Uri text NOT NULL default '',
    CONSTRAINT lastfm_album_tag_pkey PRIMARY KEY (AlbumId, Name)
);


CREATE TABLE IF NOT EXISTS public.lastfm_album_track (
    TrackId uuid NOT NULL,
    AlbumId uuid NOT NULL,
    LastFmTrackId text,
    Name text NOT NULL,
    Rank int NOT NULL default 0,
    Duration int NOT NULL default 0,
    MusicBrainzId uuid,
    ListenerCount int NOT NULL default 0,
    PlayCount int NOT NULL default 0,
    Url text NOT NULL default '',
    CONSTRAINT lastfm_album_track_pkey PRIMARY KEY (AlbumId, Name, Rank)
);

CREATE TABLE IF NOT EXISTS public.lastfm_album_track_tag (
    TrackId uuid NOT NULL,
    Name text NOT NULL,
    Count int NOT NULL default 0,
    Reach int NOT NULL default 0,
    RelatedTo text NOT NULL default '',
    Streamable bool NOT NULL default false,
    Url text NOT NULL default '',
    CONSTRAINT lastfm_album_track_tag_pkey PRIMARY KEY (TrackId, Name)
);

CREATE TABLE IF NOT EXISTS public.lastfm_album_track_similar (
    TrackId uuid NOT NULL,
    SimilarTrackId uuid NOT NULL,
    CONSTRAINT lastfm_album_track_similar_pkey PRIMARY KEY (TrackId, SimilarTrackId)
);

CREATE INDEX IF NOT exists idx_lastfm_artist_name_lower_trgm ON lastfm_artist USING gin (lower(name) gin_trgm_ops);
CREATE INDEX IF NOT exists idx_lastfm_album_name_lower_trgm ON lastfm_album USING gin (lower(name) gin_trgm_ops);
CREATE INDEX IF NOT exists idx_lastfm_album_track_name_lower_trgm ON lastfm_album_track USING gin (lower(name) gin_trgm_ops);