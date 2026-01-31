CREATE TABLE IF NOT EXISTS accounts (
    user_id SERIAL PRIMARY KEY,
    username VARCHAR(50) UNIQUE NOT NULL,
    pass_hash VARCHAR(128) NOT NULL
);

CREATE TABLE IF NOT EXISTS players (
    player_id SERIAL PRIMARY KEY,
    user_id INT REFERENCES accounts(user_id),
    pos_x FLOAT,
    pos_y FLOAT,
    hp INT,
    max_hp INT
);
