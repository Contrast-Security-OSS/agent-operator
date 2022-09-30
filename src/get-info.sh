#!/bin/bash

readyEndpoint="https://localhost:5001/ready"
healthEndpoint="https://localhost:5001/health"
metricsEndpoint="https://localhost:5001/api/v1/metrics"
tagsEndpoint="https://localhost:5001/api/v1/metrics/tags"

echo "Accessing ready endpoint at $readyEndpoint..."
curl --insecure --silent $readyEndpoint
echo ""

echo "Accessing health endpoint at $healthEndpoint..."
curl --insecure --silent $healthEndpoint
echo ""

echo "Accessing metrics endpoint at $tagsEndpoint..."
curl --insecure --silent $tagsEndpoint | jq

echo "Accessing metrics endpoint at $metricsEndpoint (this can take a minute to gather performace metrics)..."
curl --insecure --silent $metricsEndpoint | jq
