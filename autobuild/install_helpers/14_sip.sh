#!/bin/bash
if [ -e /usr/lib/python2.7/site-packages/sipdistutils.py ]; then
	echo "  Found sip"
	exit 0
fi

test -d "$1" || (echo "Missing base directory" ; exit 1)
cd $1

#SIP_VERSION=4.15.5
SIP_VERSION=4.16.2

test -e sip-$SIP_VERSION.tar.gz || wget http://$SOURCEFORGE_MIRROR/project/pyqt/sip/sip-$SIP_VERSION/sip-$SIP_VERSION.tar.gz
test -d sip-$SIP_VERSION || tar xf sip-$SIP_VERSION.tar.gz || exit 1
cd sip-$SIP_VERSION|| exit 1
test -e Makefile || python configure.py || exit 1
make || exit 1
make install || exit 1
