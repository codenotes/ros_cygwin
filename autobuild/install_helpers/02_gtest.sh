#!/bin/bash
if [ -e /usr/include/gtest/gtest.h ]; then
	echo "  Found gtest"
	exit 0
fi

test -d "$1" || (echo "Missing base directory" ; exit 1)
cd $1
PACKAGE_BASE=gtest-1.7.0
test -e $PACKAGE_BASE.zip || wget https://googletest.googlecode.com/files/$PACKAGE_BASE.zip
test -d $PACKAGE_BASE || unzip $PACKAGE_BASE.zip
cd $PACKAGE_BASE || exit 1
cmake . || exit 1
make || exit 1
cp -a include/gtest /usr/include
cp -a *.a /usr/lib/
