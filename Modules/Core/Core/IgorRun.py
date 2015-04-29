#!/usr/bin/env python

import sys;
import os;
import argparse;
import shutil;
import stat;
import subprocess;
import xml.etree.ElementTree as ET


from contextlib import closing
from sys import platform as _platform

try:
	from urllib.request import urlopen
except ImportError:
	from urllib import urlopen

AlwaysUpdate = False
Local = False
RunningDirectory = os.path.join('Assets', 'Editor', 'Igor')
CoreDirectory = os.path.join(RunningDirectory, 'Modules', 'Core', 'Core')
TempLocalXMLFile = os.path.join('Temp', 'Core.xml')
TempLocalPythonFile = os.path.join('Temp', 'IgorRun.py')
RemoteXMLFile = "https://raw.githubusercontent.com/mikamikem/Igor/master/Modules/Core/Core/Core.xml"
RemotePythonFile = "https://raw.githubusercontent.com/mikamikem/Igor/master/Modules/Core/Core/IgorRun.py"
LocalXMLFile = "TestDeploy/Modules/Core/Core/Core.xml"
LocalPythonFile = "TestDeploy/Modules/Core/Core/IgorRun.py"
JobConfigFilename = "IgorJob.xml"

UnityAutomatorFilename = "IgorRun.py"

def GetUnityPath():
	if _platform == "linux" or _platform == "linux2":
		return ""
	elif _platform == "darwin":
		return "/Applications/Unity/Unity.app/Contents/MacOS/Unity"
	elif _platform == "win32":
		if os.path.exists("C:\\Program Files\\Unity\\Editor\\Unity.exe"):
			return "\"C:\\Program Files\\Unity\\Editor\\Unity.exe\""
		return "\"C:\\Program Files (x86)\\Unity\\Editor\\Unity.exe\""
	
	return ""
	
def num(s):
	try:
		return int(s)
	except ValueError:
		return float(s)

def SetFileExecutable(Filename):
	Stats = os.stat(Filename)
	os.chmod(Filename, Stats.st_mode | stat.S_IEXEC)

	return

def SetFileWritable(Filename):
	Stats = os.stat(Filename)
	os.chmod(Filename, Stats.st_mode | stat.S_IWRITE)

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
		out_file.write('</JobCommandLineParams>\n  </Persistent>\n</IgorJobConfig>'.encode('utf-8'))

	return

def RunUnity(Function):
	BuildCommand = GetUnityPath() + " -projectPath \"" + os.getcwd() + "\" -buildmachine -executeMethod " + Function + " -logfile Igor.log"

	print("Starting job with: " + BuildCommand)

	BuildRC = subprocess.call(BuildCommand, shell=True)

	with open("Igor.log", 'r') as fin:
		print(fin.read())

	if BuildRC != 0:
		print("Return code from Unity was not 0.  Something went wrong so check the logs.")

		sys.exit(BuildRC)

	return

#BootstrapIfRequested()

parser = argparse.ArgumentParser(description='Igor - The Unity automator.', add_help=False)
parser.add_argument('--noselfupdate', action='store_true')
parser.add_argument('--nounityupdate', action='store_true')
parser.add_argument('--finalbootstrap')
parser.add_argument('--bootstrap')

testargs, passthrough = parser.parse_known_args()

#if testargs.noselfupdate == False and testargs.finalbootstrap == None and testargs.bootstrap == None:
#	SelfUpdate()

passthroughstring = ' '.join(passthrough)

if passthroughstring.find('-'):
	passthroughstring = passthroughstring[passthroughstring.find('-'):]

print("\n\n-= Igor - The Unity Automator =-\n\n")

print("Calling Unity with pass through parameters " + passthroughstring)

CreateJobConfigFile(passthroughstring)

FunctionName = "Igor.IgorCore.UpdateAndRunJob"

if testargs.nounityupdate:
	FunctionName = "Igor.IgorCore.CommandLineRunJob"

RunUnity(FunctionName)

print("\n\n-= Igor - The Unity Automator finished!! =-\n\n")

sys.exit(0)
