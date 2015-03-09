#!/bin/bash
if [ -e /usr/local/lib/libyaml-cpp.a ]; then
	echo "  Found yaml_cpp"
	exit 0
fi

test -d "$1" || (echo "Missing base directory" ; exit 1)
cd $1

YAML_CPP_VERSION=0.3.0		#Newer versions are not compatible as of Fall 2014
test -e yaml-cpp-$YAML_CPP_VERSION.tar.gz || wget https://yaml-cpp.googlecode.com/files/yaml-cpp-$YAML_CPP_VERSION.tar.gz || exit 1
test -d yaml-cpp || (tar xf yaml-cpp-$YAML_CPP_VERSION.tar.gz) || exit 1
cd yaml-cpp || exit 1
mkdir -p build
cd build || exit 1
test -e Makefile || cmake .. || exit 1
make || exit 1
make install || exit 1
