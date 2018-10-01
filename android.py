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
sys.dont_write_bytecode = True
sys.path.insert(0, 'submodules/cmake-builder')
try:
    import prepare
except Exception as e:
    error(
        "Could not find prepare module: {}, probably missing submodules/cmake-builder? Try running:\n"
        "git submodule sync && git submodule update --init --recursive".format(e))
    exit(1)



class AndroidTarget(prepare.Target):

    def __init__(self, arch):
        prepare.Target.__init__(self, 'android-' + arch, 'android')
        current_path = os.path.dirname(os.path.realpath(__file__))
        self.config_file = 'configs/config-android.cmake'
        self.toolchain_file = 'toolchains/toolchain-android-' + arch + '.cmake'
        self.output = 'android/liblinphone-sdk/android-' + arch
        self.external_source_path = os.path.join(current_path, 'submodules')
        external_builders_path = os.path.join(current_path, 'cmake_builder')
        self.additional_args = [
            "-DLINPHONE_BUILDER_EXTERNAL_BUILDERS_PATH=" + external_builders_path
        ]


class AndroidArmTarget(AndroidTarget):

    def __init__(self):
        AndroidTarget.__init__(self, 'arm')
        self.additional_args += ['-DENABLE_VIDEO=NO']


class AndroidArmv7Target(AndroidTarget):

    def __init__(self):
        AndroidTarget.__init__(self, 'armv7')


class AndroidArm64Target(AndroidTarget):

    def __init__(self):
        AndroidTarget.__init__(self, 'arm64')


class AndroidX86Target(AndroidTarget):

    def __init__(self):
        AndroidTarget.__init__(self, 'x86')


class AndroidX86_64Target(AndroidTarget):

    def __init__(self):
        AndroidTarget.__init__(self,'x86_64')


android_targets = {
    'arm': AndroidArmTarget(),
    'armv7': AndroidArmv7Target(),
    'arm64': AndroidArm64Target(),
    'x86': AndroidX86Target(),
    'x86_64': AndroidX86_64Target()
}

android_virtual_targets = {
    'all': ['armv7', 'arm64', 'x86', 'x86_64']
}

class AndroidPreparator(prepare.Preparator):

    def __init__(self, targets=android_targets):
        prepare.Preparator.__init__(self, targets, default_targets=['armv7', 'arm64'], virtual_targets=android_virtual_targets)
        self.min_supported_ndk = 16
        self.max_supported_ndk = 17
        self.unsupported_ndk_version = None
        self.min_cmake_version = "3.10"
        self.release_with_debug_info = True
        self.veryclean = True
        self.show_gpl_disclaimer = True
        self.argparser.add_argument('-ac', '--all-codecs', help="Enable all codecs, including the non-free ones", action='store_true')

    def parse_args(self):
        prepare.Preparator.parse_args(self)

        if self.args.all_codecs:
            self.additional_args += ["-DENABLE_GPL_THIRD_PARTIES=YES"]
            self.additional_args += ["-DENABLE_NON_FREE_CODECS=YES"]
            self.additional_args += ["-DENABLE_AMRNB=YES"]
            self.additional_args += ["-DENABLE_AMRWB=YES"]
            self.additional_args += ["-DENABLE_BV16=YES"]
            self.additional_args += ["-DENABLE_CODEC2=YES"]
            self.additional_args += ["-DENABLE_G729=YES"]
            self.additional_args += ["-DENABLE_GSM=YES"]
            self.additional_args += ["-DENABLE_ILBC=YES"]
            self.additional_args += ["-DENABLE_ISAC=YES"]
            self.additional_args += ["-DENABLE_OPUS=YES"]
            self.additional_args += ["-DENABLE_SILK=YES"]
            self.additional_args += ["-DENABLE_SPEEX=YES"]
            self.additional_args += ["-DENABLE_FFMPEG=YES"]
            self.additional_args += ["-DENABLE_H263=YES"]
            self.additional_args += ["-DENABLE_H263P=YES"]
            self.additional_args += ["-DENABLE_MPEG4=YES"]
            self.additional_args += ["-DENABLE_OPENH264=YES"]
            self.additional_args += ["-DENABLE_VPX=YES"]
            # self.additional_args += ["-DENABLE_X264=YES"] # Do not activate x264 because it has text relocation issues

    def list_feature_target(self):
        return android_targets['armv7']

    def check_ndk_version(self):
        retval = True
        ndk_build = find_executable('ndk-build')
        ndk_path = os.path.dirname(ndk_build)
        # NDK prior to r11 had a RELEASE.TXT file holding the version number
        release_file = os.path.join(ndk_path, 'RELEASE.TXT')
        if os.path.isfile(release_file):
            version = open(release_file).read().strip()
            res = re.match('^r(\d+)(.*)$', version)
            version = int(res.group(1))
            retval = False
        else:
            # Hack to find the NDK version since the RELEASE.TXT file is no longer there
            python_config_files = []
            for root, dirnames, filenames in os.walk(ndk_path):
                for filename in fnmatch.filter(filenames, 'python-config'):
                    python_config_files.append(os.path.join(root, filename))
            if len(python_config_files) > 0:
                version = open(python_config_files[0]).readlines()[0]
                res = re.match('^.*/(aosp-)?ndk-r(\d+).*$', version)
                if res is not None: # Will be if NDK < 16
                    version = int(res.group(2))
                    retval = False
                else:
                    release_file = os.path.join(ndk_path, 'source.properties') # Since NDK 16
                    if os.path.isfile(release_file):
                        version = open(release_file).read().strip()
                        res = re.findall(r'(?:(\d+))', version)
                        version = int(res[0])
                        retval = False
                    else:
                        error("Could not get Android NDK version!")
                        sys.exit(-1)
            else:
                error("Could not get Android NDK version!")
                sys.exit(-1)
        if retval == False and (version < self.min_supported_ndk or version > self.max_supported_ndk):
            self.unsupported_ndk_version = version
            retval = True
        return retval

    def check_environment(self):
        ret = 0
        ret_ndk = not self.check_is_installed('ndk-build', 'Android NDK r{}'.format(self.max_supported_ndk))
        if not ret_ndk:
            ret_ndk = self.check_ndk_version()
        ret |= ret_ndk
        ret |= prepare.Preparator.check_environment(self)
        return ret

    def show_environment_errors(self):
        if self.unsupported_ndk_version is not None:
            error("Unsupported Android NDK r{}. Please install version r{}.".format(self.unsupported_ndk_version, self.max_supported_ndk))
        else:
            prepare.Preparator.show_environment_errors(self)

    def clean(self):
        prepare.Preparator.clean(self)
        if os.path.isfile('Makefile.android'):
            os.remove('Makefile.android')
        if os.path.isdir('android') and not os.listdir('android'):
            os.rmdir('android')
        if os.path.isdir('liblinphone-sdk') and not os.listdir('liblinphone-sdk'):
            os.rmdir('liblinphone-sdk')

    def prepare(self):
        self.download_gradle()
        prepare.Preparator.prepare(self)

    def download_gradle(self):
        os.system('./gradlew')

    def generate_makefile(self, generator, project_file=''):
        platforms = self.args.target
        arch_targets = ""
        for arch in platforms:
            arch_targets += """
android-{arch}: android-{arch}-build

android-{arch}-build:
\t{generator} android/android-{arch}/cmake
\t@echo "Done"
""".format(arch=arch, generator=generator)
        makefile = """
android-archs={archs}
TOPDIR=$(shell pwd)

.PHONY: all
.NOTPARALLEL: all

all: generate-android-sdk

android-build: $(addprefix android-, $(addsuffix -build, $(android-archs)))

clean: java-clean

java-clean:
\t./gradlew clean

android-copy-libs:
\trm -rf android/liblinphone-sdk/res
\trm -rf android/libs-debug/armeabi
\trm -rf android/libs/armeabi
\tif test -d "android/liblinphone-sdk/android-arm"; then \\
\t\tmkdir -p android/libs-debug/armeabi && \\
\t\tcp -f android/liblinphone-sdk/android-arm/lib/lib*.so android/libs-debug/armeabi && \\
\t\tcp -f android/liblinphone-sdk/android-arm/lib/mediastreamer/plugins/*.so android/libs-debug/armeabi && \\
\t\tmkdir -p android/libs/armeabi && \\
\t\tcp -f android/liblinphone-sdk/android-arm/lib/lib*.so android/libs/armeabi && \\
\t\tcp -f android/liblinphone-sdk/android-arm/lib/mediastreamer/plugins/*.so android/libs/armeabi && \\
\t\tcp android/liblinphone-sdk/android-arm/share/linphonecs/LinphoneWrapper.cs Xamarin/Xamarin/Xamarin/ && \\
\t\tsh android/android-arm/strip.sh android/libs/armeabi/*.so; \\
\tfi
\tif test -f "android/liblinphone-sdk/android-arm/lib/wrap.sh"; then \\
\t\tmkdir -p android/liblinphone-sdk/res/lib/armeabi && \\
\t\tcp -f android/liblinphone-sdk/android-arm/lib/wrap.sh android/liblinphone-sdk/res/lib/armeabi; \\
\tfi
\trm -rf android/libs-debug/armeabi-v7a
\trm -rf android/libs/armeabi-v7a
\tif test -d "android/liblinphone-sdk/android-armv7"; then \\
\t\tmkdir -p android/libs-debug/armeabi-v7a && \\
\t\tcp -f android/liblinphone-sdk/android-armv7/lib/lib*.so android/libs-debug/armeabi-v7a && \\
\t\tcp -f android/liblinphone-sdk/android-armv7/lib/mediastreamer/plugins/*.so android/libs-debug/armeabi-v7a && \\
\t\tmkdir -p android/libs/armeabi-v7a && \\
\t\tcp -f android/liblinphone-sdk/android-armv7/lib/lib*.so android/libs/armeabi-v7a && \\
\t\tcp -f android/liblinphone-sdk/android-armv7/lib/mediastreamer/plugins/*.so android/libs/armeabi-v7a && \\
\t\tcp android/liblinphone-sdk/android-armv7/share/linphonecs/LinphoneWrapper.cs Xamarin/Xamarin/Xamarin/ && \\
\t\tsh android/android-armv7/strip.sh android/libs/armeabi-v7a/*.so; \\
\tfi
\tif test -f "android/liblinphone-sdk/android-armv7/lib/wrap.sh"; then \\
\t\tmkdir -p android/liblinphone-sdk/res/lib/armeabi-v7a && \\
\t\tcp -f android/liblinphone-sdk/android-armv7/lib/wrap.sh android/liblinphone-sdk/res/lib/armeabi-v7a; \\
\tfi
\trm -rf android/libs-debug/arm64-v8a
\trm -rf android/libs/arm64-v8a
\tif test -d "android/liblinphone-sdk/android-arm64"; then \\
\t\tmkdir -p android/libs-debug/arm64-v8a && \\
\t\tcp -f android/liblinphone-sdk/android-arm64/lib/lib*.so android/libs-debug/arm64-v8a && \\
\t\tcp -f android/liblinphone-sdk/android-arm64/lib/mediastreamer/plugins/*.so android/libs-debug/arm64-v8a && \\
\t\tmkdir -p android/libs/arm64-v8a && \\
\t\tcp -f android/liblinphone-sdk/android-arm64/lib/lib*.so android/libs/arm64-v8a && \\
\t\tcp -f android/liblinphone-sdk/android-arm64/lib/mediastreamer/plugins/*.so android/libs/arm64-v8a && \\
\t\tcp android/liblinphone-sdk/android-arm64/share/linphonecs/LinphoneWrapper.cs Xamarin/Xamarin/Xamarin/ && \\
\t\tsh android/android-arm64/strip.sh android/libs/arm64-v8a/*.so; \\
\tfi
\tif test -f "android/liblinphone-sdk/android-arm64/lib/wrap.sh"; then \\
\t\tmkdir -p android/liblinphone-sdk/res/lib/arm64-v8a && \\
\t\tcp -f android/liblinphone-sdk/android-arm64/lib/wrap.sh android/liblinphone-sdk/res/lib/arm64-v8a; \\
\tfi
\trm -rf android/libs-debug/x86
\trm -rf android/libs/x86
\tif test -d "android/liblinphone-sdk/android-x86"; then \\
\t\tmkdir -p android/libs-debug/x86 && \\
\t\tcp -f android/liblinphone-sdk/android-x86/lib/lib*.so android/libs-debug/x86 && \\
\t\tcp -f android/liblinphone-sdk/android-x86/lib/mediastreamer/plugins/*.so android/libs-debug/x86 && \\
\t\tmkdir -p android/libs/x86 && \\
\t\tcp -f android/liblinphone-sdk/android-x86/lib/lib*.so android/libs/x86 && \\
\t\tcp -f android/liblinphone-sdk/android-x86/lib/mediastreamer/plugins/*.so android/libs/x86 && \\
\t\tcp android/liblinphone-sdk/android-x86/share/linphonecs/LinphoneWrapper.cs Xamarin/Xamarin/Xamarin/ && \\
\t\tsh android/android-x86/strip.sh android/libs/x86/*.so; \\
\tfi
\tif test -f "android/liblinphone-sdk/android-x86/lib/wrap.sh"; then \\
\t\tmkdir -p android/liblinphone-sdk/res/lib/x86 && \\
\t\tcp -f android/liblinphone-sdk/android-x86/lib/wrap.sh android/liblinphone-sdk/res/lib/x86; \\
\tfi

generate-android-sdk: clean android-build android-copy-libs create-jar

create-jar:
\t./gradlew assembleRelease
\tcp ./bin/outputs/aar/liblinphone-sdk-*.aar Xamarin/Xamarin/Liblinphone/liblinphone-sdk.aar

{arch_targets}

help-prepare-options:
\t@echo "prepare.py was previously executed with the following options:"
\t@echo "   {options}"

help: help-prepare-options
\t@echo ""
\t@echo "(please read the README.md file first)"
\t@echo ""
\t@echo "Available architectures: {archs}"
\t@echo ""
\t@echo "Available targets:"
\t@echo ""
\t@echo "   * all or generate-apk: builds all architectures and creates the linphone application APK"
\t@echo "   * generate-sdk: builds all architectures and creates the liblinphone SDK"
\t@echo "   * install: install the linphone application APK (run this only after generate-apk)"
\t@echo "   * uninstall: uninstall the linphone application"
\t@echo ""
""".format(archs=' '.join(platforms), arch_opts='|'.join(platforms),
           first_arch=platforms[0], options=' '.join(sys.argv),
           arch_targets=arch_targets, generator=generator)
        f = open('Makefile.android', 'w')
        f.write(makefile)
        f.close()

def main():
    preparator = AndroidPreparator()
    preparator.parse_args()
    if preparator.check_environment() != 0:
        preparator.show_environment_errors()
        return 1
    return preparator.run()

if __name__ == "__main__":
    sys.exit(main())
