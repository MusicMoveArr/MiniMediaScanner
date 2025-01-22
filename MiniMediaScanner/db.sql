CREATE TABLE public.artists (
    artistid uuid NOT NULL,
    "name" text NULL,
    CONSTRAINT artists_name_key UNIQUE (name),
    CONSTRAINT artists_pkey PRIMARY KEY (artistid)
);
CREATE INDEX idx_artists_lower_name ON public.artists USING btree (lower(name));

CREATE TABLE public.albums (
    albumid uuid NOT NULL,
    title text NULL,
    artistid uuid NULL,
    CONSTRAINT albums_pkey PRIMARY KEY (albumid),
    CONSTRAINT albums_unique UNIQUE (title, artistid)
);
CREATE INDEX albums_artistid_idx ON public.albums USING btree (artistid, title);
CREATE INDEX idx_albums_artistid_title ON public.albums USING btree (artistid, lower(title));

CREATE TABLE public.metadata (
    metadataid uuid NOT NULL,
    "path" text NULL,
    title text NULL,
    albumid uuid NULL,
    musicbrainzartistid text NULL,
    musicbrainzdiscid text NULL,
    musicbrainzreleasecountry text NULL,
    musicbrainzreleaseid text NULL,
    musicbrainztrackid text NULL,
    musicbrainzreleasestatus text NULL,
    musicbrainzreleasetype text NULL,
    musicbrainzreleaseartistid text NULL,
    musicbrainzreleasegroupid text NULL,
    tag_subtitle text NULL,
    tag_albumsort text NULL,
    tag_comment text NULL,
    tag_year int4 DEFAULT 0 NULL,
    tag_track int4 DEFAULT 0 NULL,
    tag_trackcount int4 DEFAULT 0 NULL,
    tag_disc int4 DEFAULT 0 NULL,
    tag_disccount int4 DEFAULT 0 NULL,
    tag_lyrics text NULL,
    tag_grouping text NULL,
    tag_beatsperminute int4 DEFAULT 0 NULL,
    tag_conductor text NULL,
    tag_copyright text NULL,
    tag_datetagged timestamp NULL,
    tag_amazonid text NULL,
    tag_replaygaintrackgain float8 DEFAULT 0 NULL,
    tag_replaygaintrackpeak float8 DEFAULT 0 NULL,
    tag_replaygainalbumgain float8 DEFAULT 0 NULL,
    tag_replaygainalbumpeak float8 DEFAULT 0 NULL,
    tag_initialkey text NULL,
    tag_remixedby text NULL,
    tag_publisher text NULL,
    tag_isrc text NULL,
    tag_length text NULL,
    tag_acoustidfingerprint text NULL,
    tag_acoustid text NULL,
    file_lastwritetime timestamp DEFAULT '1999-01-08 00:00:00'::timestamp without time zone NULL,
    file_creationtime timestamp DEFAULT '1999-01-08 00:00:00'::timestamp without time zone NULL,
    tag_acoustidfingerprint_duration float8 DEFAULT 0 NULL,
    tag_alljsontags text NULL,
    CONSTRAINT metadata_path_key UNIQUE (path),
    CONSTRAINT metadata_pkey PRIMARY KEY (metadataid)
);
CREATE INDEX idx_metadata_trackid_title_albumid ON public.metadata USING btree (musicbrainztrackid, lower(title), albumid);
CREATE INDEX metadata_musicbrainztrackid_idx ON public.metadata USING btree (musicbrainztrackid, albumid, title);
CREATE INDEX metadata_title_idx ON public.metadata USING btree (title, albumid);


CREATE TABLE public.musicbrainzartist (
    musicbrainzartistid uuid NOT NULL,
    musicbrainzremoteid text NULL,
    releasecount int4 NULL,
    "name" text NULL,
    "type" text NULL,
    country text NULL,
    sortname text NULL,
    disambiguation text NULL,
    CONSTRAINT musicbrainzartist_musicbrainzremoteid_key UNIQUE (musicbrainzremoteid),
    CONSTRAINT musicbrainzartist_pkey PRIMARY KEY (musicbrainzartistid)
);
CREATE INDEX idx_musicbrainzartist_id ON public.musicbrainzartist USING btree (musicbrainzartistid);
CREATE INDEX musicbrainzartist_name_idx ON public.musicbrainzartist USING btree (name);

CREATE TABLE public.musicbrainzrelease (
    musicbrainzreleaseid uuid NOT NULL,
    musicbrainzartistid text NULL,
    musicbrainzremotereleaseid text NULL,
    title text NULL,
    status text NULL,
    statusid text NULL,
    "date" text NULL,
    barcode text NULL,
    country text NULL,
    disambiguation text NULL,
    quality text NULL,
    CONSTRAINT musicbrainzrelease_musicbrainzremotereleaseid_key1 UNIQUE (musicbrainzremotereleaseid),
    CONSTRAINT musicbrainzrelease_pkey1 PRIMARY KEY (musicbrainzreleaseid)
);
CREATE INDEX idx_musicbrainzrelease_artist_country_status ON public.musicbrainzrelease USING btree (musicbrainzartistid, lower(country), lower(status));
CREATE INDEX musicbrainzrelease_title_idx ON public.musicbrainzrelease USING btree (title);


CREATE TABLE public.musicbrainzreleasetrack (
    musicbrainzreleasetrackid uuid NOT NULL,
    musicbrainzremotereleasetrackid text NULL,
    musicbrainzremoterecordingtrackid text NULL,
    title text NULL,
    status text NULL,
    statusid text NULL,
    musicbrainzremotereleaseid text NULL,
    CONSTRAINT musicbrainzrelease_musicbrainzremotereleaseid_key UNIQUE (musicbrainzremotereleasetrackid),
    CONSTRAINT musicbrainzrelease_pkey PRIMARY KEY (musicbrainzreleasetrackid)
);

CREATE INDEX idx_musicbrainzreleasetrack_remotereleaseid ON public.musicbrainzreleasetrack USING btree (musicbrainzremotereleaseid);
CREATE INDEX musicbrainzreleasetrack_musicbrainzremotereleaseid_idx ON public.musicbrainzreleasetrack USING btree (musicbrainzremotereleaseid);

alter table musicbrainzreleasetrack add column length int default 0;
alter table musicbrainzreleasetrack add column number int default 0;
alter table musicbrainzreleasetrack add column position int default 0;
alter table musicbrainzreleasetrack add column recordingid text null;
alter table musicbrainzreleasetrack add column recordinglength int default 0;
alter table musicbrainzreleasetrack add column recordingtitle text null;
alter table musicbrainzreleasetrack add column recordingvideo bool default false;
alter table musicbrainzreleasetrack add column mediatrackcount int default 0;
alter table musicbrainzreleasetrack add column mediaformat text null;
alter table musicbrainzreleasetrack add column mediatitle text null;
alter table musicbrainzreleasetrack add column mediaposition int default 0;
alter table musicbrainzreleasetrack add column mediatrackoffset int default 0;