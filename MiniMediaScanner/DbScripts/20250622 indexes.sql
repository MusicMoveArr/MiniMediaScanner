CREATE INDEX idx_deezer_artist_image_link_artistid ON public.deezer_artist_image_link (ArtistId);
CREATE INDEX idx_deezer_artist_name_trgm ON deezer_artist USING gin (name gin_trgm_ops);

CREATE INDEX idx_deezer_album_title_trgm ON deezer_album USING gin (title gin_trgm_ops);
CREATE INDEX idx_deezer_track_title_trgm ON deezer_track USING gin (title gin_trgm_ops);

CREATE INDEX idx_spotify_album_name_trgm ON spotify_album USING gin (name gin_trgm_ops);
CREATE INDEX idx_spotify_track_title_trgm ON spotify_track USING gin (name gin_trgm_ops);