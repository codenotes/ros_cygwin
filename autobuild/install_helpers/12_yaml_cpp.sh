#!/bin/bash
if [ -e /usr/local/lib/libyaml-cpp.a ]; then
	echo "  Found yaml_cpp"
	exit 0
fi

test -d "$1" || (echo "Missing base directory" ; exit 1)
cd $1

YAML_CPP_VERSION=0.3.0		#Newer versions are not compatible as of Fall 2014
PACKAGE_URL=release-$YAML_CPP_VERSION
PACKAGE_BASE=yaml-cpp-$PACKAGE_URL
test -e $PACKAGE_URL.tar.gz || wget https://github.com/jbeder/yaml-cpp/archive/$PACKAGE_URL.tar.gz || exit 1
test -d $PACKAGE_BASE || (tar xf $PACKAGE_URL.tar.gz) || exit 1
cd $PACKAGE_BASE || exit 1
mkdir -p build
cd build || exit 1
test -e Makefile || cmake .. || exit 1
make || exit 1
make install || exit 1
