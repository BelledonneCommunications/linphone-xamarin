#!/usr/bin/env python

############################################################################
# prepare.py
# Copyright (C) 2015  Belledonne Communications, Grenoble France
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

import os
import re
import shutil
import sys
from distutils.spawn import find_executable
from logging import error, warning, info
from subprocess import Popen, PIPE
sys.dont_write_bytecode = True
sys.path.insert(0, 'submodules/cmake-builder')
try:
    import prepare
except Exception as e:
    error(
        "Could not find prepare module: {}, probably missing submodules/cmake-builder? Try running:\n"
        "git submodule sync && git submodule update --init --recursive".format(e))
    exit(1)



class IOSTarget(prepare.Target):

    def __init__(self, arch):
        prepare.Target.__init__(self, 'ios-' + arch, 'iOS')
        current_path = os.path.dirname(os.path.realpath(__file__))
        self.config_file = 'configs/config-ios-' + arch + '.cmake'
        self.toolchain_file = 'toolchains/toolchain-ios-' + arch + '.cmake'
        self.output = 'iOS/liblinphone-sdk/' + arch + '-apple-darwin.ios'
	self.external_source_path = os.path.join(current_path, 'submodules')


class IOSi386Target(IOSTarget):

    def __init__(self):
        IOSTarget.__init__(self, 'i386')


class IOSx8664Target(IOSTarget):

    def __init__(self):
        IOSTarget.__init__(self, 'x86_64')


class IOSarmv7Target(IOSTarget):

    def __init__(self):
        IOSTarget.__init__(self, 'armv7')


class IOSarm64Target(IOSTarget):

    def __init__(self):
        IOSTarget.__init__(self, 'arm64')



ios_targets = {
    'i386': IOSi386Target(),
    'x86_64': IOSx8664Target(),
    'armv7': IOSarmv7Target(),
    'arm64': IOSarm64Target()
}

ios_virtual_targets = {
    'devices': ['armv7', 'arm64'],
    'simulators': ['i386', 'x86_64'],
    'all': ['i386', 'x86_64', 'armv7', 'arm64']
}

class IOSPreparator(prepare.Preparator):

    def __init__(self, targets=ios_targets, virtual_targets=ios_virtual_targets):
        prepare.Preparator.__init__(self, targets, default_targets=['armv7', 'arm64', 'x86_64'], virtual_targets=virtual_targets)
        self.veryclean = True
        self.show_gpl_disclaimer = True
        self.argparser.add_argument('-ac', '--all-codecs', help="Enable all codecs, including the non-free ones. Final application must comply with their respective license (see README.md).", action='store_true')

    def parse_args(self):
        prepare.Preparator.parse_args(self)

        if self.args.all_codecs:
            self.additional_args += ["-DENABLE_GPL_THIRD_PARTIES=ON"]
            self.additional_args += ["-DENABLE_NON_FREE_CODECS=ON"]
            self.additional_args += ["-DENABLE_AMRNB=ON"]
            self.additional_args += ["-DENABLE_AMRWB=ON"]
            self.additional_args += ["-DENABLE_BV16=ON"]
            self.additional_args += ["-DENABLE_CODEC2=ON"]
            self.additional_args += ["-DENABLE_G729=ON"]
            self.additional_args += ["-DENABLE_GSM=ON"]
            self.additional_args += ["-DENABLE_ILBC=ON"]
            self.additional_args += ["-DENABLE_ISAC=ON"]
            self.additional_args += ["-DENABLE_OPUS=ON"]
            self.additional_args += ["-DENABLE_SILK=ON"]
            self.additional_args += ["-DENABLE_SPEEX=ON"]
            self.additional_args += ["-DENABLE_OPENH264=ON"]
            self.additional_args += ["-DENABLE_VPX=ON"]

    def clean(self):
        prepare.Preparator.clean(self)
        if os.path.isfile('Makefile'):
            os.remove('Makefile')
        if os.path.isdir('iOS') and not os.listdir('iOS'):
            os.rmdir('iOS')
        #if os.path.isdir('liblinphone-sdk'):
        #    l = os.listdir('liblinphone-sdk')
        #    if len(l) == 1 and l[0] == 'apple-darwin':
        #        shutil.rmtree('liblinphone-sdk', ignore_errors=False)


    def detect_package_manager(self):
        if find_executable("brew"):
            return "brew"
        elif find_executable("port"):
            return "sudo port"
        else:
            error("No package manager found. Please README or install brew using:\n\truby -e \"$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/master/install)\"")
            return "brew"

    def check_environment(self):
        reterr = 0
        reterr |= prepare.Preparator.check_environment(self)
        package_manager_info = {"brew-pkg-config": "pkg-config",
                                "sudo port-pkg-config": "pkgconfig",
                                "brew-binary-path": "/usr/local/bin/",
                                "sudo port-binary-path": "/opt/local/bin/"
                                }

        for prog in ["autoconf", "automake", "doxygen", "java", "nasm", "cmake", "wget", "yasm", "optipng"]:
            reterr |= not self.check_is_installed(prog, prog)

        reterr |= not self.check_is_installed("pkg-config", package_manager_info[self.detect_package_manager() + "-pkg-config"])
        reterr |= not self.check_is_installed("ginstall", "coreutils")
        reterr |= not self.check_is_installed("intltoolize", "intltool")
        reterr |= not self.check_is_installed("convert", "imagemagick")

        if find_executable("nasm"):
            nasm_output = Popen("nasm -f elf32".split(" "), stderr=PIPE, stdout=PIPE).stderr.read()
            if "fatal: unrecognised output format" in nasm_output:
                error("Invalid version of nasm detected. Please make sure that you are NOT using Apple's binary here")
                self.missing_dependencies["nasm"] = "nasm"
                reterr = 1

        if self.check_is_installed("libtoolize", "libtoolize", warn=False):
            if not self.check_is_installed("glibtoolize", "libtool"):
                reterr = 1
                glibtoolize_path = find_executable("glibtoolize")
                if glibtoolize_path is not None:
                    msg = "Please do a symbolic link from glibtoolize to libtoolize:\n\tln -s {} ${}"
                    error(msg.format(glibtoolize_path, glibtoolize_path.replace("glibtoolize", "libtoolize")))

        devnull = open(os.devnull, 'wb')
        # just ensure that JDK is installed - if not, it will automatically display a popup to user
        p = Popen("java -version".split(" "), stderr=devnull, stdout=devnull)
        p.wait()
        if p.returncode != 0:
            error("Please install Java JDK (not just JRE).")
            reterr = 1

        p = Popen("xcrun --sdk iphoneos --show-sdk-path".split(" "), stdout=devnull, stderr=devnull)
        p.wait()
        if p.returncode != 0:
            error("iOS SDK not found, please install Xcode from AppStore or equivalent.")
            reterr = 1
        else:
            xcode_version = int(
                Popen("xcodebuild -version".split(" "), stdout=PIPE).stdout.read().split("\n")[0].split(" ")[1].split(".")[0])
            if xcode_version < 7:
                if not find_executable("strings"):
                    sdk_strings_path = Popen("xcrun --find strings".split(" "), stdout=PIPE).stdout.read().split("\n")[0]
                    error("strings binary missing, please run:\n\tsudo ln -s {} {}".format(sdk_strings_path, package_manager_info[detect_package_manager() + "-binary-path"]))
                    reterr = 1
        return reterr

    def show_missing_dependencies(self):
        if self.missing_dependencies:
            error("The following binaries are missing: {}. Please install them using:\n\t{} install {}".format(
                " ".join(self.missing_dependencies.keys()),
                self.detect_package_manager(),
                " ".join(self.missing_dependencies.values())))

    def generate_makefile(self, generator, project_file=''):
        platforms = self.args.target
        arch_targets = ""
        for arch in platforms:
            arch_targets += """
{arch}: {arch}-build

{arch}-build:
\t{generator} iOS/ios-{arch}/cmake/{project_file}
\t@echo "Done"
""".format(arch=arch, generator=generator, project_file=project_file)
        multiarch = ""
        for arch in platforms[1:]:
            multiarch += \
                """\tif test -f "$${arch}_path"; then \\
\t\tall_paths=`echo $$all_paths $${arch}_path`; \\
\t\tall_archs="$$all_archs,{arch}" ; \\
\telse \\
\t\techo "WARNING: archive `basename $$archive` exists in {first_arch} tree but does not exists in {arch} tree: $${arch}_path."; \\
\tfi; \\
""".format(first_arch=platforms[0], arch=arch)
        makefile = """
archs={archs}
LINPHONE_IPHONE_VERSION=$(shell git describe --always)

.PHONY: all
.SILENT: sdk
all: generate-ios-sdk

sdk:
\tarchives=`find iOS/liblinphone-sdk/{first_arch}-apple-darwin.ios -name '*.framework'` && \\
\trm -rf iOS/liblinphone-sdk/apple-darwin && \\
\tmkdir -p iOS/liblinphone-sdk/apple-darwin && \\
\tcp iOS/liblinphone-sdk/{first_arch}-apple-darwin.ios/share/linphonecs/LinphoneWrapper.cs Xamarin/Xamarin/Xamarin && \\
\tcp -rf iOS/liblinphone-sdk/{first_arch}-apple-darwin.ios/share iOS/liblinphone-sdk/apple-darwin/. && \\
\tcp -rf iOS/liblinphone-sdk/{first_arch}-apple-darwin.ios/lib iOS/liblinphone-sdk/apple-darwin/. && \\
\tcp -rf iOS/liblinphone-sdk/{first_arch}-apple-darwin.ios/include iOS/liblinphone-sdk/apple-darwin/. && \\
\tcp -rf iOS/liblinphone-sdk/{first_arch}-apple-darwin.ios/Frameworks iOS/liblinphone-sdk/apple-darwin/. && \\
\tfor archive in $$archives ; do \\
\t\tarmv7_path=`echo $$archive | sed -e "s/{first_arch}/armv7/"`; \\
\t\tarm64_path=`echo $$archive | sed -e "s/{first_arch}/arm64/"`; \\
\t\ti386_path=`echo $$archive | sed -e "s/{first_arch}/i386/"`; \\
\t\tx86_64_path=`echo $$archive | sed -e "s/{first_arch}/x86_64/"`; \\
\t\tdestpath=`echo $$archive | sed -e "s/-debug//" | sed -e "s/{first_arch}-//" | sed -e "s/\.ios//"`; \\
\t\tall_paths=`echo $$archive`; \\
\t\tall_archs="{first_arch}"; \\
\t\tarchive_name=`basename $$archive`; \\
\t\tframework_name=`echo $$archive_name | cut -d '.' -f 1`; \\
\t\tmkdir -p `dirname $$destpath`; \\
\t\t{multiarch} \\
\t\techo "[{archs}] Mixing `basename $$archive` in $$destpath"; \\
\t\tlipo -create -output $$destpath/$$framework_name $$armv7_path/$$framework_name $$arm64_path/$$framework_name $$x86_64_path/$$framework_name; \\
\tdone; \\

generate-ios-sdk: $(addsuffix -build, $(archs))
\t$(MAKE) sdk

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
\t@echo "   * all or generate-ios-sdk: builds all architectures and creates the liblinphone SDK"
\t@echo "   * sdk: creates the liblinphone SDK. Use this only after a full build"
\t@echo ""
""".format(archs=' '.join(platforms), arch_opts='|'.join(platforms),
           first_arch=platforms[0], options=' '.join(sys.argv),
           arch_targets=arch_targets,
           multiarch=multiarch, generator=generator)
        f = open('Makefile.ios', 'w')
        f.write(makefile)
        f.close()



def main():
    preparator = IOSPreparator()
    preparator.parse_args()
    if preparator.check_environment() != 0:
        preparator.show_environment_errors()
        return 1
    return preparator.run()

if __name__ == "__main__":
    sys.exit(main())

