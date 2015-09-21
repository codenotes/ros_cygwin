# ros_cygwin
Cygwin port of ROS

This repo contains relatively complete binary releases of ROS Jade and Indigo for the Windows environment (via Cygwin). 

Setup/installs (https://github.com/codenotes/ros_cygwin/releases) are included which will install a pre-configured cygwin environment to your Windows installation that is set up with the appropriate binaries which approximate a "desktop-full" release of ROS for windows.  These include the cor ROS libraries, key packages and tools (such as RVIZ/RQT) etc.  

Process should be turnkey and "just work." upon launching the cygwin shell after install. 

All scripts for the setup/install applicaiton as well as all patches required to create the cygwin build of the ROS binaries (including patches to various environments such as PCL and Boost) are included in this site. 

Please note that if you build from source with your own cygwin install, you are very likely to encounter build errors as Cygwin itself changes its libraries frequently.  It is best to use the binaries provided along with the cygwin environment also provided via the installers.

Please forgive the lack of specific documentation, this is a work in progress. 

Gregory Brill
gbrill@infusion.com
