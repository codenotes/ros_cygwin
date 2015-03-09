#!/bin/bash

COLLADA_DOM_VERSION=2.4.0
COLLADA_DOM_VERSION_BASE=2.4

if [ -d /usr/local/include/collada-dom2.4 ]; then
	echo "  Found collada_dom"
	exit 0
fi

test -d "$1" || (echo "Missing base directory" ; exit 1)
cd $1


test -e collada-dom-$COLLADA_DOM_VERSION.tgz || wget http://$SOURCEFORGE_MIRROR/project/collada-dom/Collada%20DOM/Collada%20DOM%20$COLLADA_DOM_VERSION_BASE/collada-dom-$COLLADA_DOM_VERSION.tgz || exit 1
test -d collada-dom-$COLLADA_DOM_VERSION || (tar xf collada-dom-$COLLADA_DOM_VERSION.tgz && patch -d collada-dom-$COLLADA_DOM_VERSION -p1 < $(dirname $0)/../patches/3rdparty/collada_dom.patch) || exit 1
cd collada-dom-$COLLADA_DOM_VERSION || exit 1

mkdir -p build
cd build || exit 1
test -e Makefile || cmake .. || exit 1
make $PARALLEL_BUILD_FLAGS || exit 1
make install || exit 1
