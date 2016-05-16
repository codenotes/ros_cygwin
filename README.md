# ros_cygwin
Cygwin port of ROS

This repo contains relatively complete binary releases of ROS Jade and Indigo for the Windows environment (via Cygwin, 64 bit only). 

Setup/installs (https://github.com/codenotes/ros_cygwin/releases) are included which will install a pre-configured cygwin environment to your Windows installation that is set up with the appropriate binaries which approximate a "desktop-full" release of ROS for windows.  These include the cor ROS libraries, key packages and tools (such as RVIZ/RQT) etc.  

Process should be turnkey and "just work." upon launching the cygwin shell after install.  The goal is to create a complete working environment in cygwin for Windows that operates the same as the Ubuntu distros.  Many of the tools (rqt_*, rviz, etc.) are present and work. Some tools and libraries may be present or missing and work or not work...this distro has not been heavily tested outside of my group's particular use case, so you will certainly find issues.  Cygwin is terrific, but it is an imperfect linux.  Take a look at the FAQ for more information. 

All scripts for the setup/install applicaiton as well as all patches required to create the cygwin build of the ROS binaries (including patches to various environments such as PCL and Boost) are included in this site.  These are not plug and play, however, strongly suggest you just leverage the binaries.  

Please note that if you build from source with your own cygwin install, you are very likely to encounter build errors as Cygwin itself changes its libraries frequently.  It is best to use the binaries provided along with the cygwin environment also provided via the installers.

Please forgive the lack of specific documentation, this is a work in progress. 

Strongly suggest you read the FAQ document.  A fair amount of detail in there. 

INSTRUCTIONS:
Once it is installed, run the .bat file installed to your desktop.  You will see a bash command prompt. From there type:

source /opt/ros/install_isolated/setup.bash

And then maybe:

export ROS_IP=<your IP address>

And now things should work the way they would (mostly) on ubuntu.  


Gregory Brill

