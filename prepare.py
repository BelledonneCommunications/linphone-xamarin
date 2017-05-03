#!/usr/bin/env python

############################################################################
# prepare.py
# Copyright (C) 2016  Belledonne Communications, Grenoble France
#
############################################################################
#
# This program is free software; you can redistribute it and/or
# modify it under the terms of the GNU General Public License
# as published by the Free Software Foundation; either version 2
# of the License, or (at your option) any later version.
#
# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.
#
# You should have received a copy of the GNU General Public License
# along with this program; if not, write to the Free Software
# Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
#
############################################################################

import fnmatch
import os
import re
import sys
from distutils.spawn import find_executable
from logging import error, warning, info
from subprocess import Popen
from android import AndroidPreparator
from ios import IOSPreparator
sys.dont_write_bytecode = True
sys.path.insert(0, 'submodules/cmake-builder')
try:
    import prepare
except Exception as e:
    error(
        "Could not find prepare module: {}, probably missing submodules/cmake-builder? Try running:\n"
        "git submodule sync && git submodule update --init --recursive".format(e))
    exit(1)

def main():
    android = AndroidPreparator()
    if android.check_environment() != 0:
        android.show_environment_errors()
        return 1
    android.parse_args()
    android.run()

    ios = IOSPreparator()
    if ios.check_environment() != 0:
            ios.show_environment_errors()
            return 1
    ios.parse_args()
    ios.run()

    makefile = """
.PHONY: all
.NOTPARALLEL: all

VERSION=$(shell git --git-dir=submodules/linphone/.git --work-tree=submodules/linphone describe)

include Makefile.android
include Makefile.ios

all: generate-android-sdk generate-ios-sdk sdk

sdk:
\tzip -r liblinphone-xamarin-sdk-$(VERSION).zip Xamarin
"""
    f = open('Makefile', 'w')
    f.write(makefile)
    f.close()
    return 0

if __name__ == "__main__":
    sys.exit(main())    
