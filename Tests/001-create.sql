create table Person ( data json NOT NULL );

CREATE UNIQUE INDEX people_id ON Person ((data->>'_id'));
