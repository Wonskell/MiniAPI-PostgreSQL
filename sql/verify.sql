-- Любая ошибка завершит psql с ненулевым кодом при -v ON_ERROR_STOP=1.
BEGIN;
DO $$
DECLARE
    test_department integer;
    test_user uuid := gen_random_uuid();
    rejected boolean;
BEGIN
    INSERT INTO departments(name) VALUES ('test-' || gen_random_uuid()::text) RETURNING id INTO test_department;
    INSERT INTO users(id, name, age, department_id) VALUES (test_user, 'SQL check', 0, test_department);
    UPDATE users SET age = 150 WHERE id = test_user;

    rejected := false;
    BEGIN
        UPDATE users SET age = 151 WHERE id = test_user;
    EXCEPTION WHEN check_violation THEN rejected := true;
    END;
    IF NOT rejected THEN RAISE EXCEPTION 'Age constraint is missing'; END IF;

    rejected := false;
    BEGIN
        UPDATE users SET name = '   ' WHERE id = test_user;
    EXCEPTION WHEN check_violation THEN rejected := true;
    END;
    IF NOT rejected THEN RAISE EXCEPTION 'Name constraint is missing'; END IF;

    rejected := false;
    BEGIN
        UPDATE users SET department_id = -1 WHERE id = test_user;
    EXCEPTION WHEN foreign_key_violation THEN rejected := true;
    END;
    IF NOT rejected THEN RAISE EXCEPTION 'Foreign key is missing'; END IF;

    DELETE FROM departments WHERE id = test_department;
    IF NOT EXISTS (SELECT 1 FROM users WHERE id = test_user AND department_id IS NULL) THEN
        RAISE EXCEPTION 'ON DELETE SET NULL failed';
    END IF;
END $$;
ROLLBACK;

