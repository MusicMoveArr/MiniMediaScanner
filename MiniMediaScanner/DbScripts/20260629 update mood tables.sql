--requirement:
--apt update
--apt install postgresql-18-pgvector
CREATE EXTENSION IF NOT EXISTS vector;

ALTER TABLE metadata_mood ADD COLUMN IF NOT EXISTS mood_vector vector(29);
CREATE index IF NOT exists idx_metadata_mood_vector ON metadata_mood USING hnsw (mood_vector vector_l1_ops);

UPDATE metadata_mood SET mood_vector = ARRAY[
    (mood_happy->>'happy')::float,
    (mood_happy->>'non_happy')::float,
    (mood_sad->>'sad')::float,
    (mood_sad->>'non_sad')::float,
    (mood_aggressive->>'aggressive')::float,
    (mood_aggressive->>'not_aggressive')::float,
    (mood_relaxed->>'relaxed')::float,
    (mood_relaxed->>'non_relaxed')::float,
    (mood_acoustic->>'acoustic')::float,
    (mood_acoustic->>'non_acoustic')::float,
    (mood_electronic->>'electronic')::float,
    (mood_electronic->>'non_electronic')::float,
    (mood_party->>'party')::float,
    (mood_party->>'non_party')::float,
    (ability_approach->>'approachable')::float,
    (ability_approach->>'moderately approachable')::float,
    (ability_approach->>'not approachable')::float,
    (ability_dance->>'danceable')::float,
    (ability_dance->>'not_danceable')::float,
    (voice_instrumental->>'voice')::float,
    (voice_instrumental->>'instrumental')::float,
    (timbre->>'bright')::float,
    (timbre->>'dark')::float,
    (engagement_3c->>'engaging')::float,
    (engagement_3c->>'moderately engaging')::float,
    (engagement_3c->>'not engaging')::float,
    (engagement_regression->>'engagement')::float,
    (gender->>'male')::float,
    (gender->>'female')::float
]::vector where mood_vector is null;