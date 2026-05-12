#!/bin/bash
# AAPlus — Mac Catalyst Build & Run
cd "$(dirname "$0")"

echo "🎯 AAPlus build ediliyor..."
rm -rf bin obj

# Proje dizinindeki extended attributes temizle (codesign hatası önlemi)
find . -name '.DS_Store' -delete 2>/dev/null
find . -name '._*' -not -path './.git/*' -delete 2>/dev/null
xattr -cr . 2>/dev/null

dotnet build AAPlus.csproj -f net10.0-maccatalyst -c Debug

APP_PATH="bin/Debug/net10.0-maccatalyst/maccatalyst-arm64/AAPlus.app"

if [ $? -ne 0 ]; then
    echo "❌ Build başarısız! Manuel signing deneniyor..."
    echo "🔧 Signing düzeltiliyor..."
    xattr -cr bin/ 2>/dev/null
    find bin/ -name '.DS_Store' -delete 2>/dev/null
    find bin/ -name '._*' -delete 2>/dev/null
    codesign --force --deep -s - "$APP_PATH" 2>/dev/null
fi

echo "🚀 Uygulama açılıyor..."
open "$APP_PATH"
