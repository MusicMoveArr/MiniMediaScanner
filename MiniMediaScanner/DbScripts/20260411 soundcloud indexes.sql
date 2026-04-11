CREATE INDEX idx_soundcloud_playlist_title_trgm ON soundcloud_playlist USING gin (lower(title) gin_trgm_ops);

CREATE INDEX idx_soundcloud_track_title_trgm ON soundcloud_track USING gin (lower(title) gin_trgm_ops);

CREATE INDEX idx_soundcloud_user_title_trgm ON soundcloud_user USING gin (lower(title) gin_trgm_ops);
