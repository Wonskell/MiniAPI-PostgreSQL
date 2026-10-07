CREATE TABLE IF NOT EXISTS departments (
    id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name varchar(100) NOT NULL UNIQUE,
    CONSTRAINT departments_name_valid CHECK (char_length(btrim(name)) BETWEEN 1 AND 100)
);

CREATE TABLE IF NOT EXISTS users (
    id uuid PRIMARY KEY,
    name varchar(100) NOT NULL,
    age integer NOT NULL,
    department_id integer REFERENCES departments(id) ON DELETE SET NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT users_name_valid CHECK (char_length(btrim(name)) BETWEEN 1 AND 100),
    CONSTRAINT users_age_valid CHECK (age BETWEEN 0 AND 150)
);

CREATE INDEX IF NOT EXISTS ix_users_age ON users(age);
CREATE INDEX IF NOT EXISTS ix_users_name_lower ON users(lower(name));
CREATE INDEX IF NOT EXISTS ix_users_department_id ON users(department_id);
CREATE INDEX IF NOT EXISTS ix_users_created_at ON users(created_at DESC, id);

INSERT INTO departments(name) VALUES ('Разработка'), ('Поддержка'), ('Аналитика')
ON CONFLICT (name) DO NOTHING;
