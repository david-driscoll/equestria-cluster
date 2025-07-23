#!/bin/sh

rclone mkdir /data/backrest
rclone mkdir /data/mysql
rclone mkdir /data/postgres
rclone serve s3 --cache-dir /cache --vfs-cache-mode writes --addr :8080 --auth-key $R3_USER_CLUSTER_USER,$R3_PASSWORD_CLUSTER_USER --auth-key $R3_USER_BACKREST,$R3_PASSWORD_BACKREST --auth-key $R3_USER_MYSQL,$R3_PASSWORD_MYSQL --auth-key $R3_USER_POSTGRES,$R3_PASSWORD_POSTGRES /data
