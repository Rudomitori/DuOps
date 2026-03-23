CREATE TABLE IF NOT EXISTS duops_operations
(
    type          text          NOT NULL,
    id            text          NOT NULL,

    queue         text          NOT NULL,
    scheduled_at  timestamptz,
    locked_until  timestamptz,
    args          text          NOT NULL,

    created_at    timestamptz   NOT NULL,
    finished_at   timestamptz,

    state         smallint      NOT NULL,

    result        text,

    fail_reason   text,

    retry_count   integer       NOT NULL,

    primary key (type, id)
);

CREATE INDEX IF NOT EXISTS ix_duops_operations_queue
    ON duops_operations (queue, coalesce(locked_until, scheduled_at))
    -- where state = Active
    WHERE state = 10;


comment on column duops_operations.state is $$
Active = 10
Completed = 20,
Failed = 30,
$$;

CREATE TABLE IF NOT EXISTS duops_inner_results
(
    operation_type             text         NOT NULL,
    operation_id               text         NOT NULL,

    inner_result_type          text         NOT NULL,
    inner_result_id            text,

    value                      text         NOT NULL,
    created_at                 timestamptz  NOT NULL,
    updated_at                 timestamptz,

    UNIQUE NULLS NOT DISTINCT (
         operation_type,
         operation_id,
         inner_result_type,
         inner_result_id
    )
);