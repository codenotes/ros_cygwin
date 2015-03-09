#!/bin/bash
if [ -e /usr/local/include/eigen3/Eigen ]; then
	echo "  Found eigen"
	exit 0
fi
test -d "$1" || (echo "Missing base directory" ; exit 1)
cd $1
EIGEN_VERSION=3.2.1
test -e $EIGEN_VERSION.tar.bz2 || wget  http://bitbucket.org/eigen/eigen/get/$EIGEN_VERSION.tar.bz2
test -d eigen-* || tar xf $EIGEN_VERSION.tar.bz2
cd eigen-* || exit 1
mkdir -p build || exit 1
cd build || exit 1
cmake .. || exit 1
make || exit 1
make install || exit 1