#!/bin/bash
if [ -d /usr/local/share/urdfdom_headers ]; then
	echo "  Found urdfdom_headers"
	exit 0
fi

test -d "$1" || (echo "Missing base directory" ; exit 1)
cd $1

test -e urdfdom_headers || git clone https://github.com/ros/urdfdom_headers.git || exit 1
cd urdfdom_headers || exit 1
test -e Makefile || cmake .
make || exit 1
make install || exit 1
