CREATE TABLE IF NOT EXISTS public.metadata_mood (
    MetadataId uuid not null,
    mood_happy jsonb not null default '[]',
    mood_sad jsonb not null default '[]',
    mood_aggressive jsonb not null default '[]',
    mood_relaxed jsonb not null default '[]',
    mood_acoustic jsonb not null default '[]',
    mood_electronic jsonb not null default '[]',
    mood_party jsonb not null default '[]',
    ability_approach jsonb not null default '[]',
    ability_dance jsonb not null default '[]',
    voice_instrumental jsonb not null default '[]',
    timbre jsonb not null default '[]',
    engagement_3c jsonb not null default '[]',
    engagement_regression jsonb not null default '[]',
    gender jsonb not null default '[]',
    genre_json jsonb not null default '[]',
    CONSTRAINT metadata_mood_pkey PRIMARY KEY (MetadataId)
);