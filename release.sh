#!/usr/bin/bash

docker-compose -f docker-compose-release.yml build
docker-compose -f docker-compose-release.yml push