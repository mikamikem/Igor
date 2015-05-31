BitTorrent Sync Module
=============

## Summary

Use this module to distribute your files via BitTorrent Sync.

## Description

This module is designed to work with [BitTorrent Sync](https://www.getsync.com/) to distribute any generated files.  BitTorrent Sync allows you to share a folder with users that can be updated at any time and replicates the data across all clients with the BitTorrent protocol.

## Jenkins Setup

The ideal setup for this module is to create a folder that you will use as the root for all of your BitTorrent Sync shares you want Igor to use on a given Jenkins node.  In the Jenkins Node configuration, add a new environment variable for this directory (something like BTSyncBase) and use that environment variable name in the module configuration for your Igor job in Unity.

## Project Setup

This module does not require any changes to your project.

## Runtime Info

This module only runs in the editor.

## Developer Info

This module does not expose any useful functionality for development purposes.