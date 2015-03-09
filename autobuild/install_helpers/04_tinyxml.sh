#!/bin/bash
if [ -e /usr/lib/libtinyxml.dll.a ]; then
	echo "  Found tinyxml"
	exit 0
fi
test -d "$1" || (echo "Missing base directory" ; exit 1)
cd $1
TINYXML_VERSION=2.6.2
TINYXML_VERSION_PLAIN=2_6_2

test -e tinyxml_$TINYXML_VERSION_PLAIN.tar.gz || wget http://$SOURCEFORGE_MIRROR/project/tinyxml/tinyxml/$TINYXML_VERSION/tinyxml_$TINYXML_VERSION_PLAIN.tar.gz || exit 1
test -d tinyxml || (tar xf tinyxml_$TINYXML_VERSION_PLAIN.tar.gz && patch -d tinyxml -p1 < $(dirname $0)/../patches/3rdparty/tinyxml.patch) || exit 1
cd tinyxml || exit 1
make TINYXML_USE_STL=YES || exit 1
g++ -shared tinystr.o tinyxml.o tinyxmlerror.o tinyxmlparser.o -o cygtinyxml.dll -Wl,--export-all-symbols  -Wl,--out-implib=libtinyxml.dll.a || exit 1
cp *.h /usr/include || exit 1
cp libtinyxml.dll.a /usr/lib || exit 1
cp cygtinyxml.dll /usr/bin || exit 1
