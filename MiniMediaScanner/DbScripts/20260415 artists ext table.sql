CREATE TABLE public.artists_ext (
    ArtistId uuid NOT NULL,
    ExtArtistId text NOT NULL,
    Provider text NOT NULL,
    CONSTRAINT artists_ext_pkey PRIMARY KEY (ArtistId, ExtArtistId, Provider)
);
CREATE INDEX artists_ext_artistid ON artists_ext (ArtistId);

ALTER TABLE public.artists DROP CONSTRAINT artists_name_key;
