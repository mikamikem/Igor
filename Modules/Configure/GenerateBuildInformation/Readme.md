Zip Module
=============

## Summary

This module provides functionality for making information about the parameters of an Igor job available to Unity's runtime.

## Description

This module creates a resource text file that the static IgorBuildInformation class will parse as a key-value dictionary and make available to an application that incorporates Igor.
Igor attempts to pull most of these values from environment variables set on the build machine when the job is executed. VERSION is partially defined by the user (Major and Minor) and by
the number of runs that have executed.

The list of keys that Igor will attempt to populate are as follows.

VERSION: Versioning (defined as Major.Minor.Revision). Partially defined by the user (Major and Minor) and by the number of runs that have executed. Every time the job runs, the Revision is incremented by 1.

GIT_COMMIT : The commit hash Jenkins pulls before executing the job.

GIT_TAG : The tag, if any, associated with GIT_COMMIT.
	-> (Windows only) To facilitate pushing the tag of the head Git commit to Igor via an environment variable, add this workaround to the Jenkins batch command that executes your Igor job:

git tag --points-at %GIT_COMMIT% > tmp_git_tag.txt
SET /p GIT_TAG= < tmp_git_tag.txt
del tmp_git_tag.txt

GIT_TAG_MESSAGE : The tag message, if any, associated with GIT_TAG.
	-> Please install this Jenkins plugin: https://wiki.jenkins-ci.org/display/JENKINS/Git+Tag+Message+Plugin

GIT_BRANCH: The branch for the active Git repo when the job was run.

GIT_AUTHOR: The author of the last Git commit.

JOB_TIME: The time at which the job was triggered.

## Jenkins Setup

(Optional) See GIT_TAG and GIT_TAG_MESSAGE.

## Project Setup

This module does not require any changes to your project.

## Runtime Info

Access to the build information is obtained through the static class IgorBuildInformation -- specifically,

string Get(string inKey)
and
IEnumerable<string, string> GetAll()

It's recommended that you write a small script that calls GetAll and logs all values in the text file to confirm that the module is reading your Jenkins-side version control information correctly.

## Developer Info

### IgorBuildInformation.Get()

This static function attempts to get a value for inKey and returns an empty string is the key is not found.

### IgorBuildInformation.GetAll()

This function returns an IEnumerable collection of KeyValuePairs representing all pairs found in the generated text file.