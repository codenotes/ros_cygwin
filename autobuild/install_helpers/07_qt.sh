#!/bin/bash

QT_VERSION=4.8.6
QT_VERSION_BASE=4.8

if [ -e /usr/local/lib/libQtCore.a ]; then
	echo "  Found Qt"
	exit 0
fi
test -d "$1" || (echo "Missing base directory" ; exit 1)
cd $1

test -e qt-everywhere-opensource-src-$QT_VERSION.zip || wget http://download.qt.io/official_releases/qt/$QT_VERSION_BASE/$QT_VERSION/qt-everywhere-opensource-src-$QT_VERSION.zip || exit 1
test -d qt-everywhere-opensource-src-$QT_VERSION || (unzip qt-everywhere-opensource-src-$QT_VERSION.zip && chown -R $USERNAME qt-everywhere-opensource-src-$QT_VERSION && patch -d qt-everywhere-opensource-src-$QT_VERSION -p1 < $(dirname $0)/../patches/3rdparty/qt.patch) || exit 1
cd qt-everywhere-opensource-src-$QT_VERSION || exit 1
echo Configuring Qt...
set -o igncr
export SHELLOPTS
test -e Makefile || ./configure -opensource -no-xinput -no-accessibility -no-freetype -platform win32-cygwin-g++ -no-qt3support -confirm-license -no-sql-sqlite -no-webkit --prefix=/usr/local || exit 1
echo Building Qt...
make $PARALLEL_BUILD_FLAGS || exit 1
echo Installing Qt...
make install || exit 1

echo Creating library symlinks...
for lib in `ls -1 /usr/local/lib/libQt*.dll.*`; do
	filename=`echo $lib | sed 's/.*\(libQt.*\\.dll.*\)/\\1/'`
	basename=`echo $lib | sed 's/.*\(libQt.*\)\\.dll.*/\\1/'`
	ln -s $filename /usr/local/lib/$basename.dll
	ln -s $basename.dll /usr/local/lib/$basename.a
done
