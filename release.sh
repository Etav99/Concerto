#!/usr/bin/bash

if [ -z "$1" ]; then
  echo "Error: VERSION not provided."
  exit 1
fi

set -a
VERSION=$1
BASE_JITSI_TAG=${2:-stable-8615}
JITSI_RELEASE="${BASE_JITSI_TAG}-concerto-${VERSION}"

FORCE_REBUILD=0
JITSI_REPO=etav

COMPOSE_DOCKER_CLI_BUILD=1
DOCKER_BUILDKIT=1

docker buildx build \
	--platform linux/amd64,linux/arm64 \
	--progress=plain \
	--pull --push \
	--tag etav/concerto_server:${VERSION} \
	--file ./Server/Dockerfile .
	
docker buildx build \
	--platform linux/amd64,linux/arm64 \
	--progress=plain \
	--pull --push \
	--tag etav/concerto_proxy:${VERSION} \
	--file ./DockerCompose/nginx/Dockerfile ./DockerCompose/nginx
	
# docker buildx build \
# 	--platform linux/amd64,linux/arm64 \
# 	--progress=plain \
# 	--pull --push \
# 	--tag etav/kask:${VERSION} \
# 	--file ./DockerCompose/nginx_kask_sim/Dockerfile ./DockerCompose/nginx_kask_sim
	
cd JitsiDocker
make buildx_web
make buildx_jibri
make buildx_jvb
cd ..


#docker-compose -f DockerCompose/docker-compose.release.yml build && \
#docker-compose -f DockerCompose/docker-compose.release.yml push