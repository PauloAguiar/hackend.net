# Use Amazon Corretto 21 with Alpine JDK as the base image
FROM amazoncorretto:21-alpine-jdk

# working directory for gatling
WORKDIR /opt

ENV GATLING_VERSION 3.10.3

RUN mkdir -p gatling && \
  apk add --update wget bash libc6-compat curl && \
  mkdir -p /tmp/downloads && \
  wget -q -O /tmp/downloads/gatling-$GATLING_VERSION.zip \
  https://repo1.maven.org/maven2/io/gatling/highcharts/gatling-charts-highcharts-bundle/$GATLING_VERSION/gatling-charts-highcharts-bundle-$GATLING_VERSION-bundle.zip && \
  mkdir -p /tmp/archive && cd /tmp/archive && \
  unzip /tmp/downloads/gatling-$GATLING_VERSION.zip && \
  mv /tmp/archive/gatling-charts-highcharts-bundle-$GATLING_VERSION/* /opt/gatling/ && \
  rm -rf /tmp/*

WORKDIR  /opt/gatling
VOLUME ["/opt/gatling/conf", "/opt/gatling/results", "/opt/gatling/user-files/simulations"]

ENV PATH="/opt/gatling/bin:${PATH}"
ENV GATLING_HOME /opt/gatling