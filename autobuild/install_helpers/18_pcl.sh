#!/bin/bash

if [ -e /usr/local/lib/libpcl_common.dll.a ]; then
	echo "  Found pcl"
	exit 0
fi

test -d "$1" || (echo "Missing base directory" ; exit 1)
cd $1

PCL_VERSION=1.7.1

test -e pcl-$PCL_VERSION.zip || wget https://github.com/PointCloudLibrary/pcl/archive/pcl-$PCL_VERSION.zip || exit 1
test -d pcl-pcl-$PCL_VERSION || (unzip pcl-$PCL_VERSION.zip && patch -d pcl-pcl-$PCL_VERSION -p1 < $(dirname $0)/../patches/3rdparty/pcl.patch) || exit 1
cd pcl-pcl-$PCL_VERSION || exit 1

mkdir -p build
cd build || exit 1
test -e Makefile || cmake .. -DCMAKE_BUILD_TYPE=Release || exit 1	#Building a version with debug symbols reults in a "too many sections" error for sac_segmentation.cpp
make $PARALLEL_BUILD_FLAGS || exit 1
make install || exit 1
