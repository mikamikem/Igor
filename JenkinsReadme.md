Igor - Jenkins Guide
=============

This guide will explain how to use Igor with Jenkins.

## Build Executor Setup

For each node that runs Jenkins jobs, you will need to configure some values so Igor knows where the Unity installations are.  You may also need to add environment variables for certain modules, but you can see that information in the Readme.md for each module.

### Global Igor Environment Variables

To set up environment variables for a given build machine:

1. Go to your Jenkins dashboard and click on one of the machine names in the Build Executor Setup window on the bottom left.
2. On this screen click Configure.
3. Scroll down to Node Properties and check the Environment variables box.
4. Click Add to add a new environment variable and fill in the Name and Value as described below.
5. Make sure you click Save when you're done!

![Environment Variable Example](https://raw.githubusercontent.com/mikamikem/Igor/master/DocsImages/JenkinsEnvVariableExample.png)

Igor uses environment variables set on the build nodes to figure out where the Unity Editor is installed on each machine.  You need to set at least UNITY_LATEST to give Igor one valid install path.  On a Windows node that would look like:

```
Name: UNITY_LATEST
Value: C:\Program Files\Unity
```

And on a Mac node that would look like:

```
Name: UNITY_LATEST
Value: /Applications/Unity/Unity.app/
```

You can optionally set additional variables for additional versions.  The variable names take the form of UNITY_MAJOR_MINOR_RELEASE_PATCH where MAJOR, MINOR, RELEASE, and PATCH can be swapped out for explicit values or for the word "LATEST".  All of these are examples of valid environment variable names:

```
UNITY_4_LATEST
UNITY_5_LATEST
UNITY_4_6_LATEST
UNITY_4_6_1_4
UNITY_5_0_1_LATEST
```

When Igor runs a job, it attempts to find the most recent build of Unity that satisfies the job's minimum and maximum version numbers.  If you don't have a matching version, Igor will error out and report a job failure to Jenkins.

- Note that the last number is for patch versions.  Most public Unity releases have version numbers of the form 4.6.2f1 where the 1 in this case is NOT a patch number.  Patch versions are labeled with a "p" instead of an "f", so a patch version would look like 4.6.2p4.  If your Unity Editor version has an f, you should use a 0 for the last digit since the version is an official release and patch release's with the same major, minor, and release version are newer then the official release.

## Job Setup

Set up your job to use the appropriate version control system for your project with whatever options you would like to enable for polling or scheduled jobs.

### Igor Specific Setup

Once you have the general Jenkins plugins configured for your project, do the following to get Igor working:

1. Add an Execute Shell build step to your job and then go to your Unity Editor.
2. In the Editor go to Window -> Igor -> Igor Configuration.
3. Select the Job in the Igor Configuration Window that you want to create a job for.
4. It is highly recommended to use the "Trigger Job By Name" option so that you don't have to update your Jenkins job when you need to update your Igor Job configuration.
5. Click on the Jenkins Job button and the text below the button should switch to start with "python Assets/Editor/Igor/IgorRun.py"
6. Copy that text into the Execute Shell field in your new Jenkins job.

Once you've copied that text into the Jenkins job it should be ready to go!

![Execute Shell Example](https://raw.githubusercontent.com/mikamikem/Igor/master/DocsImages/JenkinsExecuteShellExample.png)

### Job Log Parsing Jenkins Plugin

To set up your job to use the [Jenkins Log Parser Plugin](https://wiki.jenkins-ci.org/display/JENKINS/Log+Parser+Plugin), do the following:

1. Install the [Jenkins Log Parser Plugin](https://wiki.jenkins-ci.org/display/JENKINS/Log+Parser+Plugin).
2. Create a rule file on your Jenkins dashboard server and populate it with the following text:

	```
	error /^Igor Error:/
	warning /^Igor Warning:/
	info /^Igor Log:/
	start /^Igor Module Start/
	```

3. In Jenkins' Configure System screen, add a Console Output Parsing rules file and point it to the rule file path you just created.
4. In each Igor Jenkins Job, configure the Job and add a post build action for Console output parsing.