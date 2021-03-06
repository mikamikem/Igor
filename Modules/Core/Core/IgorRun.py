#!/usr/bin/env python

import sys;
import os;
import argparse;
import shutil;
import stat;
import subprocess;
import xml.etree.ElementTree as ET
import time;


from contextlib import closing
from sys import platform as _platform

if _platform != "win32":
	import signal;

try:
	from urllib.request import urlopen
except ImportError:
	from urllib import urlopen

AlwaysUpdate = False
Local = False
RunningDirectory = os.path.join('Assets', 'Igor')
CoreDirectory = os.path.join(RunningDirectory, 'Modules', 'Core', 'Core')
TempLocalXMLFile = os.path.join('Temp', 'Core.xml')
TempLocalPythonFile = os.path.join('Temp', 'IgorRun.py')
RemoteXMLFile = "https://raw.githubusercontent.com/mikamikem/Igor/master/Modules/Core/Core/Core.xml"
RemotePythonFile = "https://raw.githubusercontent.com/mikamikem/Igor/master/Modules/Core/Core/IgorRun.py"
LocalXMLFile = "TestDeploy/Modules/Core/Core/Core.xml"
LocalPythonFile = "TestDeploy/Modules/Core/Core/IgorRun.py"
IgorConfigFilename = "Assets/Igor/IgorConfig.xml"
JobConfigFilename = "IgorJob.xml"
MinVersionString = ""
MaxVersionString = ""
BatchModeString = ""
NoGraphicsString = ""
UnityDirectory = ""
MaxTimeBetweenLogs = 60 # Set the default timeout to be an hour in case we're running something like a lightbake that takes forever and doesn't log anything during the process

UnityAutomatorFilename = "IgorRun.py"

def num(s):
	try:
		return int(s)
	except ValueError:
		return float(s)

def GetPlatformStringFromPath(UnityEnvName):
	EnvNameSplit = UnityEnvName.split('_')

	if len(EnvNameSplit) > 0:
		return EnvNameSplit[0]

	return "UNITY"

def GetVersionNumbersForPath(UnityEnvName):
	VersionNums = [ 100, 100, 100, 100 ]
	EnvNameSplit = UnityEnvName.split('_')

	if len(EnvNameSplit) > 1:
		if EnvNameSplit[1] != "LATEST":
			try:
				VersionNums[0] = num(EnvNameSplit[1])
			except ValueError:
				VersionNums[0] = 100

		if len(EnvNameSplit) > 2:
			if EnvNameSplit[2] != "LATEST":
				try:
					VersionNums[1] = num(EnvNameSplit[2])
				except ValueError:
					VersionNums[1] = 100

			if len(EnvNameSplit) > 3:
				if EnvNameSplit[3] != "LATEST":
					try:
						VersionNums[2] = num(EnvNameSplit[3])
					except ValueError:
						VersionNums[2] = 100

				if len(EnvNameSplit) > 4:
					if EnvNameSplit[4] != "LATEST":
						try:
							VersionNums[3] = num(EnvNameSplit[4])
						except ValueError:
							VersionNums[3] = 100

	return VersionNums

def GetBestMatchingUnityPath(PlatformString, MinMajorVersion, MinMinorVersion, MinReleaseVersion, MinPatchVersion, MaxMajorVersion, MaxMinorVersion, MaxReleaseVersion, MaxPatchVersion, bDebug):
	if bDebug == True:
		print("Checking for best matching path with requested version minimum " + str(MinMajorVersion) + "." + str(MinMinorVersion) + "." + str(MinReleaseVersion) + "." + str(MinPatchVersion) + " and maximum " + str(MaxMajorVersion) + "." + str(MaxMinorVersion) + "." + str(MaxReleaseVersion) + "." + str(MaxPatchVersion))

	BestMatchNums = [ -1, -1, -1, -1 ]
	BestMatch = ""
	LatestLatest = ""
	UnityEnvNames = []
	for key in os.environ.keys():
		if key.startswith(PlatformString + "_"):
			UnityEnvNames.append(key)

			if bDebug == True:
				print("Possible Unity path env variable " + key + " with path " + os.environ[key])

	for CurrentPath in UnityEnvNames:
		CurrentVersionNums = GetVersionNumbersForPath(CurrentPath)
		if bDebug == True:
			print("Current version check is " + CurrentPath + " and " + str(CurrentVersionNums) + " with the current best being " + BestMatch)

		if CurrentVersionNums[0] == 100:
			LatestLatest = CurrentPath
		else:
			if MinMajorVersion == CurrentVersionNums[0] and BestMatchNums[1] <= CurrentVersionNums[1] and BestMatchNums[2] <= CurrentVersionNums[2] and BestMatchNums[3] <= CurrentVersionNums[3]:
				if MinMinorVersion <= CurrentVersionNums[1] or MinMinorVersion == 100:
					if MinReleaseVersion <= CurrentVersionNums[2] or MinReleaseVersion == 100:
						if MinPatchVersion <= CurrentVersionNums[3] or MinPatchVersion == 100:
							if MaxMinorVersion >= CurrentVersionNums[1] or MaxMinorVersion == 100:
								if MaxReleaseVersion >= CurrentVersionNums[2] or MaxReleaseVersion == 100:
									if MaxPatchVersion >= CurrentVersionNums[3] or MaxPatchVersion == 100:
										BestMatchNums[0] = CurrentVersionNums[0]
										BestMatchNums[1] = CurrentVersionNums[1]
										BestMatchNums[2] = CurrentVersionNums[2]
										BestMatchNums[3] = CurrentVersionNums[3]
										BestMatch = CurrentPath

	if bDebug == True:
		print("Best match after loop is " + BestMatch + " with LatestLatest " + LatestLatest)

	if BestMatch == "":
		BestMatch = LatestLatest

	if BestMatch != "":
		if bDebug == True:
			print("Returning BestMatch " + BestMatch + " with path " + os.environ[BestMatch])
		return os.environ[BestMatch]

	if bDebug == True:
		print("No env variables found at all.  Defaulting to hard coded paths.")

	return ""

def GetUnityPath():
	global UnityDirectory, MinVersionString, MaxVersionString

	if UnityDirectory == "" and MinVersionString != "":
		MinVersions = GetVersionNumbersForPath(MinVersionString)

		if MaxVersionString == None:
			MaxVersionString = ""

		MaxVersions = GetVersionNumbersForPath(MaxVersionString)
		UnityDirectory = GetBestMatchingUnityPath(GetPlatformStringFromPath(MinVersionString), MinVersions[0], MinVersions[1], MinVersions[2], MinVersions[3], MaxVersions[0], MaxVersions[1], MaxVersions[2], MaxVersions[3], False)
	if(str(UnityDirectory) == ""):
		if _platform == "linux" or _platform == "linux2":
			return ""
		elif _platform == "darwin":
			return "/Applications/Unity/Unity.app/Contents/MacOS/Unity"
		elif _platform == "win32":
			if os.path.exists("C:\\Program Files\\Unity\\Editor\\Unity.exe"):
				return "\"C:\\Program Files\\Unity\\Editor\\Unity.exe\""
			return "\"C:\\Program Files (x86)\\Unity\\Editor\\Unity.exe\""
	else:
		if _platform == "linux" or _platform == "linux2":
			return ""
		elif _platform == "darwin":
			return "\"" + str(UnityDirectory) + "/Contents/MacOS/Unity" + "\"";
		elif _platform == "win32":
			return "\"" + str(UnityDirectory) + "\\Editor\\Unity.exe\"";
	
	return ""

def KillProcess(pid):
	if _platform == "linux" or _platform == "linux2":
		print("Igor Error: Attempted to kill process " + pid + ", but we're on linux and we haven't implemented process killing yet.")
	elif _platform == "darwin":
		os.killpg(pid, signal.SIGTERM)
	elif _platform == "win32":
		subprocess.Popen("TASKKILL /F /PID {pid} /T".format(pid=pid))

	return
	
def SetFileWritable(Filename):
	Stats = os.stat(Filename)
	os.chmod(Filename, Stats.st_mode | stat.S_IWRITE)

	return

def SetFileExecutable(Filename):
	Stats = os.stat(Filename)
	os.chmod(Filename, Stats.st_mode | stat.S_IEXEC)

	return

def DownloadFileToLocation(FileURL, LocalURL):
	global Local

	if os.path.dirname(LocalURL) != '':
		if not os.path.exists(os.path.dirname(LocalURL)):
			print("Making dirs \"" + os.path.dirname(LocalURL) + "\"")
			os.makedirs(os.path.dirname(LocalURL))

	if Local == False:
		# Download the file from `url` and save it locally under `file_name`:
		with closing(urlopen(FileURL)) as response, open(LocalURL, 'wb') as out_file:
			data = response.read() # a `bytes` object
			out_file.write(data)
	else:
		shutil.copy(FileURL, LocalURL)

	return

def BootstrapIfRequested():
	global UnityAutomatorFilename, RunningDirectory

	bootstrapparser = argparse.ArgumentParser(add_help=False)
	bootstrapparser.add_argument('--bootstrap')
	bootstrapparser.add_argument('--finalbootstrap')

	bootstrapargs, unknown = bootstrapparser.parse_known_args()

	if bootstrapargs.finalbootstrap != None and bootstrapargs.finalbootstrap != '':
		TempFile = bootstrapargs.finalbootstrap

		print("Bootstrap (4/4) - Removing temp bootstrap file " + TempFile)

		os.remove(TempFile)

	elif bootstrapargs.bootstrap != None and bootstrapargs.bootstrap != '':
		OriginalFile = bootstrapargs.bootstrap

		BaseName = UnityAutomatorFilename

		print("Bootstrap (2/4) - Removing the original file " + OriginalFile)

		SetFileWritable(OriginalFile)
		os.remove(OriginalFile)
		
		print("Bootstrap (3/4) - Copying the new file to " + BaseName)
	
		PathToNewScript = os.path.join(RunningDirectory, BaseName)
		shutil.copy(__file__, os.path.join(os.getcwd(), PathToNewScript))

		os.execvp('python', ["\"" + os.getcwd() + "\"", PathToNewScript, '--finalbootstrap="' + sys.argv[0] + '"'] + sys.argv)

	return

def SelfUpdate():
	global Local, RemoteXMLFile, RemotePythonFile, LocalXMLFile, LocalPythonFile, TempLocalXMLFile, AlwaysUpdate, CoreDirectory, TempLocalPythonFile

	print("Igor is attempting self-update...\n")

	if Local == False:
		DownloadFileToLocation(RemoteXMLFile, TempLocalXMLFile)
	else:
		DownloadFileToLocation(LocalXMLFile, TempLocalXMLFile)

	InstalledPath = os.path.join(CoreDirectory, "Core.xml")

	if os.path.exists(TempLocalXMLFile) and os.path.exists(InstalledPath):
		InstalledTree = ET.parse(InstalledPath)
		InstalledRoot = InstalledTree.getroot()
		InstalledVersion = num(InstalledRoot.find('ModuleVersion').text)

		NewTree = ET.parse(TempLocalXMLFile)
		NewRoot = NewTree.getroot()
		NewVersion = num(NewRoot.find('ModuleVersion').text)

		if NewVersion > InstalledVersion or AlwaysUpdate == True:
			print("Newer version available! (We have " + str(InstalledVersion) + " and latest is " + str(NewVersion) + ")\n")

			print("Attempting bootstrap...")
			print("Bootstrap (1/4) - Launching the new builder script for bootstrapping.")

			if Local == False:
				DownloadFileToLocation(RemotePythonFile, TempLocalPythonFile)
			else:
				DownloadFileToLocation(LocalPythonFile, TempLocalPythonFile)

			if os.path.exists(TempLocalPythonFile):
				SetFileExecutable(TempLocalPythonFile)

				os.execvp('python', ["\"" + os.getcwd() + "\"", TempLocalPythonFile, '--bootstrap="' + sys.argv[0] + '"'] + sys.argv)
		else:
			print("We have the latest version!\n")

	return

def CreateJobConfigFile(PassThroughParams):
	global JobConfigFilename

	print("Creating config file for the running job.")

	with open(JobConfigFilename, 'wb') as out_file:
		out_file.write('<?xml version="1.0" encoding="us-ascii"?>\n<IgorJobConfig xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">\n  <Persistent>\n    <JobCommandLineParams>'.encode('utf-8'))
		out_file.write(PassThroughParams.encode('utf-8'))
		if not "ExecuteJob" in PassThroughParams:
			out_file.write(bytes(" --ExecuteJob=\"" + os.environ["JobName"] + "\"", 'UTF-8'))
		out_file.write('</JobCommandLineParams>\n  </Persistent>\n</IgorJobConfig>'.encode('utf-8'))

	return

def CheckForGlobalConfigKey(Line, Key, FileHandle, CurrentValue):
	if "<Key>" + Key + "</Key>" in Line:
		nextLine = FileHandle.readline()

		if nextLine != None and "<Value>" in nextLine:
			GlobalConfigValue = nextLine.strip().replace("<Value>", "").replace("</Value>", "")

			print("Found Global Config Key " + Key + " with value " + GlobalConfigValue)

			return GlobalConfigValue

	return CurrentValue

def CheckForConfigParam(Line, Flag, CurrentValue):
	if "--" + Flag + "=" in Line:
		startPoint = Line.find("--" + Flag + "=") + len("--" + Flag + "=") + 1
		endPoint = Line.find("\"", startPoint)

		Value = Line[startPoint:endPoint]

		print("Found flag --" + Flag + " in named job with value " + Value)

		return Value

	return CurrentValue

def ReadPythonValuesFromJobConfig(PassThroughParams):
	global MinVersionString, MaxVersionString, MaxTimeBetweenLogs, IgorConfigFilename

	NamedJob = ""

	for CurrentParam in PassThroughParams:
		if CurrentParam.startswith("--ExecuteJob="):
			NamedJob = CurrentParam.replace("--ExecuteJob=", "").replace("\"", "")

	if NamedJob == "":
		NamedJob = os.environ["JobName"]

	if NamedJob != "":
		configHandle = None
		nextline = None

		if configHandle == None and os.path.exists(IgorConfigFilename):
			with open(IgorConfigFilename, 'r') as configHandle:
				if configHandle != None:
					configLine = configHandle.readline()
					nextline = configLine
					while configLine and nextline:
						MinVersionString = CheckForGlobalConfigKey(configLine, "Core.Core.minunityversion", configHandle, MinVersionString)
						MaxVersionString = CheckForGlobalConfigKey(configLine, "Core.Core.maxunityversion", configHandle, MaxVersionString)

						if "<JobCommandLineParams>" in configLine:
							nextline = configHandle.readline()

							if nextline != None and ("<JobName>" + NamedJob + "</JobName>") in nextline:
								print("Found named job " + NamedJob + " in " + IgorConfigFilename + ".  Parsing for python config values.")

								MinVersionString = CheckForConfigParam(configLine, "minunityversion", MinVersionString)
								MaxVersionString = CheckForConfigParam(configLine, "maxunityversion", MaxVersionString)
								MaxTimeBetweenLogs = float(CheckForConfigParam(configLine, "maxminutesbeforebuildishung", str(MaxTimeBetweenLogs)))

						configLine = configHandle.readline()

	return

def RunUnity(Function):
	global BatchModeString, NoGraphicsString, MaxTimeBetweenLogs
	AdditionalUnityArgs = BatchModeString + NoGraphicsString

	BuildCommand = GetUnityPath() + " -projectPath \"" + os.getcwd() + "\" -buildmachine -executeMethod " + Function + " -logfile Igor.log" + AdditionalUnityArgs

	print("Starting job with: " + BuildCommand)

	logHandle = None

	if os.path.exists("Igor.log"):
		os.remove("Igor.log")

	if _platform == "win32":
		BuildProc = subprocess.Popen(BuildCommand, shell=True)
	else:
		BuildProc = subprocess.Popen(BuildCommand, shell=True, preexec_fn=os.setpgrp)

	hasRefreshedAssetDB = False
	nextline = None

	lastLogTime = time.time()

	while BuildProc.poll() is None:
		if logHandle == None and os.path.exists("Igor.log"):
			logHandle = open("Igor.log", 'r')

		if logHandle != None:
			where = logHandle.tell()
			line = logHandle.readline()
			if not line:
				time.sleep(10)
				logHandle.seek(where)
			else:
				lastLogTime = time.time()
				if "Assembly-CSharp-Editor-firstpass.dll into Unity Child Domain" in line:
					hasRefreshedAssetDB = True

				if hasRefreshedAssetDB:
					if "Compilation failed: " in line:
						print("Igor Error: Killing Unity because script compilation failed and anything beyond this point is undefined behavior.")

						KillProcess(BuildProc.pid)

				if "UnityUpgradable" in line:
					print("Igor Error: Killing Unity because the editor version is newer then your project version and your project requires an API upgrade.")

					KillProcess(BuildProc.pid)
				elif "Crash!!!" in line:
					nextline = logHandle.readline()

					if "SymInit" in nextline:
						print("Igor Error: Killing Unity because the editor crashed!")

						KillProcess(BuildProc.pid)
				elif "Error while reading window layout" in line:
					print ("Igor Error: Killing Unity because scripts compilation failed and we possibly haven't successfully built the Igor code so nothing will happen.")

					KillProcess(BuildProc.pid)
				elif "compilationIOException" in line:
					print("Igor Error: Killing Unity because there was a problem attempting to compile scripts.  Possibly the disk is full?")

					KillProcess(BuildProc.pid)

				sys.stdout.write(line)
				
				if nextline:
					sys.stdout.write(nextline)
					nextline = None
			
		if (time.time() - lastLogTime) > (MaxTimeBetweenLogs * 60.0):
			print("Igor Error: The build took too long so we might be stuck on a window prompt or waiting for Unity to crash.")
			KillProcess(BuildProc.pid)

	if logHandle != None:
		Rest = logHandle.read()

		if "Compilation failed: " in Rest:
			print("Igor Error: Killing Unity because script compilation failed and anything beyond this point is undefined behavior.")
		elif "UnityUpgradable" in Rest:
			print("Igor Error: Killing Unity because the editor version is newer then your project version and your project requires an API upgrade.")

		sys.stdout.write(Rest)

		logHandle.close()

	BuildRC = BuildProc.returncode

	if BuildRC != 0:
		print("Igor Error: Return code from Unity was " + str(BuildRC) + " which is non-zero.  Something went wrong so check the logs.")

		sys.exit(1)

	return

#BootstrapIfRequested()

parser = argparse.ArgumentParser(description='Igor - The Unity automator.', add_help=False)
parser.add_argument('--noselfupdate', action='store_true')
parser.add_argument('--nounityupdate', action='store_true')
parser.add_argument('--finalbootstrap')
parser.add_argument('--bootstrap')
parser.add_argument('--minunityversion')
parser.add_argument('--maxunityversion')
parser.add_argument('--batchmode', action='store_true')
parser.add_argument('--nographics', action='store_true')
parser.add_argument('--maxminutesbeforebuildishung')

testargs, passthrough = parser.parse_known_args()

ReadPythonValuesFromJobConfig(passthrough)

passthroughstring = ' '.join(passthrough)

if passthroughstring.find('-'):
	passthroughstring = passthroughstring[passthroughstring.find('-'):]

if testargs.minunityversion != None and testargs.minunityversion != "":
	MinVersionString = testargs.minunityversion

if testargs.maxunityversion != None and testargs.maxunityversion != "":
	MaxVersionString = testargs.maxunityversion

if testargs.batchmode:
	BatchModeString = " -batchmode"

if testargs.nographics:
	NoGraphicsString = " -nographics"

if testargs.maxminutesbeforebuildishung != None:
	MaxTimeBetweenLogs = testargs.maxminutesbeforebuildishung

print("After parsing " + IgorConfigFilename + " and command line params, python config values are:\nMinVersionString: " + MinVersionString + "\nMaxVersionString: " + MaxVersionString + "\nMaxTimeBetweenLogs: " + str(MaxTimeBetweenLogs))

#if testargs.noselfupdate == False and testargs.finalbootstrap == None and testargs.bootstrap == None:
#	SelfUpdate()

print("\n\n-= Igor - The Unity Automator =-\n\n")

print("Calling Unity with pass through parameters " + passthroughstring)

CreateJobConfigFile(passthroughstring)

#FunctionName = "Igor.IgorEditorCore.UpdateAndRunJob"
# Updating on the builder is very rarely a good idea.  We're going to disable it by default now.
FunctionName = "Igor.IgorEditorCore.CommandLineRunJob"

#if testargs.nounityupdate:
#	FunctionName = "Igor.IgorEditorCore.CommandLineRunJob"

RunUnity(FunctionName)

print("\n\n-= Igor - The Unity Automator finished!! =-\n\n")

sys.exit(0)
