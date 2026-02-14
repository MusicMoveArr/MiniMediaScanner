CREATE TABLE public.tidal_album_similar (
    AlbumId int NOT NULL,
    SimilarAlbumId int NOT NULL,
    CONSTRAINT tidal_album_similar_pkey PRIMARY KEY (AlbumId, SimilarAlbumId)
);
CREATE INDEX idx_tidal_album_similar_albumId ON tidal_album_similar (AlbumId);