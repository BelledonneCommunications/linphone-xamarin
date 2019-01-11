[![pipeline status](https://gitlab.linphone.org/BC/public/linphone-xamarin/badges/master/pipeline.svg)](https://gitlab.linphone.org/BC/public/linphone-xamarin/commits/master)

# Xamarin SDK

To use Linphone with Xamarin, you need the Xamarin SDK which contains the native libraries for Android and iOS (for each architecture) and the C# wrapper that matches those libraries.

You can build your own SDK (see [Linphone C# wrapper](https://wiki.linphone.org/xwiki/wiki/public/view/Lib/Linphone%20C%23%20wrapper/)) or you can download one from our [nightly builds](https://linphone.org/snapshots/xamarin/).

## What's in the box

The Xamarin SDK embed the following:

* The C# wrapper (named LinphoneWrapper.cs) ;
* The Android libraries for armv7, arm64 and x86 ;
* The Linphone java classes as a jar (liblinphone-sdk.aar) which is required for Android ;
* The iOS libraries for armv7, arm64 and x86_64 (as frameworks) ;
* A sample solution using a shared project that contains a Xamarin Forms application along with Android and iOS projects.

If you want to support an architecture that is not included in our SDK (for example armv5), you can compile the libraries by yourself (see [Linphone C# wrapper](https://wiki.linphone.org/xwiki/wiki/public/view/Lib/Linphone%20C%23%20wrapper/)).

## Building the SDK

To build the sdk, go to linphone-sdk folder and follow README instructions for Android (don't forget to enable the C# wrapper in the cmake options).
Then copy (or create a symbolic link) from ./linphone-sdk/bin/sdk-assets/assets/org.linphone.core/LinphoneWrapper.cs to ./Xamarin/Xamarin/Xamarin/LinphoneWrapper.cs
Do the same for ./linphone-sdk/bin/outputs/aar/linphone-sdk-android-debug.aar to ./Xamarin/Xamarin/Liblinphone/liblinphone-sdk.aar

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

1. Create a [Binding Aar](https://developer.xamarin.com/guides/android/advanced_topics/binding-a-java-library/binding-an-aar/) with the liblinphone-sdk.aar(You can find it in the zip [here](https://www.linphone.org/releases/android/liblinphone-android-sdk-latest.zip)).
1. Add to the Binding Aar project in Transforms/Metadata.xml
```
<remove-node path="/api/package[@name='org.linphone.core']"/>
```
1. Add to your Android project the reference of the Binding Aar created.

On Android, you must manually load the libraries before the first call to Linphone API. Here's the code:
```java
Java.Lang.JavaSystem.LoadLibrary("bctoolbox");
Java.Lang.JavaSystem.LoadLibrary("ortp");
Java.Lang.JavaSystem.LoadLibrary("mediastreamer");
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

#### UWP project

With or without Xamarin, our wrapper can be used in an UWP project.

Her's how to create an UWP project:

1. Follow the instructions available [here](https://developer.xamarin.com/guides/xamarin-forms/platform-features/windows/installation/universal/). If you don't want to use Xamarin, skip step 2 and stop after step 3.
1. Add a reference in the UWP project to the shared project LinphoneXamarin.
1. Download the nuget package [here](http://linphone.org/snapshots/windows10/LinphoneSDKlatest.nupkg).
1. Install the nuget package of LinphoneSdk for your UWP project(Instruction [here](https://wiki.linphone.org/xwiki/wiki/public/view/Lib/Getting%20started/Windows%20UWP/#HInstalllocalnugetpackage)).


### Adding video

#### Android

1. Add a TextureView to the [Xamarin.Forms.Layout](https://developer.xamarin.com/guides/xamarin-forms/user-interface/layouts/) where will be displayed the video.
2. Set Core.NativeVideoWindowId to the created TextureView ptr(Object.Handler).
3. Ensure Core.VideoDisplayEnabled is set to true.
4. Do the same to display the captured video using Core.NativePreviewWindowId and Core.VideoCaptureEnabled.

#### UWP

To get/set NativeVideoWindowId and NativePreviewWindowId, you must use NativeVideoWindowIdString and NativePreviewWindowIdString
