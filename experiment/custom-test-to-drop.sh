#!/usr/bin/bash

# Use este script para executar testes locais

RESULTS_WORKSPACE="/experiment/load-test/user-files/results"
GATLING_BIN_DIR=/gatling/bin
GATLING_WORKSPACE="/experiment/load-test/user-files"

runGatling() {
    sh $GATLING_BIN_DIR/gatling.sh -rm local -s CustomTestRampUpToDrop \
        -rd "Rinha de Backend - 2024/Q1: Crébito" \
        -rf $RESULTS_WORKSPACE \
        -sf "$GATLING_WORKSPACE/simulations"
}

startTest() {
    for i in {1..20}; do
        curl --fail http://nginx:9999/wipe && \
        echo "Wipe" && \
        # 2 requests to wake the 2 api instances up :)
        curl --fail http://nginx:9999/clientes/1/extrato && \
        echo "R1" && \
        curl --fail http://nginx:9999/clientes/1/extrato && \
        echo "R2" && \
        runGatling && \
        break || sleep 2;
    done
}

startTest
