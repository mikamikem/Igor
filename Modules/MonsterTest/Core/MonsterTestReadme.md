Monster Test - Unity Automated Testing Framework
=============

Monster integrates with Igor jobs or runs in standalone form to provide a generic game testing framework.  Depending on the game you are working on and what modules have been contributed, you can use existing modules to drive your game or derive your own from the Monster framework.  The framework uses a node based graph system to design the tests and should require minimal coding work to integrate if the modules available are suitable for your game.  Extending the modules is designed to be very easy and seamless for cross-platform development.  All functionality written in a module is handled uniformly across editor testing, standlaone testing, and device testing.

## How do I use it?

1. Use Igor to install game specific testing modules.
2. Create a testing state machine to determine how the test will control your game and how it responds to changes in your game.
3. Run the test in editor to make sure it works.
4. Optionally chain multiple tests together into testing plans to run all together.
5. Optionally set up Jenkins by following the [Jenkins Guide](../../../JenkinsReadme.md).

## What does Monster stand for?

Monster stands for Maiming Observable Nuisances Systematically Through Excessive Repetition of course!

## What's currently supported?

- Launching the game

## What's planned to be supported?

- In-game Dialogue Tree Testing
- Navigation testing (with or without NavMesh)
- Input mocking
- Input fuzzing
- Runtime stats
- Automatic and manual screenshot capture and archiving
- Log collection and archiving
- Test plans with multiple tests running in a single Jenkins job

## What's on the wish list?

- Rolling video capture and archiving
- Mobile and console platform support