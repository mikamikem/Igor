Core Module
=============

## Summary

This module contains all of the shared functionality that makes writing other modules easier as well as the editor configuration window.

## Description

This module contains the common utility functions necessary for Igor to run jobs as well as useful functions for other modules to maintain consistency in logging, assertions, configuration, and other uses.

## Jenkins Setup

This module does not require any special Jenkins setup.

## Project Setup

This module does not require any changes to your project.

## Runtime Info

This module is fully available at runtime except for the configuration window and a few utility functions.

## Developer Info

This module contains all of the basic utilities and base classes you need to write your own modules.  Here is a quick summary of the most useful files:

### IgorAssert.cs

Contains the assertion functions that are used throughout Igor.

### IgorBuiltinLogger.cs

This contains a reference implementation for the IIgorLogger interface that simply calls the various Debug.Log* functions.  Look into this function if you plan to implement a custom logging module.

### IgorModuleBase.cs

IgorModuleBase is what you should derive all of your module classes from.  This provides a basic implementation of useful logging and configuration functions for you to help maintain consistency across modules.

### IgorUtils.cs

IgorUtils is a general purpose utility class (part of which is implemented in the IgorUpdater.cs file).  This contains useful functions for file operations, string parsing, job configuration, and more!

### IgorUtils.GetLevels()

This gets the list of levels enabled for the build in the Editor Build Settings.