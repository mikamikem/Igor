Zip Module
=============

## Summary

This module provides functionality for overriding the define symbols set by the versioned PlayerSettings.asset file.

## Description

This module allows you to support multiple jobs with different define symbol permutations. By default Unity stores a semicolon delimited list of defines per
platform, but you may have cases where you need to conditionally compile your codebase in different ways even though the platform is the same (e.g. a Windows x64 build with different 
defines for Steam, Desura and GOG). The string you enter to specify your defines should also be semicolon delimitted.

## Jenkins Setup

This module does not require any special Jenkins setup.

## Project Setup

This module does not require any changes to your project.

## Runtime Info

This module only runs in the editor.

## Developer Info

This module does not expose any useful functionality for development purposes.