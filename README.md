[![pipeline status](https://gitlab.linphone.org/BC/public/linphone-xamarin/badges/master/pipeline.svg)](https://gitlab.linphone.org/BC/public/linphone-xamarin/commits/master)

# Xamarin SDK

To use Linphone with Xamarin, you need the Xamarin SDK which contains the native libraries for Android and iOS (for each architecture) and the C# wrapper that matches those libraries.

You can find a nightly build here: [Snapshots](http://linphone.org/snapshots/xamarin/) and our latest release here: [Releases](http://linphone.org/releases/xamarin/)

## What's in the box

The Xamarin SDK embed the following:

* The C# wrapper (named LinphoneWrapper.cs) ;
* The Android libraries for armv7, arm64 and x86 ;
* The Linphone java classes as an AAR (liblinphone-sdk.aar) which is required for Android (debug and release flavors) ;
* The iOS libraries for armv7, arm64 and x86_64 (as frameworks) ;
* A sample solution using a shared project that contains a Xamarin Forms application along with Android and iOS projects.

## Building the SDK

To build the sdk, clone the linphone SDK [git repository](https://gitlab.linphone.org/BC/public/linphone-sdk.git) and follow the README instructions.

## Getting started

### Using our sample solution in the SDK

The sample we provide is a solution using four projects: one for Android, one for iOS, one for the native libraries and a shared one.

The shared one contains most of the UI stuff. It has an application and a default view that allow the user to register his SIP account to a proxy and make/receive calls. 
It also includes the C# wrapper from the SDK.

The Android project contains the Android Manifest for the generated APK and an Activity that will load and display the application from the shared project.

The iOS project does the same thing that the Android one, but for iOS (obviously).

Finally, the Liblinphone project contains the native libraries built by linphone-sdk.

### Add the SDK binaries and wrapper

#### For Android

In the linphone sdk xamarin zip file, copy from linphone-sdk-android directory either the debug or release AAR into Xamarin\Xamarin\Liblinphone\liblinphone-sdk.aar (the name must remain the same)

Also copy the file linphone-sdk-ios\linphone-sdk\apple-darwin\share\linphonecs\LinphoneWrapper.cs into Xamarin\Xamarin\Xamarin\LinphoneWrapper.cs (the name must remain the same)

That's all! Generate the solution and you can deploy the sample app on any Android device.

#### For iOS

For the C# wrapper to work, it needs to find the Linphone native libraries. On IOS, here's the procedure to add them to the project:

* Import the Frameworks folder with all the frameworks within your Xamarin.iOS project ;
* Right click on your Xamarin.iOS project then select add -> add native references and select all the frameworks you imported in the project.

Do not forget to add your required permissions in your project Info.plist (i.e: use of microphone etc...) or your app will crash !
