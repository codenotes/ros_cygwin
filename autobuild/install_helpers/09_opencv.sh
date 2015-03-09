#!/bin/bash
if [ -e /usr/local/lib/libopencv_core.dll.a ]; then
	echo "  Found opencv"
	exit 0
fi

test -d "$1" || (echo "Missing base directory" ; exit 1)
cd $1

OPENCV_VERSION=2.4.9

test -e opencv-$OPENCV_VERSION.zip || wget http://$SOURCEFORGE_MIRROR/project/opencvlibrary/opencv-unix/$OPENCV_VERSION/opencv-$OPENCV_VERSION.zip || exit 1
test -d opencv-$OPENCV_VERSION || (unzip opencv-$OPENCV_VERSION.zip && patch -d opencv-$OPENCV_VERSION -p1 < $(dirname $0)/../patches/3rdparty/opencv.patch) || exit 1
cd opencv-$OPENCV_VERSION || exit 1
mkdir -p build
cd build || exit 1
test -e Makefile || cmake .. || exit 1
echo Building OpenCV...
make $PARALLEL_BUILD_FLAGS || exit 1
make install || exit 1
