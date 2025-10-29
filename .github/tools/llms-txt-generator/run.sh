#!/bin/bash

# Quick setup and run script for llms.txt generator

set -e

echo "╔════════════════════════════════════════════════════════════╗"
echo "║          🤖 LLMs.txt Generator - Quick Start              ║"
echo "╚════════════════════════════════════════════════════════════╝"
echo ""

# Check if API key is set
if [ -z "$OPENROUTER_API_KEY" ]; then
    echo "❌ Error: OPENROUTER_API_KEY environment variable not set!"
    echo ""
    echo "Please set it first:"
    echo "  export OPENROUTER_API_KEY='your-key-here'"
    echo ""
    exit 1
fi

# Default path to analyze
TARGET_PATH="${1:-../../../src}"
OUTPUT_PATH="${2:-../../../llms.txt}"
MODEL="${3:-anthropic/claude-3.5-sonnet}"

echo "📂 Target path: $TARGET_PATH"
echo "📝 Output path: $OUTPUT_PATH"
echo "🤖 Model: $MODEL"
echo ""

# Navigate to tool directory
cd "$(dirname "$0")"

echo "🔧 Restoring dependencies..."
dotnet restore

echo ""
echo "🏗️  Building project..."
dotnet build -c Release

echo ""
echo "🚀 Running agent..."
echo ""

dotnet run -c Release -- \
    --path "$TARGET_PATH" \
    --output "$OUTPUT_PATH" \
    --model "$MODEL"
