CREATE INDEX idx_tidal_artist_name_lower_trgm ON tidal_artist USING gin (lower(name) gin_trgm_ops);
CREATE INDEX idx_spotify_artist_name_lower_trgm ON spotify_artist USING gin (lower(name) gin_trgm_ops);
