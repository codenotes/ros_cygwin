#!/bin/bash
if [ -e /usr/local/lib/libbz2.a ]; then
	echo "  Found libbzip2"
	exit 0
fi
test -d "$1" || (echo "Missing base directory" ; exit 1)
cd $1
BZIP2_VERSION=1.0.6

test -e bzip2-$BZIP2_VERSION.tar.gz || wget http://www.bzip.org/$BZIP2_VERSION/bzip2-$BZIP2_VERSION.tar.gz || exit 1
test -d bzip2-$BZIP2_VERSION || (tar xf bzip2-$BZIP2_VERSION.tar.gz) || exit 1
cd bzip2-$BZIP2_VERSION || exit 1
make || exit 1
make install || exit 1