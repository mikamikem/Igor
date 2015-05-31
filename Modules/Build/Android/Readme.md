Android Build Module
=============

## Summary

This module adds support for building to Android and some useful utility functions for Android platform build plugins.

## Description

The Android build module requires a Unity license that supports building to Android!  This module allows you to build Android in a way that we can extend with other Igor modules.

## Jenkins Setup

This module does not require any special Jenkins setup.

## Project Setup

This module does not require any special project setup.

## Runtime Info

This module only runs in the editor.

## Developer Info

### IgorBuildAndroid.AddNewLibrary()

If you are creating a new Android module that uses a standalone library project that must be compiled into the final application, make sure to call this function with your library's name so that it is appropriately compiled and linked with the final executable.

### IgorBuildAndroid.GetAndroidSDKPath()

This function returns the Android SDK path in case you need to run any Android SDK tools on your module's files.

### IgorBuildAndroid.SwapStringValueInStringsXML()

This utility function allows you to easily swap a string value by key in a given Android string.xml file.  You need to run this on each of the localizations you support.