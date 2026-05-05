#!/bin/bash
# AAPlus — Mac Catalyst Build & Run
cd "$(dirname "$0")"

echo "🎯 AAPlus build ediliyor..."
rm -rf bin obj

dotnet build AAPlus.csproj -f net10.0-maccatalyst -c Debug

if [ $? -ne 0 ]; then
    echo "❌ Build başarısız! Manuel signing deneniyor..."
fi

echo "🔧 Signing düzeltiliyor..."
APP_PATH="bin/Debug/net10.0-maccatalyst/maccatalyst-arm64/AAPlus.app"
xattr -cr bin/ 2>/dev/null
codesign --force --deep -s - "$APP_PATH" 2>/dev/null

echo "🚀 Uygulama açılıyor..."
open "$APP_PATH"
