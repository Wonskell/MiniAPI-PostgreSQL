-- Запускайте после первого старта приложения: он создаёт схему.
SELECT id, name, age FROM users WHERE age BETWEEN 18 AND 30 ORDER BY age, name;

SELECT u.name, u.age, d.name AS department
FROM users u LEFT JOIN departments d ON d.id = u.department_id
ORDER BY u.name;

SELECT d.name, count(u.id) AS user_count, round(avg(u.age), 1) AS average_age
FROM departments d LEFT JOIN users u ON u.department_id = d.id
GROUP BY d.id, d.name
ORDER BY user_count DESC, d.name;

SELECT d.name, count(u.id) AS user_count
FROM departments d JOIN users u ON u.department_id = d.id
GROUP BY d.id, d.name HAVING count(u.id) >= 2;

SELECT name, age FROM users WHERE age > (SELECT avg(age) FROM users);

EXPLAIN ANALYZE SELECT * FROM users WHERE lower(name) = lower('Анна Смирнова');

-- Изменения видны внутри транзакции, но не сохраняются после ROLLBACK.
BEGIN;
INSERT INTO users(id, name, age)
VALUES (gen_random_uuid(), 'Пример транзакции', 20)
RETURNING id, name, age;
SAVEPOINT before_update;
UPDATE users SET age = age + 1 WHERE name = 'Пример транзакции' AND age < 150;
SELECT name, age FROM users WHERE name = 'Пример транзакции';
ROLLBACK TO SAVEPOINT before_update;
DELETE FROM users WHERE name = 'Пример транзакции';
ROLLBACK;

