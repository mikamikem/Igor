Jenkins FTP Module
=============

## Summary

Use this module to distribute your files via FTP as a post build action in Jenkins.

## Description

This module copies an output file to a specific pre-determined filename so that the Jenkins post-build action will find it and upload it.  This module works in that the FTP upload post-build action does not cause the build to fail if the file doesn't exist, so it allows us to optionally upload the file by just copying it to the expected location.

## Jenkins Setup

This module requires the (Jenkins FTP Upload Plugin)[https://wiki.jenkins-ci.org/display/JENKINS/FTP-Publisher+Plugin] to be installed and configured appropriately for your FTP.  Once you have it set up, add the post-build step to your Jenkins job and provide a filename that is not currently being used in your Igor build job.  In the Jenkins FTP Igor Module config, enter this new filename into the FTP Filename field.

## Project Setup

This module does not require any changes to your project.

## Runtime Info

This module only runs in the editor.

## Developer Info

This module does not expose any useful functionality for development purposes.