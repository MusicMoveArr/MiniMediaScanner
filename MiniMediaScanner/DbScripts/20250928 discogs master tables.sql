--master tables
CREATE TABLE public.discogs_master (
    Id int8 NOT NULL,
    MainReleaseId int8 NOT NULL,
    Year int8 NOT NULL,
    Title text NOT NULL,
    DataQuality text NOT NULL,
    lastsynctime timestamp DEFAULT current_timestamp,
    CONSTRAINT discogs_master_pkey PRIMARY KEY (Id, MainReleaseId)
);

CREATE TABLE public.discogs_master_artist (
    MasterId int8 NOT NULL,
    ArtistId int8 NOT NULL,
    JoinText text NOT NULL,
    SortIndex int8 NOT NULL,
    CONSTRAINT discogs_master_artist_pkey PRIMARY KEY (MasterId, ArtistId)
);

CREATE TABLE public.discogs_master_genre (
    MasterId int8 NOT NULL,
    Genre text NOT NULL,
    CONSTRAINT discogs_master_genre_pkey PRIMARY KEY (MasterId, Genre)
);

CREATE TABLE public.discogs_master_style (
    MasterId int8 NOT NULL,
    Style text NOT NULL,
    CONSTRAINT discogs_master_style_pkey PRIMARY KEY (MasterId, Style)
);

CREATE TABLE public.discogs_master_video (
    MasterId int8 NOT NULL,
    Duration text NOT NULL,
    Embed bool NOT NULL,
    Source text NOT NULL,
    Title text NOT NULL,
    Description text NOT NULL,
    CONSTRAINT discogs_master_video_pkey PRIMARY KEY (MasterId, Source)
);

--indices
CREATE INDEX idx_discogs_master_title_lower_trgm ON discogs_master USING gin (lower(Title) gin_trgm_ops);
CREATE INDEX idx_discogs_master_id ON discogs_master (Id);
CREATE INDEX idx_discogs_master_masterreleaseid ON discogs_master (MainReleaseId);

CREATE INDEX idx_discogs_master_artist_artistid ON discogs_master_artist (ArtistId);
CREATE INDEX idx_discogs_master_artist_masterid ON discogs_master_artist (MasterId);

CREATE INDEX idx_discogs_master_genre_masterid ON discogs_master_genre (MasterId);

CREATE INDEX idx_discogs_master_style_masterid ON discogs_master_style (MasterId);

CREATE INDEX idx_discogs_master_video_releaseid ON discogs_master_video (MasterId);
