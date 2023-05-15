FROM nginx:alpine

RUN apk update && apk add bash

COPY nginx.conf /etc/nginx/nginx.template.conf

COPY wait-for-it.sh /wait-for-it.sh
RUN chmod +x /wait-for-it.sh

CMD set -a \
    && CERT_FILE=${CERT_FILE:-crt.crt} \
	&& CERT_KEY_FILE=${CERT_KEY_FILE:-key.key} \
    && CONCERTO_UPSTREAM=${CONCERTO_UPSTREAM:-concerto_server:80} \
	&& envsubst '$${BASE_PATH} $${CONCERTO_DOMAIN} $${JITSI_DOMAIN} $${KEYCLOAK_DOMAIN} $${CERT_FILE} $${CERT_KEY_FILE} $${CONCERTO_UPSTREAM}' < /etc/nginx/nginx.template.conf > /etc/nginx/nginx.conf \
	&& /wait-for-it.sh ${CONCERTO_UPSTREAM} -t 0 \
	&& /wait-for-it.sh keycloak:8080 -t 0 \
	&& /wait-for-it.sh jitsi_web:80 -t 0 \
	&& nginx -g 'daemon off;'