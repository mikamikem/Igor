#!/usr/bin/env python

import sys;
import os;
import argparse;
import shutil;
import stat;
import subprocess;
import time;
import zipfile;
from os.path import expanduser


from contextlib import closing
from sys import platform as _platform

if _platform != "win32":
	import signal;

LaunchersDir = os.path.join('Assets', 'Igor', 'Monster', 'Launchers')

def num(s):
	try:
		return int(s)
	except ValueError:
		return float(s)

def KillProcess(pid):
	if _platform == "linux" or _platform == "linux2":
		print("Igor Error: Attempted to kill process " + pid + ", but we're on linux and we haven't implemented process killing yet.")
	elif _platform == "darwin":
		os.killpg(pid, signal.SIGTERM)
	elif _platform == "win32":
		subprocess.Popen("TASKKILL /F /PID {pid} /T".format(pid=pid))

	return

def GetLauncherZip():
	if _platform == "linux" or _platform == "linux2":
		print("Igor Error: Attempted to launch tests, but we're on linux and we haven't implemented launching processes yet.")
	elif _platform == "darwin":
		return os.path.join(LaunchersDir, 'MonsterLauncherOSX.zip')
	elif _platform == "win32":
		return os.path.join(LaunchersDir, 'MonsterLauncherWindows.zip')

	return ""
	
def GetLauncherExecutable():
	if _platform == "linux" or _platform == "linux2":
		print("Igor Error: Attempted to launch tests, but we're on linux and we haven't implemented launching processes yet.")
	elif _platform == "darwin":
		return os.path.join(LaunchersDir, 'MonsterLauncher.app', 'Contents', 'MacOS', 'MonsterLauncher')
	elif _platform == "win32":
		return os.path.join(LaunchersDir, 'MonsterLauncher.exe')

	return ""

def GetPlayerLogFile():
	if _platform == "linux" or _platform == "linux2":
		print("Igor Error: Attempted to launch tests, but we're on linux and we haven't implemented launching processes yet.")
	elif _platform == "darwin":
		return os.path.join(expanduser("~"), 'Library', 'Logs', 'Unity', 'Player.log')
	elif _platform == "win32":
		return "Player.log"

	return ""

def unzip(source_filename, dest_dir):
    with zipfile.ZipFile(source_filename) as zf:
        for member in zf.infolist():
            words = member.filename.split('/')
            path = dest_dir
            zf.extract(member, path)

def SetFileWritable(Filename):
	Stats = os.stat(Filename)
	os.chmod(Filename, Stats.st_mode | stat.S_IWRITE)

	return

def SetFileExecutable(Filename):
	Stats = os.stat(Filename)
	os.chmod(Filename, Stats.st_mode | stat.S_IEXEC)

	return

def RunLauncher(envvars):
	LauncherZip = GetLauncherZip()

	unzip(LauncherZip, LaunchersDir)

	RunCommand = GetLauncherExecutable()

	SetFileExecutable(RunCommand)

	RunCommand += " -batchmode"

	print("Starting launcher with: " + RunCommand)

	logHandle = None

	if os.path.exists("Monser.log"):
		os.remove("Monser.log")

	if os.path.exists(GetPlayerLogFile()):
		os.remove(GetPlayerLogFile())

	if _platform == "win32":
		LauncherProc = subprocess.Popen(RunCommand, shell=True, env=envvars)
	else:
		LauncherProc = subprocess.Popen(RunCommand, shell=True, env=envvars, preexec_fn=os.setpgrp)

	print("Log file location " + GetPlayerLogFile())

	while LauncherProc.poll() is None:
		if logHandle == None:
			if os.path.exists("Monster.log"):
				logHandle = open("Monster.log", 'r')
			elif os.path.exists(GetPlayerLogFile()):
				logHandle = open(GetPlayerLogFile(), 'r')

		if logHandle != None:
			where = logHandle.tell()
			line = logHandle.readline()
			if not line:
				time.sleep(10)
				logHandle.seek(where)
			else:
#				if "Compilation failed: " in line:
#					print("Monster Error: Killing Launcher because script compilation failed and anything beyond this point is undefined behavior.")

#					KillProcess(BuildProc.pid)
#				elif "UnityUpgradable" in line:
#					print("Igor Error: Killing Unity because the editor version is newer then your project version and your project requires an API upgrade.")

#					KillProcess(BuildProc.pid)
				sys.stdout.write(line)

	if logHandle != None:
		Rest = logHandle.read()

#	if "Compilation failed: " in Rest:
#		print("Igor Error: Killing Unity because script compilation failed and anything beyond this point is undefined behavior.")
#	elif "UnityUpgradable" in Rest:
#		print("Igor Error: Killing Unity because the editor version is newer then your project version and your project requires an API upgrade.")

		sys.stdout.write(Rest)

		logHandle.close()

	RunRC = LauncherProc.returncode

	if RunRC != 0:
		print("Monster Error: Return code from Launcher was " + str(RunRC) + " which is non-zero.  Something went wrong so check the logs.")

		sys.exit(1)

	return

parser = argparse.ArgumentParser(description='Monster Test - Unity Automated Testing Framework', add_help=False)
parser.add_argument('--ExecuteJob')

testargs, passthrough = parser.parse_known_args()

passthroughstring = ' '.join(passthrough)

if passthroughstring.find('-'):
	passthroughstring = passthroughstring[passthroughstring.find('-'):]

print("\n\n-= Monster - The Unity Test Framework =-\n\n")

envvars = dict(os.environ)

if testargs.ExecuteJob != None and testargs.ExecuteJob != "":
	envvars["MonsterJobName"] = testargs.ExecuteJob

RunLauncher(envvars)

print("\n\n-= Monster - The Unity Test Framework finished!! =-\n\n")

sys.exit(0)
