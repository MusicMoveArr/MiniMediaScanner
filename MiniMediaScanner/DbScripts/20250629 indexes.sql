DROP INDEX public.idx_deezer_artist_lower_name;
DROP INDEX public.musicbrainzartist_name_lowercase_idx;

CREATE INDEX idx_deezer_artist_name_lower_trgm ON deezer_artist USING gin (lower(name) gin_trgm_ops);

CREATE INDEX idx_deezer_album_title_lower_trgm ON deezer_album USING gin (lower(title) gin_trgm_ops);
CREATE INDEX idx_deezer_track_title_lower_trgm ON deezer_track USING gin (lower(title) gin_trgm_ops);

CREATE INDEX idx_spotify_album_name_lower_trgm ON spotify_album USING gin (lower(name) gin_trgm_ops);
CREATE INDEX idx_spotify_track_title_lower_trgm ON spotify_track USING gin (lower(name) gin_trgm_ops);

CREATE INDEX idx_musicbrainz_artist_name_lower_trgm ON musicbrainz_artist USING gin (lower(name) gin_trgm_ops);
CREATE INDEX idx_musicbrainz_release_track_title_lower_trgm ON musicbrainz_release_track USING gin (lower(title) gin_trgm_ops);
