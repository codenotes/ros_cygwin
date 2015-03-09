#!/bin/ash
ROS_WORKSPACE=/opt/ros
echo Searching for additional libraries...
/bin/find /usr/local/bin /usr/local/lib $ROS_WORKSPACE | /bin/grep \\.dll$ > all_libs.lst
/bin/echo Found `/bin/wc -l < all_libs.lst` additional libraries. Rebasing...
/bin/rebaseall -T all_libs.lst
/bin/rm all_libs.lst