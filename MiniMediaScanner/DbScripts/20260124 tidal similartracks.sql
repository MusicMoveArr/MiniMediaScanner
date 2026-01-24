CREATE TABLE public.tidal_track_similar (
    TrackId int NOT NULL,
    SimilarTrackId int NOT NULL,
    SimilarISRC text NOT NULL,
    CONSTRAINT tidal_track_similar_pkey PRIMARY KEY (TrackId, SimilarTrackId)
);
CREATE INDEX idx_tidal_track_similar_trackid ON tidal_track_similar (TrackId);