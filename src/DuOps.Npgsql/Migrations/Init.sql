create table if not exists duops_operations
(
    discriminator       text                     not null,
    id                  text                     not null,

    polling_schedule_id text,
    started_at          timestamp with time zone not null,

    args                text                     not null,

    is_finished         bool                     not null,
    result              text,

    inter_results       jsonb                    not null,
    -- { 
    --     "discriminator1": "serializedValue",
    --     "discriminator2": {
    --         "key1": "serializedValue",
    --         "key1": "serializedValue"
    --     }
    -- }

    primary key (discriminator, id)
);