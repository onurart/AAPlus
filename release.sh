#!/bin/bash
# ═══════════════════════════════════════════════════════════
# AAPlus — Google Play Release Build Script
# Android AAB (App Bundle) oluşturur
# ═══════════════════════════════════════════════════════════
cd "$(dirname "$0")"

KEYSTORE_FILE="aaplus.keystore"
KEYSTORE_ALIAS="aaplus"

# ─── 1. Keystore Oluştur (sadece ilk seferde) ───────────
if [ ! -f "$KEYSTORE_FILE" ]; then
    echo "🔑 Keystore oluşturuluyor..."
    echo "❗ Bir şifre belirlemen gerekecek — bu şifreyi UNUTMA!"
    echo ""
    keytool -genkeypair \
        -v \
        -keystore "$KEYSTORE_FILE" \
        -alias "$KEYSTORE_ALIAS" \
        -keyalg RSA \
        -keysize 2048 \
        -validity 10000 \
        -storepass aaplus123 \
        -keypass aaplus123 \
        -dname "CN=AAPlus Developer, O=AAPlus, L=Istanbul, C=TR"
    
    if [ $? -ne 0 ]; then
        echo "❌ Keystore oluşturulamadı!"
        exit 1
    fi
    echo "✅ Keystore oluşturuldu: $KEYSTORE_FILE"
    echo "⚠️  BU DOSYAYI YEDEKLEYİN! Kaybedersen uygulamayı güncelleyemezsin."
    echo ""
fi

# ─── 2. Şifre ────────────────────────────────────────────
read -s -p "🔐 Keystore şifresi (varsayılan: aaplus123): " PASS
PASS=${PASS:-aaplus123}
echo ""
export KEYSTORE_PASSWORD="$PASS"

# ─── 3. Temizle ──────────────────────────────────────────
echo "🧹 Temizleniyor..."
rm -rf bin obj

# ─── 4. Release Build (AAB) ──────────────────────────────
echo "🏗️  Release AAB build ediliyor..."
dotnet publish AAPlus.csproj \
    -f net10.0-android \
    -c Release \
    -p:AndroidKeyStore=true \
    -p:AndroidSigningKeyStore="$KEYSTORE_FILE" \
    -p:AndroidSigningKeyAlias="$KEYSTORE_ALIAS" \
    -p:AndroidSigningStorePass="$PASS" \
    -p:AndroidSigningKeyPass="$PASS"

if [ $? -ne 0 ]; then
    echo "❌ Build başarısız!"
    exit 1
fi

# ─── 5. Sonuç ────────────────────────────────────────────
AAB_PATH=$(find bin -name "*.aab" | head -1)
APK_PATH=$(find bin -name "*-Signed.apk" | head -1)

echo ""
echo "═══════════════════════════════════════════════════"
echo "✅ BUILD BAŞARILI!"
echo "═══════════════════════════════════════════════════"
if [ -n "$AAB_PATH" ]; then
    SIZE=$(du -h "$AAB_PATH" | cut -f1)
    echo "📦 AAB: $AAB_PATH ($SIZE)"
    echo "   → Bu dosyayı Google Play Console'a yükle"
fi
if [ -n "$APK_PATH" ]; then
    SIZE=$(du -h "$APK_PATH" | cut -f1)
    echo "📱 APK: $APK_PATH ($SIZE)"
    echo "   → Test için: adb install \"$APK_PATH\""
fi
echo ""
echo "⚠️  UNUTMA:"
echo "   1. aaplus.keystore dosyasını güvenli yerde sakla"
echo "   2. Şifreyi unutma"
echo "   3. Google Play Console'da AAB'ı yükle"
echo "═══════════════════════════════════════════════════"
