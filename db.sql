CREATE DATABASE finodb
    WITH
    OWNER = postgres
    ENCODING = 'UTF8'
    LC_COLLATE = 'English_Israel.1252'
    LC_CTYPE = 'English_Israel.1252'
    LOCALE_PROVIDER = 'libc'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1
    IS_TEMPLATE = False;

CREATE TABLE users_revenue (
    user_id TEXT NOT NULL,
    revenue INTEGER NOT NULL
);

CREATE INDEX idx_users_revenue_user_id ON users_revenue (user_id);

CREATE PROCEDURE update_revenue(userid TEXT, value INTEGER)
LANGUAGE plpgsql
AS $$
DECLARE
    last_revenue INTEGER := 0;
BEGIN
    -- a delay
    PERFORM pg_sleep(0.001);

    -- get last revenue for the userid
    SELECT revenue INTO last_revenue
    FROM users_revenue
    WHERE user_id = userid
    ORDER BY rowid DESC
    LIMIT 1;

    -- add updated revenue
    INSERT INTO users_revenue (user_id, revenue)
    VALUES (user_id, last_revenue + diffvalue);
END;
$$;
