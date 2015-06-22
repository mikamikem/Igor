Zip Module
=============

## Summary

This module provides functionality for overriding the versioned copy of any number of ProjectSettings files.

## Description

This module allows you to specify overrides for the versioned copy of any number of ProjectSettings files. The module works by making copies of the ProjectSettings file(s)
you have selected from the dropdown. When you click the save button, the module saves out a copy of whatever exists in the on-disk copy of each targeted ProjectSettings. At job-time,
the module replaces the versioned copy of the targeted ProjectSettings files with the saved-off copies, and ensures that these settings are recompiled.

NOTE: When you hit the save button, the module's saving operation will only save what's on disk -- meaning that if you haven't done a "Save Project" since you started making changes to anything
in the editor which gets serialized as part of a ProjectSettings .asset file, those changes reside ONLY in memory and will not be reflected in the version the module saves.

NOTE: The mapping of a set of copied ProjectSettings files to a job is done by way of the job's name, which means that if you change the job name you should also update the folder
containing the saved ProjectSettings files.

## Jenkins Setup

This module does not require any special Jenkins setup.

## Project Setup

This module does not require any changes to your project.

## Runtime Info

This module only runs in the editor.

## Developer Info