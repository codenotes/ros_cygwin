#!/bin/bash
if [ -e /usr/local/lib/liborocos-bfl.dll.a ]; then
	echo "  Found orocos_bfl"
	exit 0
fi

test -d "$1" || (echo "Missing base directory" ; exit 1)
cd $1

OROCOS_BFL_VERSION=0.8.0
test -e orocos-bfl-$OROCOS_BFL_VERSION-src.tar.bz2 || wget http://people.mech.kuleuven.be/~tdelaet/bfl_tar/orocos-bfl-$OROCOS_BFL_VERSION-src.tar.bz2 || exit 1
test -d orocos-bfl-$OROCOS_BFL_VERSION || (tar xf orocos-bfl-$OROCOS_BFL_VERSION-src.tar.bz2) || exit 1
cd orocos-bfl-$OROCOS_BFL_VERSION || exit 1
mkdir -p build
cd build || exit 1
test -e Makefile || cmake .. || exit 1
make || exit 1
make install || exit 1
