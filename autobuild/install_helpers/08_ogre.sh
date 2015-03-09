#!/bin/bash
if [ -e /usr/local/lib/libOgreMain.dll.a ]; then
	echo "  Found ogre"
	exit 0
fi

test -d "$1" || (echo "Missing base directory" ; exit 1)
cd $1

OGRE_VERSION=1-8-1
OGRE_VERSION_BASE=1.8
OGRE_VERSION_ALT=`echo $OGRE_VERSION | sed s/-/./g`

test -e ogre_src_v$OGRE_VERSION.tar.bz2 || wget http://$SOURCEFORGE_MIRROR/project/ogre/ogre/$OGRE_VERSION_BASE/$OGRE_VERSION_ALT/ogre_src_v$OGRE_VERSION.tar.bz2 || exit 1
test -d ogre_src_v$OGRE_VERSION || (tar xf ogre_src_v$OGRE_VERSION.tar.bz2 && patch -d ogre_src_v$OGRE_VERSION -p1 < $(dirname $0)/../patches/3rdparty/ogre.patch) || exit 1
cd ogre_src_v$OGRE_VERSION || exit 1
mkdir -p build
cd build || exit 1
test -e Makefile || cmake .. -DOGRE_BUILD_SAMPLES=FALSE || exit 1
echo Building Ogre...
make $PARALLEL_BUILD_FLAGS || exit 1
make install || exit 1
