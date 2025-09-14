--artist tables
CREATE TABLE public.discogs_artist (
    ArtistId int8 NOT NULL,
    Name text NOT NULL,
    RealName text NOT NULL,
    Profile text NOT NULL,
    DataQuality text NOT NULL,
    lastsynctime timestamp DEFAULT current_timestamp,
    CONSTRAINT discogs_artist_pkey PRIMARY KEY (ArtistId)
);

CREATE TABLE public.discogs_artist_url (
    ArtistId int8 NOT NULL,
    Url text NOT NULL,
    CONSTRAINT discogs_artist_url_pkey PRIMARY KEY (ArtistId, Url)
);

CREATE TABLE public.discogs_alias (
    Id int8 NOT NULL,
    Name text NOT NULL,
    CONSTRAINT discogs_alias_pkey PRIMARY KEY (Id)
);

CREATE TABLE public.discogs_artist_alias (
    ArtistId int8 NOT NULL,
    AliasId int8 NOT NULL,
    CONSTRAINT discogs_artist_alias_pkey PRIMARY KEY (ArtistId, AliasId)
);

--label tables
CREATE TABLE public.discogs_label (
    LabelId int8 NOT NULL,
    Name text NOT NULL,
    ContactInfo text NOT NULL,
    Profile text NOT NULL,
    DataQuality text NOT NULL,
    CONSTRAINT discogs_label_pkey PRIMARY KEY (LabelId)
);
CREATE TABLE public.discogs_label_url (
    LabelId int8 NOT NULL,
    Url text NOT NULL,
    CONSTRAINT discogs_label_url_pkey PRIMARY KEY (LabelId, Url)
);
CREATE TABLE public.discogs_label_sublabel (
    LabelId int8 NOT NULL,
    SubLabelId int8 NOT NULL,
    CONSTRAINT discogs_label_sublabel_pkey PRIMARY KEY (LabelId, SubLabelId)
);
CREATE TABLE public.discogs_sublabel (
    LabelId int8 NOT NULL,
    Name text NOT NULL,
    CONSTRAINT discogs_sublabel_pkey PRIMARY KEY (LabelId, Name)
);

--release tables
CREATE TABLE public.discogs_release (
    ReleaseId int8 NOT NULL,
    Status text NOT NULL,
    Title text NOT NULL,
    Country text NOT NULL,
    Released text NOT NULL,
    Notes text NOT NULL,
    DataQuality text NOT NULL,
    IsMainRelease bool NOT NULL,
    MasterId int8 NOT NULL,
    CONSTRAINT discogs_release_pkey PRIMARY KEY (ReleaseId, MasterId)
);

CREATE TABLE public.discogs_release_genre (
    ReleaseId int8 NOT NULL,
    Genre text NOT NULL,
    CONSTRAINT discogs_release_genre_pkey PRIMARY KEY (ReleaseId, Genre)
);

CREATE TABLE public.discogs_release_style (
    ReleaseId int8 NOT NULL,
    Style text NOT NULL,
    CONSTRAINT discogs_release_style_pkey PRIMARY KEY (ReleaseId, Style)
);

CREATE TABLE public.discogs_release_format (
    ReleaseFormatUuId uuid NOT NULL,
    ReleaseId int8 NOT NULL,
    Name text NOT NULL,
    Quantity int8 NOT NULL,
    Text text NOT NULL,
    CONSTRAINT discogs_release_format_pkey PRIMARY KEY (ReleaseId)
);

CREATE TABLE public.discogs_release_format_description (
    ReleaseFormatUuId uuid NOT NULL,
    Description text NOT NULL,
    CONSTRAINT discogs_release_format_description_pkey PRIMARY KEY (ReleaseFormatUuId, Description)
);

CREATE TABLE public.discogs_release_artist (
    ReleaseId int8 NOT NULL,
    ArtistId int8 NOT NULL,
    CONSTRAINT discogs_release_artist_pkey PRIMARY KEY (ReleaseId, ArtistId)
);

CREATE TABLE public.discogs_release_label (
    ReleaseId int8 NOT NULL,
    LabelId int8 NOT NULL,
    Name text NOT NULL,
    Catno text NOT NULL,
    CONSTRAINT discogs_release_label_pkey PRIMARY KEY (ReleaseId, LabelId)
);

CREATE TABLE public.discogs_release_extra_artist (
    ReleaseId int8 NOT NULL,
    ArtistId int8 NOT NULL,
    Anv text NOT NULL,
    Role text NOT NULL,
    CONSTRAINT discogs_release_extra_artist_pkey PRIMARY KEY (ReleaseId, ArtistId)
);

CREATE TABLE public.discogs_release_identifier (
    ReleaseId int8 NOT NULL,
    Description text NOT NULL,
    Type text NOT NULL,
    Value text NOT NULL,
    CONSTRAINT discogs_release_identifier_pkey PRIMARY KEY (ReleaseId, Value)
);

CREATE TABLE public.discogs_release_video (
    ReleaseId int8 NOT NULL,
    Duration text NOT NULL,
    Embed text NOT NULL,
    Source text NOT NULL,
    Title text NOT NULL,
    Description text NOT NULL,
    CONSTRAINT discogs_release_video_pkey PRIMARY KEY (ReleaseId, Source)
);

CREATE TABLE public.discogs_release_company (
    ReleaseId int8 NOT NULL,
    CompanyId int8 NOT NULL,
    Name text NOT NULL,
    EntityType text NOT NULL,
    EntityTypeName text NOT NULL,
    ResourceUrl text NOT NULL,
    CONSTRAINT discogs_release_company_pkey PRIMARY KEY (ReleaseId, CompanyId)
);

CREATE TABLE public.discogs_release_track (
    ReleaseId int8 NOT NULL,
    Title text NOT NULL,
    Position text NOT NULL,
    Duration text NOT NULL,
    CONSTRAINT discogs_release_track_pkey PRIMARY KEY (ReleaseId, Position)
);

--indices
CREATE INDEX idx_discogs_artist_name_lower_trgm ON discogs_artist USING gin (lower(name) gin_trgm_ops);
CREATE INDEX idx_discogs_artist_realname_lower_trgm ON discogs_artist USING gin (lower(realname) gin_trgm_ops);

CREATE INDEX idx_discogs_artist_url_artistid ON discogs_artist_url (ArtistId);

CREATE INDEX idx_discogs_artist_alias_artistid ON discogs_artist_alias (ArtistId);

CREATE INDEX idx_discogs_label_url_labelid ON discogs_label_url (LabelId);

CREATE INDEX idx_discogs_label_sublabel_labelid ON discogs_label_sublabel  (LabelId);

CREATE INDEX idx_discogs_release_releaseid ON discogs_release (ReleaseId);

CREATE INDEX idx_discogs_release_genre_releaseid ON discogs_release_genre (ReleaseId);

CREATE INDEX idx_discogs_release_style_releaseid ON discogs_release_style (ReleaseId);

CREATE INDEX idx_discogs_release_format_description_releaseformatuuid ON discogs_release_format_description (ReleaseFormatUuId);

CREATE INDEX idx_discogs_release_artist ON discogs_release_artist (ReleaseId)

CREATE INDEX idx_discogs_release_extra_artist_releaseid ON discogs_release_extra_artist (ReleaseId);

CREATE INDEX idx_discogs_release_identifier_releaseid ON discogs_release_identifier (ReleaseId);

CREATE INDEX idx_discogs_release_video_releaseid ON discogs_release_video (ReleaseId);

CREATE INDEX idx_discogs_release_company_releaseid ON discogs_release_company (ReleaseId);

CREATE INDEX idx_discogs_release_track_releaseid ON discogs_release_track (ReleaseId);
CREATE INDEX idx_discogs_release_track_title_lower_trgm ON discogs_release_track USING gin (lower(Title) gin_trgm_ops);
