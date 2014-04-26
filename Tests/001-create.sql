create table people ( data json NOT NULL );

CREATE UNIQUE INDEX people_id ON people ((data->>'_id'));
