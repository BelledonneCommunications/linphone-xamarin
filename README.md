# Xamarin SDK

To use Linphone with Xamarin, you need the Xamarin SDK which contains the native libraries for Android and iOS (for each architecture) and the C# wrapper that matches those libraries.

You can build your own SDK (see [Linphone C# wrapper](https://wiki.linphone.org/xwiki/wiki/public/view/Lib/Linphone%20C%23%20wrapper/)) or you can download one from our [nightly builds](https://linphone.org/snapshots/xamarin/).

## What's in the box

The Xamarin SDK embed the following:

* The C# wrapper (named LinphoneWrapper.cs) ;
* The Android libraries for armv7, arm64 and x86 ;
* The Linphone java classes as a jar (liblinphone.jar) which is required for Android ;
* The iOS libraries for armv7, arm64 and x86_64 (as frameworks) ;
* A sample solution using a shared project that contains a Xamarin Forms application along with Android and iOS projects.

If you want to support an architecture that is not included in our SDK (for example armv5), you can compile the libraries by yourself (see [Linphone C# wrapper](https://wiki.linphone.org/xwiki/wiki/public/view/Lib/Linphone%20C%23%20wrapper/)).

## Building the SDK

To build the sdk, run the following commands :
./prepare.py -c && ./prepare.py && make

In order to only build the SDK for iOS or Android, you can add the following options to your ./prepare.py call : "-DENABLE_IOS=OFF" or "-DENABLE_ANDROID=OFF".

## Getting started

Once you have our SDK (either built by yourself or downloaded from our snapshots), you can either start your application using the preconfigured solution in the SDK, or you may already have a Xamarin solution and want to add Linphone to it.

Here's how to do each one of them.

### Using our sample solution in the SDK

The sample we provide is a solution using three projects: one for Android, one for iOS and a shared one.

The shared one contains most of the stuff. It has an application and a default view that allow the user to register his SIP account to a proxy and make/receive calls.

The Android project contains the Android Manifest for the generated APK and an Activity that will load and display the application from the shared project. It also contains the Linphone native libraries.

The iOS project does the same thing that the Android one, but for iOS (obviously).

### Adding linphone to your existing solution

If you already have a Xamarin solution and want to add Linphone to it, here are the changes you need to make in order to be able to use Linphone API.
#### Shared project

If you have a shared project (Xamarin forms or not), you can add the LinphoneWrapper.cs file (the C# wrapper that is automatically generated) into this project.

This way, it will make Linphone namespace available from both iOS and Android projects if they reference the shared one (which is most likely), and of course you'll be able to use Linphone API from the shared project itself.

This is the solution we chose for our sample (see above).

#### Portable class library project

When you created your Xamarin solution, you may have chosen the PCL project instead of the shared one. If you did and assuming you don't have a shared project (in this case see Shared project above), you must add the LinphoneWrapper.cs in each project (Android and iOS) because you can't use platform invoke features in a PCL project, and these features are mandatory for our wrapper.

#### Android project

For the C# wrapper to work, it needs to find the Linphone native libraries. On Android, here's the procedure to add them to the project:

1. Create a "Libs" directory in the root directory of the project ;
1. Create a directory for each architecture you want to support named with the eabi code (armeabi-v7a, arm64, armv5, x86, etc...) ;
1. Add in each directory the libraries (.so) ;
1. For each .so, set the Build Action to AndroidNativeLibrary ;
1. In the "Libs" directory, add the liblinphone.jar and set it's Build Action to AndroidJavaLibrary.

On Android, you must manually load the libraries before the first call to Linphone API. Here's the code:
```java
Java.Lang.JavaSystem.LoadLibrary("bctoolbox");
Java.Lang.JavaSystem.LoadLibrary("ortp");
Java.Lang.JavaSystem.LoadLibrary("mediastreamer_base");
Java.Lang.JavaSystem.LoadLibrary("mediastreamer_voip");
Java.Lang.JavaSystem.LoadLibrary("linphone");
```

Finally, you need to give to Linphone the context of the application, and it must be done before the first call to Linphone API:
```java
LinphoneAndroid.setAndroidContext(Android.Runtime.JNIEnv.Handle, this.Handle);
```

If Visual/Xamarin Studio doesn't find the LinphoneAndroid symbol, ensure you have added the ANDROID conditional compilation symbol in the Android project.

Of course, don't forget to edit the AndroidManifest.xml of your application to add the required permissions if you haven't done it yet (for example RECORD_AUDIO if you intend to do audio calls).

#### iOS project

For the C# wrapper to work, it needs to find the Linphone native libraries. On IOS, here's the procedure to add them to the project:

1. Import the Frameworks folder with all the frameworks within your Xamarin.iOS project ;
1. Right click on your Xamarin.iOS project then select add -> add native references and select all the frameworks you imported in the project.

Do not forget to add your required permissions in your project Info.plist (i.e: use of microphone etc...) or your app will crash !