#!/bin/bash
if [ -e /usr/local/lib/libconsole_bridge.dll.a ]; then
	echo "  Found console_bridge"
	exit 0
fi

test -d "$1" || (echo "Missing base directory" ; exit 1)
cd $1
test -d console_bridge || (git clone git://github.com/ros/console_bridge.git || exit 1)
cd console_bridge || exit 1
cmake . || exit 1
make || exit 1
make install || exit 1
