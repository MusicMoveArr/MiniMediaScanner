CREATE OR REPLACE FUNCTION get_tag_value(m metadata, VARIADIC tag_keys text[])
RETURNS text
LANGUAGE sql
IMMUTABLE
AS $$
    SELECT value::text
    FROM jsonb_each_text(m.tag_alljsontags)
    WHERE lower(key) = ANY(SELECT lower(k) FROM unnest(tag_keys) AS k)
    LIMIT 1;
$$;