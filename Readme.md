Igor - The Unity Automation Tool
=============

Igor gives you a framework for automating any task in Unity, both in-editor and through a continuous integration setup.  It's designed around a module architecture to allow you to customize it to your own needs while also allowing us to share and improve modules with the community.

## How do I use it?

### Installing Igor

1. Install Igor by either:

    * Installing the [Unity Package](https://raw.githubusercontent.com/mikamikem/Igor/master/Igor.unitypackage)
    * Installing the [Igor Updater Script](https://raw.githubusercontent.com/mikamikem/Igor/master/IgorUpdater.cs) to Assets\Editor\Igor\

2. Once the script has been added to your Unity project, launching Unity will pull down the latest version of all the necessary core files.
3. Next open the new Igor Project Configuration window by going to Window -> Igor Project Configuration.
4. Enable any modules for platforms, Unity plugins, SDKs, testing tools, source control systems, or any other module you might want and click Save Configuration to automatically download the latest versions.
5. Optionally set up Jenkins by following the [Jenkins Guide](JenkinsReadme.md).

### Using Igor

Open the Igor Project Configuration window by going to Window -> Igor Project Configuration.  The sections in this window are described in more detail below.

![Igor Configuration Window](https://raw.githubusercontent.com/mikamikem/Igor/master/DocsImages/ConfigurationWindow.png)

- For every job you create, a menu option is added to Window -> Igor that allows you to run that job from in the editor without opening the Igor configuration window.
- You can also run the job that's currently selected by clicking the Run Job button.  That runs it locally, not remotely, so it's good for testing jobs and for doing local builds/testing/etc.

From a high level, these are the important parts of Igor:

- Job - In Igor, a Job is a set of tasks that you want to run in a certain order.  This could be things like building your game, packaging your build, or running automated testing.
- Modules - An Igor Module provides the functionality to actually complete tasks.  So things like [building for desktop](Modules/Build/Desktop) or [Zipping the built files](Modules/Package/Zip).

#### Available Modules

![Available Modules](https://raw.githubusercontent.com/mikamikem/Igor/master/DocsImages/AvailableModules.png)

This section is sorted by category to show you what modules are available on GitHub.

- If a module is not installed, it will show the text "Avail" followed by a version number to signify which version is the latest available.
- If a module is installed, you will see an Installed version and an Available version.
	- If you see a module that is outdated, you can run Check For Updates to get the latest version of each module.

Checking and unchecking modules will only take effect when you click Save Configuration.  Clicking on Save Configuration will automatically attmept to retrieve or remove any modules that were enabled or disabled.  Note that enabling a module in this section does not mean that you need to use the module in all of your jobs, it just means that it is available for use in any of your jobs.  If you disable a module, it will be removed and unavailable for any of the jobs in your project.

#### Parameter Preview

![Parameter Preview](https://raw.githubusercontent.com/mikamikem/Igor/master/DocsImages/JobParameters.png)

The section in the middle that has a Parameters button and a Jenkins Job button shows you what the parameters are that you need to pass to Igor to run the currently selected job.  You should be able to copy the Jenkins Job text directly into a Jenkins Job's Execute Shell step.

- Checking the "Trigger Job By Name" option is highly recommended.  With this checked, the job is triggered by name which means that you can update your job's configuration without needing to update the Jenkins job.

#### Enabled Job Steps

![Enabled Job Steps](https://raw.githubusercontent.com/mikamikem/Igor/master/DocsImages/EnabledJobSteps.png)

This section allows you to preivew what the job will be doing and what order each task will be executed in.

#### Global Options

![Global Options](https://raw.githubusercontent.com/mikamikem/Igor/master/DocsImages/GlobalOptions.png)

This section contains a really important section if you are running on Jenkins builders that are used for multiple projects.  This section allows you to set the minimum and maximum Unity Editor version number that will be used for this job.  This is really important to get right so that you don't end up getting jobs that hang waiting for you to click on the update API button.

#### Module Options

This is another categorized and sorted heirarchy that displays all the options for every module you have installed and that is applicable to the current job.

- Note that for things like the iOS and Desktop build modules, you will only see the one that makes sense for your chosen platform.

#### Parameters And Config Values

When you are configuring a job there are two types of input you can provide to a module.

1. Per-job parameters which can be boolean values or string values.
2. Global configuration parameters which can only be string values.

Per-job parameters are useful for things that will probably always change per job (like the final built executable filenames), while global config values are useful for shared values (like a Facebook App ID).  Once you set a config value, that value is automatically used by any jobs that use that module in the same project, but you can also override the config value on an individual basis (so you could set a generally shared value and override it for 1 or 2 jobs as needed).

For a value that can be set either in the config or as a parameter, you will see something like the following:

![Param or Config Values](https://raw.githubusercontent.com/mikamikem/Igor/master/DocsImages/ParamConfigUI.png)

- The text field on the left is the value you want to use.
- Clicking the right arrow will set the config value for that field to whatever you typed in the box.
- Clicking the left arrow will reset the job specific value to whatever is in the config.
- Clicking on the X will clear the config value.

## What is currently supported?

- Build Platforms: Windows, OSX, iOS
- Continuous Integration: Jenkins
- Packaging: Zip, iOS OTA
- Distribution: FTP, BitTorrent Sync
- 3rd Party SDKs: (iOS)StoreKit, (iOS)Facebook

## What is on the roadmap?

- Planned Build Platforms: Linux, Android, PS4, PSVita, Xbox One
- Planned Distribution: Dropbox
- Planned 3rd Party SDKs: (Android)Facebook

## How do I make it work for my SDK/plugin/platform/etc?

Check out the [Developer Readme](DeveloperReadme.md) for more information on writing your own modules.