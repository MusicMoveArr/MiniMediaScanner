CREATE TABLE public.acoustid_submission (
    SubmissionId int NOT NULL,
    MetadataId uuid NOT NULL,
    Status text NOT NULL,
    SubmittedAt timestamp DEFAULT current_timestamp,
    CONSTRAINT acoustid_submission_pkey PRIMARY KEY (SubmissionId, MetadataId)
);

alter table acoustid_submission add column importid uuid default null;