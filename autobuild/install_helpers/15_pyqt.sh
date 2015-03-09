#!/bin/bash
if [ -e /usr/lib/python2.7/site-packages/PyQt4/pyqtconfig.py ]; then
	echo "  Found pyqt"
	exit 0
fi

test -d "$1" || (echo "Missing base directory" ; exit 1)
cd $1

PYQT_VERSION=4.11.1

test -e PyQt-win-gpl-$PYQT_VERSION.zip || wget http://$SOURCEFORGE_MIRROR/project/pyqt/PyQt4/PyQt-$PYQT_VERSION/PyQt-win-gpl-$PYQT_VERSION.zip
test -d PyQt-win-gpl-$PYQT_VERSION || (unzip PyQt-win-gpl-$PYQT_VERSION.zip && patch -d PyQt-win-gpl-$PYQT_VERSION -p1 < $(dirname $0)/../patches/3rdparty/pyqt.patch) || exit 1
cd PyQt-win-gpl-$PYQT_VERSION || exit 1
export PATH=$PATH:/usr/local/lib
test -e Makefile || python configure-ng.py --confirm-license || exit 1

make $PARALLEL_BUILD_FLAGS || exit 1
make install || exit 1

#Looks like configure.py is the legacy configuration script that does not produce valid code anymore, but the new configure-ng.py script does not generate pyqtconfig.py needed by ROS.
#We work around this by patching configure.py to ONLY produce the pyqtconfig.py and NOT touch any other files (see related patch)
test -e pyqtconfig.py || python configure.py --confirm-license || exit 1
cp pyqtconfig.py /usr/lib/python2.7/site-packages/PyQt4/pyqtconfig.py

cd /usr/lib/python2.7/site-packages/PyQt4 || exit 1
for module in `ls -1 *.pyd`; do
	renamed_module=`echo $module | sed s/\\.pyd/\\.dll/`
	test -e $renamed_module || (echo Copying $module to $renamed_module... && cp $module $renamed_module) || exit 1
done
