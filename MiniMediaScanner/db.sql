CREATE TABLE public.artists (
    artistid uuid NOT NULL,
    "name" text NULL,
    CONSTRAINT artists_name_key UNIQUE (name),
    CONSTRAINT artists_pkey PRIMARY KEY (artistid)
);
CREATE INDEX idx_artists_lower_name ON public.artists USING btree (lower(name));

CREATE TABLE public.albums (
    albumid uuid NOT NULL,
    title text NULL,
    artistid uuid NULL,
    CONSTRAINT albums_pkey PRIMARY KEY (albumid),
    CONSTRAINT albums_unique UNIQUE (title, artistid)
);
CREATE INDEX albums_artistid_idx ON public.albums USING btree (artistid, title);
CREATE INDEX idx_albums_artistid_title ON public.albums USING btree (artistid, lower(title));

CREATE TABLE public.metadata (
    metadataid uuid NOT NULL,
    "path" text NULL,
    title text NULL,
    albumid uuid NULL,
    musicbrainzartistid text NULL,
    musicbrainzdiscid text NULL,
    musicbrainzreleasecountry text NULL,
    musicbrainzreleaseid text NULL,
    musicbrainztrackid text NULL,
    musicbrainzreleasestatus text NULL,
    musicbrainzreleasetype text NULL,
    musicbrainzreleaseartistid text NULL,
    musicbrainzreleasegroupid text NULL,
    tag_subtitle text NULL,
    tag_albumsort text NULL,
    tag_comment text NULL,
    tag_year int4 DEFAULT 0 NULL,
    tag_track int4 DEFAULT 0 NULL,
    tag_trackcount int4 DEFAULT 0 NULL,
    tag_disc int4 DEFAULT 0 NULL,
    tag_disccount int4 DEFAULT 0 NULL,
    tag_lyrics text NULL,
    tag_grouping text NULL,
    tag_beatsperminute int4 DEFAULT 0 NULL,
    tag_conductor text NULL,
    tag_copyright text NULL,
    tag_datetagged timestamp NULL,
    tag_amazonid text NULL,
    tag_replaygaintrackgain float8 DEFAULT 0 NULL,
    tag_replaygaintrackpeak float8 DEFAULT 0 NULL,
    tag_replaygainalbumgain float8 DEFAULT 0 NULL,
    tag_replaygainalbumpeak float8 DEFAULT 0 NULL,
    tag_initialkey text NULL,
    tag_remixedby text NULL,
    tag_publisher text NULL,
    tag_isrc text NULL,
    tag_length text NULL,
    tag_acoustidfingerprint text NULL,
    tag_acoustid text NULL,
    file_lastwritetime timestamp DEFAULT '1999-01-08 00:00:00'::timestamp without time zone NULL,
    file_creationtime timestamp DEFAULT '1999-01-08 00:00:00'::timestamp without time zone NULL,
    tag_acoustidfingerprint_duration float8 DEFAULT 0 NULL,
    tag_alljsontags jsonb NULL,
    CONSTRAINT metadata_path_key UNIQUE (path),
    CONSTRAINT metadata_pkey PRIMARY KEY (metadataid)
);
CREATE INDEX idx_metadata_trackid_title_albumid ON public.metadata USING btree (musicbrainztrackid, lower(title), albumid);
CREATE INDEX metadata_musicbrainztrackid_idx ON public.metadata USING btree (musicbrainztrackid, albumid, title);
CREATE INDEX metadata_title_idx ON public.metadata USING btree (title, albumid);

CREATE TABLE public.musicbrainzartist (
    musicbrainzartistid uuid NOT NULL,
    musicbrainzremoteid uuid NULL,
    releasecount int4 NULL,
    "name" text NULL,
    "type" text NULL,
    country text NULL,
    sortname text NULL,
    disambiguation text NULL,
    CONSTRAINT musicbrainzartist_musicbrainzremoteid_key UNIQUE (musicbrainzremoteid),
    CONSTRAINT musicbrainzartist_pkey PRIMARY KEY (musicbrainzartistid)
);
CREATE INDEX idx_musicbrainzartist_id ON public.musicbrainzartist USING btree (musicbrainzartistid);
CREATE INDEX musicbrainzartist_name_idx ON public.musicbrainzartist USING btree (name);
CREATE INDEX musicbrainzartist_name_lowercase_idx ON public.musicbrainzartist USING btree (lower(name));

CREATE TABLE public.musicbrainzrelease (
    musicbrainzreleaseid uuid NOT NULL,
    musicbrainzartistid uuid NULL,
    musicbrainzremotereleaseid uuid NULL,
    title text NULL,
    status text NULL,
    statusid text NULL,
    "date" text NULL,
    barcode text NULL,
    country text NULL,
    disambiguation text NULL,
    quality text NULL,
    CONSTRAINT musicbrainzrelease_musicbrainzremotereleaseid_key1 UNIQUE (musicbrainzremotereleaseid),
    CONSTRAINT musicbrainzrelease_pkey1 PRIMARY KEY (musicbrainzreleaseid)
);
CREATE INDEX idx_musicbrainzrelease_artist_country_status ON public.musicbrainzrelease USING btree (musicbrainzartistid, lower(country), lower(status));
CREATE INDEX musicbrainzrelease_title_idx ON public.musicbrainzrelease USING btree (title);


CREATE TABLE public.musicbrainzreleasetrack (
    musicbrainzreleasetrackid uuid NOT NULL,
    musicbrainzremotereleasetrackid uuid NULL,
    musicbrainzremoterecordingtrackid uuid NULL,
    title text NULL,
    status text NULL,
    statusid text NULL,
    musicbrainzremotereleaseid uuid NULL,
    CONSTRAINT musicbrainzrelease_musicbrainzremotereleaseid_key UNIQUE (musicbrainzremotereleasetrackid),
    CONSTRAINT musicbrainzrelease_pkey PRIMARY KEY (musicbrainzreleasetrackid)
);

CREATE INDEX idx_musicbrainzreleasetrack_remotereleaseid ON public.musicbrainzreleasetrack USING btree (musicbrainzremotereleaseid);
CREATE INDEX musicbrainzreleasetrack_musicbrainzremotereleaseid_idx ON public.musicbrainzreleasetrack USING btree (musicbrainzremotereleaseid);

alter table musicbrainzreleasetrack add column length int default 0;
alter table musicbrainzreleasetrack add column number int default 0;
alter table musicbrainzreleasetrack add column position int default 0;
alter table musicbrainzreleasetrack add column recordingid uuid null;
alter table musicbrainzreleasetrack add column recordinglength int default 0;
alter table musicbrainzreleasetrack add column recordingtitle text null;
alter table musicbrainzreleasetrack add column recordingvideo bool default false;
alter table musicbrainzreleasetrack add column mediatrackcount int default 0;
alter table musicbrainzreleasetrack add column mediaformat text null;
alter table musicbrainzreleasetrack add column mediatitle text null;
alter table musicbrainzreleasetrack add column mediaposition int default 0;
alter table musicbrainzreleasetrack add column mediatrackoffset int default 0;
ALTER TABLE public.musicbrainzartist ADD lastsynctime timestamp DEFAULT current_timestamp;

CREATE INDEX idx_albums_artistid ON albums (artistid);
CREATE INDEX idx_metadata_albumid ON metadata (albumid);

CREATE INDEX idx_metadata_albumfoldername_filename
    ON metadata (albumid, REGEXP_REPLACE(Path, '^.*/([^/]*/[^/]+)$', '\1', 'g'));


CREATE TABLE public.spotify_artist (
    Id text NOT NULL,
    Name text NOT NULL,
    Popularity int NOT NULL,
    Type text NOT NULL,
    Uri text NOT NULL,
    TotalFollowers int NOT NULL,
    Href text NOT NULL,
    Genres text NOT NULL,
    lastsynctime timestamp DEFAULT current_timestamp,
    CONSTRAINT spotify_artist_pkey PRIMARY KEY (Id)
);

CREATE TABLE public.spotify_artist_image (
    ArtistId text NOT NULL,
    Height int NOT NULL,
    Width int NOT NULL,
    Url text NOT NULL,
    CONSTRAINT spotify_artist_image_pkey PRIMARY KEY (ArtistId, Height, Width)
);

CREATE TABLE public.spotify_album (
    AlbumId text NOT NULL,
    AlbumGroup text NOT NULL,
    AlbumType text NOT NULL,
    Name text NOT NULL,
    ReleaseDate text NOT NULL,
    ReleaseDatePrecision text NOT NULL,
    TotalTracks int NOT NULL,
    Type text NOT NULL,
    Uri text NOT NULL,
    Label text NOT NULL,
    Popularity int NOT NULL,
    CONSTRAINT spotify_album_pkey PRIMARY KEY (AlbumId)
);

CREATE TABLE public.spotify_album_image (
    AlbumId text NOT NULL,
    Height int NOT NULL,
    Width int NOT NULL,
    Url text NOT NULL,
    CONSTRAINT spotify_album_image_pkey PRIMARY KEY (AlbumId, Height, Width)
);

CREATE TABLE public.spotify_album_artist (
    AlbumId text NOT NULL,
    ArtistId text NOT NULL,
    Type text NOT NULL,
    CONSTRAINT spotify_album_artist_pkey PRIMARY KEY (AlbumId, ArtistId, Type)
);
CREATE TABLE public.spotify_album_externalid (
    AlbumId text NOT NULL,
    Name text NOT NULL,
    Value text NOT NULL,
    CONSTRAINT spotify_album_externalid_pkey PRIMARY KEY (AlbumId, Name)
);

CREATE TABLE public.spotify_track (
    TrackId text NOT NULL,
    AlbumId text NOT NULL,
    DiscNumber int NOT NULL,
    DurationMs int NOT NULL,
    Explicit bool NOT NULL,
    Href text NOT NULL,
    IsPlayable bool NOT NULL,
    LinkedFrom text NOT NULL,
    Name text NOT NULL,
    PreviewUrl text NOT NULL,
    TrackNumber int NOT NULL,
    Type text NOT NULL,
    Uri text NOT NULL,
    CONSTRAINT spotify_track_pkey PRIMARY KEY (TrackId, AlbumId)
);

CREATE TABLE public.spotify_track_artist (
    TrackId text NOT NULL,
    ArtistId text NOT NULL,
    CONSTRAINT spotify_track_artist_pkey PRIMARY KEY (TrackId, ArtistId)
);
CREATE TABLE public.spotify_track_externalid (
    TrackId text NOT NULL,
    Name text NOT NULL,
    Value text NOT NULL,
    CONSTRAINT spotify_track_externalid_pkey PRIMARY KEY (TrackId, Name)
);


CREATE INDEX idx_spotify_artist_image_artistid ON public.spotify_artist_image (ArtistId);


CREATE INDEX idx_spotify_album_image_albumid ON public.spotify_album_image (AlbumId);

CREATE INDEX idx_spotify_album_artist_albumid ON public.spotify_album_artist (AlbumId);
CREATE INDEX idx_spotify_album_artist_artistid ON public.spotify_album_artist (ArtistId);

CREATE INDEX idx_spotify_album_externalid_albumid ON public.spotify_album_externalid (AlbumId);

CREATE INDEX idx_spotify_track_albumid ON public.spotify_track (AlbumId);

CREATE INDEX idx_spotify_track_artist_trackid ON public.spotify_track_artist (TrackId);
CREATE INDEX idx_spotify_track_artist_artistid ON public.spotify_track_artist (ArtistId);

CREATE INDEX idx_spotify_track_externalid_trackid ON public.spotify_track_externalid (TrackId);


CREATE INDEX idx_spotify_artist_name_lower ON public.spotify_artist (LOWER(name));

CREATE TABLE public.tidal_artist (
    ArtistId int NOT NULL,
    Name text NOT NULL,
    Popularity float8 NOT NULL,
    lastsynctime timestamp DEFAULT current_timestamp,
    CONSTRAINT tidal_artist_pkey PRIMARY KEY (ArtistId)
);
CREATE INDEX idx_tidal_artist_name_lower ON public.tidal_artist (LOWER(name));

CREATE TABLE public.tidal_artist_image_link (
    ArtistId int NOT NULL,
    href text NOT NULL,
    meta_width int NOT NULL,
    meta_height int NOT NULL,
    CONSTRAINT tidal_artist_image_link_pkey PRIMARY KEY (ArtistId, meta_width, meta_height)
);
CREATE INDEX idx_tidal_artist_image_link_artistid ON public.tidal_artist_image_link (ArtistId);

CREATE TABLE public.tidal_artist_external_link (
    ArtistId int NOT NULL,
    href text NOT NULL,
    meta_type text NOT NULL,
    CONSTRAINT tidal_artist_external_link_pkey PRIMARY KEY (ArtistId, href, meta_type)
);
CREATE INDEX idx_tidal_artist_external_link_trackid ON public.tidal_artist_external_link (ArtistId);


CREATE TABLE public.tidal_album (
    AlbumId int NOT NULL,
    ArtistId int NOT NULL,
    Title text NOT NULL,
    BarcodeId text NOT NULL,
    NumberOfVolumes int NOT NULL,
    NumberOfItems int NOT NULL,
    Duration text NOT NULL,
    Explicit bool NOT NULL,
    ReleaseDate text NOT NULL,
    Copyright text NOT NULL,
    Popularity float8 NOT NULL,
    Availability text NOT NULL,
    MediaTags text NOT NULL,
    
    CONSTRAINT tidal_album_pkey PRIMARY KEY (AlbumId, ArtistId)
);
CREATE INDEX idx_tidal_album_id ON public.tidal_album (AlbumId);


CREATE TABLE public.tidal_album_image_link (
    AlbumId int NOT NULL,
    href text NOT NULL,
    meta_width int NOT NULL,
    meta_height int NOT NULL,
    CONSTRAINT tidal_album_image_link_pkey PRIMARY KEY (AlbumId, meta_width, meta_height)
);
CREATE INDEX idx_tidal_album_image_link_ablumid ON public.tidal_album_image_link (AlbumId);

CREATE TABLE public.tidal_album_external_link (
    AlbumId int NOT NULL,
    href text NOT NULL,
    meta_type text NOT NULL,
    CONSTRAINT tidal_album_external_link_pkey PRIMARY KEY (AlbumId, href, meta_type)
);
CREATE INDEX idx_tidal_album_external_link_AlbumId ON public.tidal_album_external_link (AlbumId);


CREATE TABLE public.tidal_track (
    TrackId int NOT NULL,
    AlbumId int NOT NULL,
    Title text NOT NULL,
    ISRC text NOT NULL,
    Duration text NOT NULL,
    Copyright text NOT NULL,
    Explicit bool NOT NULL,
    Popularity float8 NOT NULL,
    Availability text NOT NULL,
    MediaTags text NOT NULL,
    VolumeNumber int NOT NULL,
    TrackNumber int NOT NULL,
    CONSTRAINT tidal_track_pkey PRIMARY KEY (TrackId, AlbumId)
);
CREATE INDEX idx_tidal_track_AlbumId ON public.tidal_track (AlbumId);

CREATE TABLE public.tidal_track_external_link (
    TrackId int NOT NULL,
    href text NOT NULL,
    meta_type text NOT NULL,
    CONSTRAINT tidal_track_external_link_pkey PRIMARY KEY (TrackId, href, meta_type)
);
CREATE INDEX idx_tidal_track_external_link_trackid ON public.tidal_track_external_link (TrackId);


CREATE TABLE public.tidal_track_image_link (
    TrackId int NOT NULL,
    href text NOT NULL,
    meta_width int NOT NULL,
    meta_height int NOT NULL,
    CONSTRAINT tidal_track_image_link_pkey PRIMARY KEY (TrackId, meta_width, meta_height)
);
CREATE INDEX idx_tidal_track_image_link_trackid ON public.tidal_track_image_link (TrackId);


CREATE TABLE public.tidal_provider (
    ProviderId int NOT NULL,
    name text NOT NULL,
    CONSTRAINT tidal_provider_pkey PRIMARY KEY (ProviderId)
);

CREATE TABLE public.tidal_track_artist (
    TrackId int NOT NULL,
    ArtistId int NOT NULL,
    CONSTRAINT tidal_track_artist_pkey PRIMARY KEY (TrackId, ArtistId)
);
CREATE INDEX idx_tidal_track_artist_trackid ON public.tidal_track_artist (TrackId);
CREATE INDEX idx_tidal_track_artist_artistid ON public.tidal_track_artist (ArtistId);


CREATE TABLE public.tidal_track_provider (
    TrackId int NOT NULL,
    ProviderId int NOT NULL,
    CONSTRAINT tidal_track_provider_pkey PRIMARY KEY (TrackId, ProviderId)
);
CREATE INDEX idx_tidal_track_provider_trackid ON public.tidal_track_provider (TrackId);

