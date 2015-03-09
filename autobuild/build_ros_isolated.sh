#!/bin/bash
env -i HOME=$HOME USERNAME=$USERNAME NUMBER_OF_PROCESSORS=$NUMBER_OF_PROCESSORS /bin/bash --login -c "$(dirname $(readlink -f $0))/build_ros.sh $*"