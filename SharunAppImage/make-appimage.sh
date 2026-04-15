#!/bin/sh
set -eu

ARCH=$(uname -m)
VERSION=$(git describe --tags --always | sed 's/^v//') 

export ARCH VERSION
export OUTPATH=./dist
export ICON=./Assets/app_icon.png
export DEPLOY_DOTNET=1
export DEPLOY_VULKAN=0
export STRACE_MODE=0

mkdir -p ./AppDir/bin

# 1. Compile the AOT Sidecar (This inherently runs as self-contained)
echo "Compiling NVAPI Sidecar (AOT)..."
dotnet publish Nvapi/GPU-T.Nvapi.csproj -c Release -r linux-x64 -o ./AppDir/bin \
    -p:DebugSymbols=false \
    -p:DebugType=None

# 2. Compile the Main App with strict size-reduction flags
echo "Compiling GPU-T Main App..."
dotnet publish GPU-T.csproj -c Release -r linux-x64 --no-self-contained -o ./publish_output \
    -p:DebugSymbols=false \
    -p:DebugType=None \
    -p:PublishDocumentationFiles=false \
    -p:SatelliteResourceLanguages="en"

# 2. Aggressive Cleanup
rm -f ./publish_output/*.xml || true
rm -f ./publish_output/*.pdb || true

find ./publish_output -name "*.so" -exec strip --strip-unneeded {} + || true

# 3. Deploy app directly into AppDir/bin
mkdir -p ./AppDir/bin
cp -r ./publish_output/* ./AppDir/bin/

cp ./SharunAppImage/gpu-t.desktop ./AppDir/

# 4. Build the heavily optimized container
quick-sharun \
    ./AppDir/bin/* \
    /usr/lib/libSM.so*  \
    /usr/lib/libICE.so* \
    /usr/lib/libicuuc.so*

# 6. Turn AppDir into AppImage and Test
quick-sharun --make-appimage
quick-sharun --test ./dist/*.AppImage
