


CREATE DATABASE DBTelebot;

CREATE TABLE users (
    userid BIGINT PRIMARY KEY,
    balance DECIMAL(15, 2) NOT NULL DEFAULT 0.00,
    fullname VARCHAR(255) NOT NULL,
    phonenumber VARCHAR(15) UNIQUE NOT NULL,
    passwordhash TEXT NOT NULL
);

CREATE TABLE twofactorauthenticationcodes (
    id SERIAL PRIMARY KEY,
    userid BIGINT REFERENCES users(userid) ON DELETE CASCADE,
    codes VARCHAR(10) NOT NULL,
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE transaction_fees (
    id BIGSERIAL PRIMARY KEY,
    userid BIGINT NOT NULL,
    fee DECIMAL(15, 2) NOT NULL,
    operation_type VARCHAR(50) NOT NULL,
    date TIMESTAMP DEFAULT NOW(),
    FOREIGN KEY (userid) REFERENCES users(userid) ON DELETE CASCADE
);