CREATE INDEX idx_deezer_artist_name_lower ON deezer_artist (lower(name));
CREATE INDEX idx_deezer_track_artist_artistid ON deezer_track_artist (artistid);
