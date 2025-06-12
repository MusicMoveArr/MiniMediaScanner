
CREATE INDEX idx_deezer_artist_lower_name ON public.deezer_artist USING btree (lower(name));

CREATE INDEX idx_deezer_album_id ON public.deezer_album (AlbumId);
CREATE INDEX idx_deezer_artist_id ON public.deezer_album (ArtistId);

CREATE INDEX idx_deezer_album_artist_artist_id ON public.deezer_album_artist (ArtistId);
CREATE INDEX idx_deezer_album_artist_album_id ON public.deezer_album_artist (AlbumId);

CREATE INDEX idx_deezer_track_album_id ON public.deezer_track (AlbumId);
CREATE UNIQUE INDEX idx_deezer_track_track_id ON public.deezer_track (trackid);

CREATE INDEX idx_deezer_track_artist_trackid_artistid ON deezer_track_artist(trackid, artistid);
CREATE INDEX idx_deezer_track_artist_trackid ON deezer_track_artist(trackid);

CREATE INDEX idx_deezer_album_artist_albumid_artistid ON deezer_album_artist(albumid, artistid);

CREATE INDEX idx_deezer_album_image_link_ablumid ON public.deezer_album_image_link (AlbumId);