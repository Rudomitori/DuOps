create table if not exists duops_operations
(
    discriminator       text                     not null,
    id                  text                     not null,

    polling_schedule_id text,
    started_at          timestamp with time zone not null,

    args                text                     not null,

    state               integer                  not null,
    result              text,
    fail_reason         text,
    waiting_until       timestamptz,
    retrying_at         timestamptz,
    retry_count         integer,

    inter_results       jsonb                    not null,

    primary key (discriminator, id)
);

comment on column duops_operations.state is $$
Created = 10
Waiting = 20,
Retrying = 30,
Finished = 40,
Failed = 50,
$$;

comment on column duops_operations.inter_results is $$
{ 
    "discriminator1": "serializedValue",
    "discriminator2": {
        "key1": "serializedValue",
        "key1": "serializedValue"
    }
}
$$;