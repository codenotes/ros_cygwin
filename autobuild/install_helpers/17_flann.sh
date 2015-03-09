#!/bin/bash

if [ -e /usr/local/lib/libflann.dll.a ]; then
	echo "  Found flann"
	exit 0
fi

FLANN_VERSION=1.8.4
test -d "$1" || (echo "Missing base directory" ; exit 1)
cd $1

test -e flann-$FLANN_VERSION-src.zip || wget http://www.cs.ubc.ca/research/flann/uploads/FLANN/flann-$FLANN_VERSION-src.zip || exit 1
test -d flann-$FLANN_VERSION-src || unzip flann-$FLANN_VERSION-src.zip || exit 1
cd flann-$FLANN_VERSION-src || exit 1

mkdir -p build
cd build || exit 1
test -e Makefile || cmake .. || exit 1
make $PARALLEL_BUILD_FLAGS || exit 1
make install || exit 1
