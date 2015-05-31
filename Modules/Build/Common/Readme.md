Common Build Module
=============

## Summary

This module provides common functionality for building projects.

## Description

This module does not enable building any particular platform, but provides necessary utility and helper functions that are useful across all platform builds.

## Jenkins Setup

This module does not require any special Jenkins setup.

## Project Setup

This module does not require any special project setup.

## Runtime Info

This module only runs in the editor.

## Developer Info

### IgorBuildCommon.RegisterBuildPlatforms()

Use this function if you are adding a new platform build option.  This registers the platform name so that it can appear in all the appropriate configuration locations.

### IgorBuildCommon.GetLevels()

This gets the list of levels enabled for the build in the Editor Build Settings.

### IgorBuildCommon.SetNewBuildProducts()

This function allows for setting the build products from a given Job Step.  Use this function to pass around the filenames of the latest build products.  This function and the GetBuildProducts function allow each module to work on the previous module's output without knowing about each other.

### IgorBuildCommon.GetBuildProducts()

This function allows a module to retrieve the last module's built products.