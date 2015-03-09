#!/bin/bash
if [ -d /usr/local/share/urdfdom ]; then
	echo "  Found urdfdom"
	exit 0
fi

test -d "$1" || (echo "Missing base directory" ; exit 1)
cd $1

test -e urdfdom || (git clone https://github.com/ros/urdfdom.git && patch -d urdfdom -p1 < $(dirname $0)/../patches/3rdparty/urdfdom.patch) || exit 1
cd urdfdom || exit 1
test -e Makefile || cmake .
make || exit 1
make install || exit 1
