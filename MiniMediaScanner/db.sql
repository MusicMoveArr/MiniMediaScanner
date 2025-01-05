CREATE TABLE Artists (
    ArtistId UUID PRIMARY KEY,
    Name TEXT UNIQUE
);

ALTER TABLE public.artists ADD artistid uuid NOT NULL;
ALTER TABLE public.artists ADD "name" text NULL;

CREATE TABLE Albums (
    AlbumId UUID PRIMARY KEY,
    Title TEXT,
    ArtistId UUID
    UNIQUE (Title, ArtistId)
);

CREATE INDEX albums_artistid_idx ON public.albums USING btree (artistid, title);
CREATE UNIQUE INDEX albums_pkey ON public.albums USING btree (albumid);
CREATE UNIQUE INDEX albums_unique ON public.albums USING btree (title, artistid);
CREATE INDEX idx_albums_artistid_title ON public.albums USING btree (artistid, lower(title));

CREATE TABLE Metadata (
    MetadataId UUID PRIMARY KEY,
    Path TEXT,
    Title TEXT,
    AlbumId UUID,
    MusicBrainzArtistId TEXT,
    MusicBrainzDiscId TEXT,
    MusicBrainzReleaseCountry TEXT,
    MusicBrainzReleaseId TEXT,
    MusicBrainzTrackId TEXT,
    MusicBrainzReleaseStatus TEXT,
    MusicBrainzReleaseType TEXT,
    MusicBrainzReleaseArtistId TEXT,
    MusicBrainzReleaseGroupId TEXT,
    Tag_Subtitle TEXT,
    Tag_AlbumSort TEXT,
    Tag_Comment TEXT,
    Tag_Year INT default 0,
    Tag_Track INT default 0,
    Tag_TrackCount INT default 0,
    Tag_Disc INT default 0,
    Tag_DiscCount INT default 0,
    Tag_Lyrics TEXT default null,
    Tag_Grouping TEXT default null,
    Tag_BeatsPerMinute INT default 0,
    Tag_Conductor TEXT default null,
    Tag_Copyright TEXT default null,
    Tag_DateTagged TIMESTAMP default null,
    Tag_AmazonId TEXT default null,
    Tag_ReplayGainTrackGain DOUBLE default 0,
    Tag_ReplayGainTrackPeak DOUBLE default 0,
    Tag_ReplayGainAlbumGain DOUBLE default 0,
    Tag_ReplayGainAlbumPeak DOUBLE default 0,
    Tag_InitialKey TEXT default null,
    Tag_RemixedBy TEXT default null,
    Tag_Publisher TEXT default null,
    Tag_ISRC TEXT default null,
    Tag_Length TEXT default null,
    Tag_AcoustIdFingerPrint TEXT default null,
    Tag_AcoustId TEXT default null,
    File_LastWriteTime timestamp default '1999-01-08',
    File_CreationTime timestamp default '1999-01-08',
    Tag_AcoustIdFingerPrintDuration float default 0
    Tag_AllJsonTags TEXT default null,
    UNIQUE (Path)
);

CREATE INDEX idx_metadata_trackid_title_albumid ON public.metadata USING btree (musicbrainztrackid, lower(title), albumid);
CREATE INDEX metadata_musicbrainztrackid_idx ON public.metadata USING btree (musicbrainztrackid, albumid, title);
CREATE UNIQUE INDEX metadata_path_key ON public.metadata USING btree (path);
CREATE UNIQUE INDEX metadata_pkey ON public.metadata USING btree (metadataid);
CREATE INDEX metadata_title_idx ON public.metadata USING btree (title, albumid);


CREATE TABLE MusicBrainzArtist (
    MusicBrainzArtistId UUID PRIMARY KEY,
    MusicBrainzRemoteId TEXT,
    ReleaseCount INT,
    Name TEXT,
    Type TEXT,
    Country TEXT,
    SortName TEXT,
    Disambiguation TEXT,
    UNIQUE (MusicBrainzRemoteId)
);

CREATE INDEX idx_musicbrainzartist_id ON public.musicbrainzartist USING btree (musicbrainzartistid);
CREATE UNIQUE INDEX musicbrainzartist_musicbrainzremoteid_key ON public.musicbrainzartist USING btree (musicbrainzremoteid);
CREATE INDEX musicbrainzartist_name_idx ON public.musicbrainzartist USING btree (name);
CREATE UNIQUE INDEX musicbrainzartist_pkey ON public.musicbrainzartist USING btree (musicbrainzartistid);

CREATE TABLE MusicBrainzReleaseTrack (
    MusicBrainzReleaseTrackId UUID PRIMARY KEY,
    MusicBrainzRemoteReleaseTrackId TEXT,
    MusicBrainzRemoteRecordingTrackId TEXT,
    Title TEXT,
    Status TEXT,
    StatusId TEXT,
    MusicBrainzReleaseRemoteId TEXT default null,
    UNIQUE (MusicBrainzRemoteReleaseId)
);

CREATE INDEX idx_musicbrainzreleasetrack_remotereleaseid ON public.musicbrainzreleasetrack USING btree (musicbrainzremotereleaseid);
CREATE UNIQUE INDEX musicbrainzrelease_musicbrainzremotereleaseid_key ON public.musicbrainzreleasetrack USING btree (musicbrainzremotereleasetrackid);
CREATE UNIQUE INDEX musicbrainzrelease_pkey ON public.musicbrainzreleasetrack USING btree (musicbrainzreleasetrackid);
CREATE INDEX musicbrainzreleasetrack_musicbrainzremotereleaseid_idx ON public.musicbrainzreleasetrack USING btree (musicbrainzremotereleaseid);

CREATE TABLE MusicBrainzRelease (
    MusicBrainzReleaseId UUID PRIMARY KEY,
    MusicBrainzArtistId TEXT,
    MusicBrainzRemoteReleaseId TEXT,
    Title TEXT,
    Status TEXT,
    StatusId TEXT,
    Date TEXT,
    Barcode TEXT,
    Country TEXT,
    Disambiguation TEXT,
    Quality TEXT,
    UNIQUE (MusicBrainzRemoteReleaseId)
);

CREATE INDEX idx_musicbrainzrelease_artist_country_status ON public.musicbrainzrelease USING btree (musicbrainzartistid, lower(country), lower(status));
CREATE UNIQUE INDEX musicbrainzrelease_musicbrainzremotereleaseid_key1 ON public.musicbrainzrelease USING btree (musicbrainzremotereleaseid);
CREATE UNIQUE INDEX musicbrainzrelease_pkey1 ON public.musicbrainzrelease USING btree (musicbrainzreleaseid);
CREATE INDEX musicbrainzrelease_title_idx ON public.musicbrainzrelease USING btree (title);
