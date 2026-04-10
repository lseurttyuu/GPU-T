#!/bin/sh
set -eu

echo "Installing build tools and application dependencies..."
echo "---------------------------------------------------------------"
pacman -Sy --noconfirm \
    dotnet-sdk-9.0 \
    dotnet-host \
    dotnet-runtime-9.0 \
    vulkan-tools \
    clinfo \
    mesa \
    mesa-utils \
    libva-utils \
    pciutils \
    desktop-file-utils \
    hicolor-icon-theme \
    clang \
    zlib

echo "Installing debloated packages (Mesa without LLVM)..."
echo "---------------------------------------------------------------"
get-debloated-pkgs --add-common --prefer-nano