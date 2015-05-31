iOS Build Module
=============

## Summary

This module adds support for building to iOS and some useful utility functions for iOS platform build plugins.

## Description

The iOS build module requires a Unity license that supports building to iOS!  This module allows you to build iOS in a way that we can extend with other Igor modules.

This module uses the following external GitHub projects:

[plist-cil](https://github.com/claunia/plist-cil) - For parsing and modifying plist files.
[XCodeEditor-for-Unity](https://github.com/dcariola/XCodeEditor-for-Unity) - For parsing and modifying XCode project files.

## Jenkins Setup

This module does not require any special Jenkins setup.

## Project Setup

This module does not require any special project setup.

## Runtime Info

This module only runs in the editor.

## Developer Info

This module includes a fully functional plist editor and XCode project editor.  The iOS Build module uses these libraries directly while also providing some helper functions (listed below) that make the process for common tasks much easier.

### IgorPlistUtils.SetBoolValue()

This function allows you to set a bool value in a given plist file.

### IgorPlistUtils.SetStringValue()

This function allows you to set a string value in a given plist file.

### IgorPlistUtils.GetStringValue()

This function allows you to get a string value from a given plist file.

### IgorPlistUtils.AddRequiredDeviceCapability()

This function adds a required device capability to a given plist file.

### IgorPlistUtils.AddBundleURLType()

This function adds a Bundle URL type to a given plist file.

### IgorXCodeProjUtils.AddOrUpdateForAllBuildProducts()

This function adds a key value pair to all build products (this could be used for things like CODE_SIGN_IDENTITY, etc.).

### IgorXCodeProjUtils.AddFrameworkSearchPath()

This function adds a framework search path.

### IgorXCodeProjUtils.AddNewBuildFile()

This function adds a new Objective-C source file to be built into the game.

### IgorXCodeProjUtils.AddFramework()

This function adds a new Framework to be built and linked into the game.