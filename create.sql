CREATE TABLE IF NOT EXISTS Entry (
    `id` VARCHAR(32) NOT NULL,
    `text` VARCHAR(2048) NOT NULL,
    `entry_date` DATETIME NOT NULL,
    `created_date` DATETIME NOT NULL,
    `tags` VARCHAR(2048)
);

CREATE TABLE IF NOT EXISTS Tag (
    `entry_id` VARCHAR(32) NOT NULL,
    `tag` VARCHAR(64) NOT NULL,
    CONSTRAINT `uq_entry_tag` UNIQUE(`entry_id`, `tag`)
);