#!/bin/bash
if [ -e /usr/lib/liblz4.a ]; then
	echo "  Found lz4"
	exit 0
fi
test -d "$1" || (echo "Missing base directory" ; exit 1)
cd $1
LZ4_VERSION=r121

test -e lz4-$LZ4_VERSION.zip || wget https://github.com/Cyan4973/lz4/archive/$LZ4_VERSION.zip -O lz4-$LZ4_VERSION.zip || exit 1
test -d lz4-$LZ4_VERSION || (unzip lz4-$LZ4_VERSION.zip) || exit 1
cd lz4-$LZ4_VERSION || exit 1
make || echo "Build failed, but the liblz4.a needed by ROS may be usable. Checking..."	#Make will fail due to unsupported _fileno() in the code that we don't need
test -e liblz4.a || exit 1
cp liblz4.a /usr/lib || exit 1
cp *.h /usr/include