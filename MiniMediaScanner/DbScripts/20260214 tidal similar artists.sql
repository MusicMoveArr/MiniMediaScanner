CREATE TABLE public.tidal_artist_similar (
    ArtistId int NOT NULL,
    SimilarArtistId int NOT NULL,
    CONSTRAINT tidal_artist_similar_pkey PRIMARY KEY (ArtistId, SimilarArtistId)
);
CREATE INDEX idx_tidal_artist_similar_artistid ON tidal_artist_similar (ArtistId);