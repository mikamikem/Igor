Monster Core Module
=============

## Summary

This module provides a basic interface to control starting a new game, loading a save, and quitting the game via Monster Test.

## Description

This module requires you to implement the [IMonsterMenuRunner interface](Runtime/IMonsterMenuRunner.cs) (see the developer info below for how).

## Jenkins Setup

This module does not require any special Jenkins setup.

## Project Setup

This module requires you to implement the [IMonsterMenuRunner interface](Runtime/IMonsterMenuRunner.cs) (see the developer info below for how).

## Runtime Info

This module is fully available at runtime.

## Developer Info

This module requires you to implement the [IMonsterMenuRunner interface](Runtime/IMonsterMenuRunner.cs).  The implentation should check for the values of bShouldStartNewGame, bShouldLoadGame, and bShouldQuit and handle the appropriate game logic to trigger that functionality.  When the game gets to the menu it should report back by setting bNavigatingMenu to true and by setting Igor.MonsterMenuNav.MenuRunnerInst to the instance of the implementation that corresponds to the currently active menu.  Depending on the success of the test, the implentation should also set bStartedSuccessfully, bFailedToStart, or bQuit as appropriate.