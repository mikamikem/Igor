Google Play Game Services For Android Module
=============

## Summary

This module adds support to build and link the Google Play Game Services library to your Android project.

## Description

This implementation does not provide any functionality for calling the Google Play Game Services library calls from within Unity, but it does set up your build environment appropriately so that if you've already written the native Java callbacks, it will build appropriately.

## Jenkins Setup

This module does not require any special Jenkins setup.

## Project Setup

In the Android SDK downloader utility make sure to download the Google Play services library under the Extras folder.  Open up your Android SDK folder/extras/google/google_play_services/libproject and copy the google-play-services_lib folder to your project's Assets/Plugins/Android folder.

## Runtime Info

This module only runs in the editor.

## Developer Info

This module does not expose any useful functionality for development purposes.