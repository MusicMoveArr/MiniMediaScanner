CREATE TABLE public.deezer_artist (
    ArtistId int8 NOT NULL,
    Name text NOT NULL,
    NbAlbum int NOT NULL,
    NbFan int NOT NULL,
    Radio bool NOT NULL,
    Type text NOT NULL,
    lastsynctime timestamp DEFAULT current_timestamp,
    CONSTRAINT deezer_artist_pkey PRIMARY KEY (ArtistId)
);

CREATE TABLE public.deezer_artist_image_link (
    ArtistId int8 NOT NULL,
    href text NOT NULL,
    type text NOT NULL, --picture, small, medium, big, xl
    CONSTRAINT deezer_artist_image_link_pkey PRIMARY KEY (ArtistId, type)
);

CREATE TABLE public.deezer_album (
    AlbumId int8 NOT NULL,
    ArtistId int8 NOT NULL,
    Title text NOT NULL,
    Md5Image text NOT NULL,
    GenreId int NOT NULL,
    Fans int NOT NULL,
    ReleaseDate text NOT NULL,
    RecordType text NOT NULL,
    ExplicitLyrics bool NOT NULL,
    ExplicitContentLyrics int NOT NULL,
    ExplicitContentCover int NOT NULL,
    Type text NOT NULL,
    UPC text NOT NULL,
    Label text NOT NULL,
    NbTracks int NOT NULL,
    Duration int NOT NULL,
    Available bool NOT NULL,
    CONSTRAINT deezer_album_pkey PRIMARY KEY (AlbumId, ArtistId)
);
CREATE TABLE public.deezer_album_image_link (
    AlbumId int8 NOT NULL,
    href text NOT NULL,
    type text NOT NULL, --picture, small, medium, big, xl
    CONSTRAINT deezer_album_image_link_pkey PRIMARY KEY (AlbumId, type)
);
CREATE TABLE public.deezer_album_artist (
    AlbumId int8 NOT NULL,
    ArtistId int8 NOT NULL,
    Role text NOT NULL,
    CONSTRAINT deezer_album_artist_pkey PRIMARY KEY (AlbumId, ArtistId, Role)
);
CREATE TABLE public.deezer_album_genre (
    AlbumId int8 NOT NULL,
    GenreId int8 NOT NULL,
    CONSTRAINT deezer_album_genre_pkey PRIMARY KEY (AlbumId, GenreId)
);


CREATE TABLE public.deezer_track (
    TrackId int8 NOT NULL,
    Readable bool NOT NULL,
    Title text NOT NULL,
    TitleShort text NOT NULL,
    TitleVersion text NOT NULL,
    ISRC text NOT NULL,
    Duration int NOT NULL,
    TrackPosition int NOT NULL,
    DiskNumber int NOT NULL,
    Rank int NOT NULL,
    ReleaseDate text NOT NULL,
    ExplicitLyrics bool NOT NULL,
    ExplicitContentLyrics int NOT NULL,
    ExplicitContentCover int NOT NULL,
    Preview text NOT NULL,
    BPM float8 NOT NULL,
    Gain float8 NOT NULL,
    Md5Image text NOT NULL,
    TrackToken text NOT NULL,
    ArtistId int8 NOT NULL,
    AlbumId int8 NOT NULL,
    Type text NOT NULL,
    CONSTRAINT deezer_track_pkey PRIMARY KEY (TrackId, ArtistId, AlbumId)
);
CREATE TABLE public.deezer_track_artist (
    TrackId int8 NOT NULL,
    ArtistId int8 NOT NULL,
    AlbumId int8 NOT NULL,
    CONSTRAINT deezer_track_artist_pkey PRIMARY KEY (TrackId, ArtistId, AlbumId)
);
CREATE TABLE public.deezer_genre (
    GenreId int8 NOT NULL,
    Name text NOT NULL,
    Picture text NOT NULL,
    Type text NOT NULL,
    CONSTRAINT deezer_genre_pkey PRIMARY KEY (GenreId, Type)
);