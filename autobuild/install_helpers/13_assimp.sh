#!/bin/bash
if [ -e /usr/local/lib/libassimp.dll.a ]; then
	echo "  Found assimp"
	exit 0
fi

test -d "$1" || (echo "Missing base directory" ; exit 1)
cd $1

ASSIMP_VERSION=2.0.863
ASSIMP_VERSION_BASE=2.0

test -e assimp--$ASSIMP_VERSION-sdk.zip || wget http://$SOURCEFORGE_MIRROR/project/assimp/assimp-$ASSIMP_VERSION_BASE/assimp--$ASSIMP_VERSION-sdk.zip
test -d assimp--$ASSIMP_VERSION-sdk || (unzip assimp--$ASSIMP_VERSION-sdk.zip && patch -d assimp--$ASSIMP_VERSION-sdk -p1 < $(dirname $0)/../patches/3rdparty/assimp.patch) || exit 1
cd assimp--$ASSIMP_VERSION-sdk || exit 1
test -e code/ByteSwap2.h || mv code/ByteSwap.h code/ByteSwap2.h || exit 1	#Conflict with another instance of ByteSwap.h from a different module
mkdir -p build
cd build || exit 1
test -e Makefile || cmake .. || exit 1
make $PARALLEL_BUILD_FLAGS || exit 1
make install || exit 1
