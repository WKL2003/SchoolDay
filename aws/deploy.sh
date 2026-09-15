#!/usr/bin/env bash
# One stack: DynamoDB + Lambda + HTTP API + S3 dashboard + CloudFront.
# Run from anywhere. Needs: aws cli, credentials, a bootstrap bucket for the zip.

set -euo pipefail

ROOT="$(cd "$(dirname "$0")" && pwd)"
REGION="${AWS_REGION:-ap-southeast-1}"
STACK="${STACK_NAME:-schoolday-metrics}"
DASHBOARD="$ROOT/../dashboard"

export PATH="$HOME/.local/bin:$PATH"

ACCOUNT="$(aws sts get-caller-identity --query Account --output text --region "$REGION")"
ART="schoolday-metrics-art-${ACCOUNT}-${REGION}"

echo "Account $ACCOUNT"
echo "Region  $REGION"
echo "Stack   $STACK"

if ! aws s3api head-bucket --bucket "$ART" --region "$REGION" 2>/dev/null; then
  aws s3api create-bucket \
    --bucket "$ART" \
    --region "$REGION" \
    --create-bucket-configuration LocationConstraint="$REGION"
fi

PACKAGED="$ROOT/packaged.yaml"
aws cloudformation package \
  --template-file "$ROOT/template.yaml" \
  --s3-bucket "$ART" \
  --output-template-file "$PACKAGED" \
  --region "$REGION"

aws cloudformation deploy \
  --template-file "$PACKAGED" \
  --stack-name "$STACK" \
  --capabilities CAPABILITY_IAM \
  --region "$REGION"

out() {
  aws cloudformation describe-stacks \
    --stack-name "$STACK" \
    --region "$REGION" \
    --query "Stacks[0].Outputs[?OutputKey=='$1'].OutputValue" \
    --output text
}

API="$(out ApiUrl)"
BUCKET="$(out DashboardBucketName)"
DASH_S3="$(out DashboardS3Url)"
DASH_CDN="$(out DashboardUrl)"
TABLE="$(out TableName)"

printf 'window.SCHOOLDAY_API = "%s";\n' "$API" > "$DASHBOARD/config.js"

aws s3 cp "$DASHBOARD/index.html" "s3://$BUCKET/index.html" \
  --region "$REGION" --content-type "text/html; charset=utf-8" --cache-control "no-cache"
aws s3 cp "$DASHBOARD/config.js" "s3://$BUCKET/config.js" \
  --region "$REGION" --content-type "application/javascript; charset=utf-8" --cache-control "no-cache"

DIST="$(aws cloudformation describe-stack-resources \
  --stack-name "$STACK" \
  --region "$REGION" \
  --query "StackResources[?LogicalResourceId=='DashboardCdn'].PhysicalResourceId" \
  --output text)"
if [ -n "$DIST" ] && [ "$DIST" != "None" ]; then
  aws cloudfront create-invalidation \
    --distribution-id "$DIST" \
    --paths / /index.html /config.js >/dev/null
fi

python3 - <<PY
import json
from pathlib import Path
Path("$ROOT/outputs.json").write_text(json.dumps({
    "region": "$REGION",
    "stack": "$STACK",
    "api_url": "$API",
    "dashboard_url": "$DASH_CDN",
    "dashboard_s3_url": "$DASH_S3",
    "table": "$TABLE",
}, indent=2) + "\n")
PY

echo
echo "API        $API"
echo "Dashboard  $DASH_CDN"
echo "S3 site    $DASH_S3"
echo "Table      $TABLE"
echo
echo "Set MetricsConfig.BaseUrl to:"
echo "  $API"
echo
echo "Smoke:"
curl -sS -X POST "$API/events" \
  -H "Content-Type: application/json" \
  -d '{"session_id":"smoke","kind":"session_start","ts":"2026-09-07T00:00:00Z","pocket":8}'
echo
curl -sS "$API/summary"
echo
