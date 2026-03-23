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

# 1. Compile the app with strict size-reduction flags
echo "Compiling GPU-T..."
dotnet publish GPU-T.csproj -c Release -r linux-x64 --no-self-contained -o ./publish_output \
    -p:DebugSymbols=false \
    -p:DebugType=None \
    -p:PublishDocumentationFiles=false \
    -p:SatelliteResourceLanguages="en"

# 2. Aggressive Cleanup
rm -f ./publish_output/*.xml || true
rm -f ./publish_output/*.pdb || true

find ./publish_output -name "*.so" -exec strip --strip-unneeded {} + || true

# 3. Setup the Desktop file 
export DESKTOP=/usr/share/applications/gpu-t.desktop
mkdir -p /usr/share/applications
cp ./SharunAppImage/gpu-t.desktop $DESKTOP

sed -i 's/^Exec=.*/Exec=GPU-T/' $DESKTOP

# 4. Deploy app directly into AppDir/bin
mkdir -p ./AppDir/bin
cp -r ./publish_output/* ./AppDir/bin/

# 5. Build the heavily optimized container
quick-sharun \
    ./AppDir/bin/* \
    /usr/lib/libSM.so*  \
    /usr/lib/libICE.so* \
    /usr/lib/libicuuc.so*

# 6. Tell the native binary where the runtime is
echo 'DOTNET_ROOT=${SHARUN_DIR}/bin' >> ./AppDir/.env

# 7. Turn AppDir into AppImage and Test
quick-sharun --make-appimage
quick-sharun --test ./dist/*.AppImage
