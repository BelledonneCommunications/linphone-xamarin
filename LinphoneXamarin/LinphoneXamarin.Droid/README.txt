Getting started:
- Put somewhere the .so files (in a dir which name matches the abi)
- For each .so file, set BuildAction in Visual Studio to AndroidNativeLibrary
- Put somewhere the liblinphone.jar and in Visual Studio set BuildAction to AndroidJavaLibrary

Changes required for Android (in the MainActivity.cs for example):
- The JNI is not being used, so you must call first the ms_set_jvm_from_env method to set the JavaVM pointer.
- Like a classic Android application, you have to load the libraries manually using Java.Lang.JavaSystem.LoadLibrary() before loading the application.