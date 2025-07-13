DROP INDEX public.tidal_track_trackid_idx;

CREATE INDEX tidal_track_trackid_idx ON public.tidal_track (trackid);
CREATE UNIQUE INDEX tidal_track_trackid_album_idx ON public.tidal_track (TrackId, AlbumId);