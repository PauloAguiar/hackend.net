CREATE UNLOGGED TABLE clients (
   client_id SERIAL PRIMARY KEY,
   client_name VARCHAR(50) NOT NULL,
   client_limit INTEGER NOT NULL,
   client_current INTEGER NOT NULL DEFAULT 0
);

CREATE UNLOGGED TABLE transactions (
    transaction_id           SERIAL PRIMARY KEY,
    client_id                INTEGER NOT NULL,
    transaction_value        INTEGER NOT NULL,
    transaction_type         CHAR(1)     NOT NULL,
    transaction_description  VARCHAR(10) NOT NULL,
    transaction_date         TIMESTAMP   NOT NULL,
    CONSTRAINT transactions_client_id FOREIGN KEY (client_id) REFERENCES clients(client_id)
);

CREATE INDEX idx_transactions_client_id_date ON transactions (client_id, transaction_date DESC);

INSERT INTO clients (client_name, client_limit)
  VALUES
    ('o barato sai caro', 1000 * 100),
    ('zan corp ltda', 800 * 100),
    ('les cruders', 10000 * 100),
    ('padaria joia de cocaia', 100000 * 100),
    ('kid mais', 5000 * 100);

CREATE OR REPLACE FUNCTION create_transaction_for_client(
    p_client_id integer,
    p_transaction_value integer,
    p_transaction_type character,
    p_transaction_description character varying,
    p_transaction_date timestamp without time zone,
    OUT result_code integer,
    OUT out_client_limit integer,
    OUT out_client_current integer)
RETURNS RECORD
LANGUAGE 'plpgsql'
AS $$
BEGIN
    -- Check if the client_id exists
    SELECT client_limit, client_current INTO out_client_limit, out_client_current
    FROM clients WHERE client_id = p_client_id
    FOR UPDATE;

    IF NOT FOUND THEN
        result_code := 1;
        RETURN;
    END IF;

    -- Check if the new client_current value is above the allowed limit
    IF out_client_current + p_transaction_value < -1 * out_client_limit THEN
        result_code := 2;
        RETURN;
    ELSE
        result_code := 0;
    END IF;

    -- Insert the transaction
    INSERT INTO transactions (client_id, transaction_value, transaction_type, transaction_description, transaction_date)
    VALUES (p_client_id, p_transaction_value, p_transaction_type, p_transaction_description, p_transaction_date);

    -- Update client's current value
    out_client_current := out_client_current  + p_transaction_value;
    UPDATE clients
    SET client_current = out_client_current
    WHERE client_id = p_client_id;
END;
$$;

CREATE OR REPLACE FUNCTION get_client_transactions(p_client_id INTEGER)
RETURNS TABLE(
    client_limit INTEGER,
    client_current INTEGER,
    transaction_value INTEGER,
    transaction_type CHAR(1),
    transaction_description VARCHAR(10),
    transaction_date TIMESTAMP
)
LANGUAGE 'plpgsql'
AS $$
BEGIN
    -- First, return client details even if there are no transactions
    RETURN QUERY
    SELECT c.client_limit, c.client_current, NULL::INTEGER, NULL::CHAR(1), NULL::VARCHAR(10), NULL::TIMESTAMP
    FROM clients c
    WHERE c.client_id = p_client_id;

    -- Then, return transactions if they exist
    RETURN QUERY
    SELECT c.client_limit, c.client_current, t.transaction_value, t.transaction_type, t.transaction_description, t.transaction_date
    FROM clients c
    JOIN transactions t ON c.client_id = t.client_id
    WHERE c.client_id = p_client_id
    ORDER BY t.transaction_date DESC
    LIMIT 10;
END;
$$;