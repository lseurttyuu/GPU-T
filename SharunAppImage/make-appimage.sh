#!/bin/sh
set -eu

ARCH=$(uname -m)
# Extract version from main .csproj file
VERSION=$(awk -F'<|>' '/<Version>/{print $3; exit}' ./GPU-T.csproj)

export ARCH VERSION
export OUTNAME="GPU-T-${VERSION}-${ARCH}.AppImage"
export OUTPATH=./dist
export ICON=./Assets/app_icon.png
export DEPLOY_VULKAN=0
export STRACE_MODE=0

mkdir -p ./AppDir/bin

# 1. Compile the AOT Sidecar (This inherently runs as self-contained)
echo "Compiling NVAPI Sidecar (AOT)..."
dotnet publish Nvapi/GPU-T.Nvapi.csproj -c Release -r linux-x64 -o ./AppDir/bin \
    -p:DebugSymbols=false \
    -p:DebugType=None

# 2. Compile the Main App as a self-contained donet binary
echo "Compiling GPU-T Main App..."
dotnet publish GPU-T.csproj -c Release -r linux-x64 -o ./publish_output \
    --self-contained true \
    -p:PublishTrimmed=true \
    -p:TrimMode=partial \
    -p:JsonSerializerIsReflectionEnabledByDefault=true \
    -p:DebugSymbols=false \
    -p:DebugType=None \
    -p:PublishDocumentationFiles=false \
    -p:SatelliteResourceLanguages="en"

# 2. Aggressive Cleanup
rm -f ./publish_output/*.xml
rm -f ./publish_output/*.pdb
# only loaded for tracing, pulls in liblttng-ust
rm -f ./publish_output/libcoreclrtraceptprovider.so

# 3. Deploy app directly into AppDir/bin
mkdir -p ./AppDir/bin
cp -r ./publish_output/* ./AppDir/bin/

# ==============================================================================
# FIX FOR SKIASHARP / HARFBUZZ SYMBOL COLLISION (SIGSEGV on First Launch)
# ==============================================================================
# PROBLEM: 
# SkiaSharp bundles its own 'libHarfBuzzSharp.so'. The Linux host system uses 
# 'libharfbuzz.so.0' for native OS font scanning. Because they share the exact 
# same symbol names, they both load into memory. When SkiaSharp tries to free a 
# font object created by the OS library, the memory manager panics and throws a 
# "free(): invalid pointer" crash.
#
# SOLUTION: 
# Force both SkiaSharp and the Linux system to use the exact same, single library.
# We do this by bundling the build runner's native HarfBuzz into our AppImage, 
# and replacing SkiaSharp's wrapper with a RELATIVE symlink.
# ==============================================================================

# 3.1. Dynamically find the path of the native HarfBuzz on the CI/CD build runner
HARFBUZZ_PATH=$(ldconfig -p | awk '/libharfbuzz.so.0/ {print $4; exit}')

# 3.2. Copy it directly into our AppImage payload
cp "$HARFBUZZ_PATH" ./AppDir/bin/libharfbuzz.so.0

# 3.3. Delete SkiaSharp's bundled wrapper
rm -f ./AppDir/bin/libHarfBuzzSharp.so

# 3.4. Create a RELATIVE symlink. When SkiaSharp asks for 'libHarfBuzzSharp.so', 
# it gets seamlessly redirected to our bundled 'libharfbuzz.so.0' sitting right 
# next to it. Memory collision prevented!
ln -s libharfbuzz.so.0 ./AppDir/bin/libHarfBuzzSharp.so

cp ./SharunAppImage/gpu-t.desktop ./AppDir/

# 4. Build the heavily optimized container
quick-sharun \
    ./AppDir/bin/* \
    /usr/lib/libSM.so*  \
    /usr/lib/libICE.so* \
    /usr/lib/libicuuc.so* \
    /usr/lib/libicui18n.so*

# 6. Turn AppDir into AppImage and Test
quick-sharun --make-appimage
quick-sharun --test ./dist/*.AppImage
