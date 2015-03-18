Igor - The Unity Automation Tool
=============

Igor gives you a framework for automating any task in Unity, both in-editor and through a continuous integration setup.  It's designed around a module architecture to allow you to customize it to your own needs while also allowing us to share and improve modules with the community.

== How do I use it?

1. Install Igor by either:

    * Installing the [Unity Package](Igor.unitypackage)
    * Installing the [Igor Updater Script](IgorUpdater.cs) to Assets\Editor\Igor\

2. Once the script has been added to your Unity project, launching Unity will pull down the latest version of all the necessary core files.
3. Next open the new Igor Project Configuration window by going to Window -> Igor Project Configuration.
4. Enable any modules for platforms, Unity plugins, SDKs, testing tools, source control systems, or any other module you might want and click Save Configuration to automatically download the latest versions.
5. Create Jobs to run whatever tasks you would like Igor to automate and set up menu options for use in the editor and generate sample scripts to drop in to Jenkins.
