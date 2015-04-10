# ros_cygwin
Cygwin port of ROS

The contained shell script should download and build the ROS Core Libraries as well as a number of common packages and tools
including rviz.

For Windows, XP and above.  Majority of testing on Windows 7 and 8.1.

Requires Cygwin64.  32 will work, but you can run into limitations in terms of plugins so 64bt recommended. 

If you encounter errors in the build it is often do to environmental variables being inherited from the Windows host by cygwin. 
Follow the internal documentation and build from a clean shell. 

Work in progress, but looking good so far.  

Gregory Brill
gbrill@infusion.com
