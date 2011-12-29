## CoApp

CoApp is currently undergoing a bit of a cleanup on GitHub.

This is the "core" project. There is a solution in here which can be built without the Windows SDK/DDK but 
following the instructions in https://github.com/coapp/coapp.org/wiki/Setting-Up-Your-CoApp-Development-Environment 
is still recommended.

If anything doesn't make sense, documentation looks incomplete - log an issue https://github.com/coapp/coapp.org/issues
(@voltagex is looking after some documentation as of December 2011)

### Checking out this project
This project uses git submodules, which means you have to clone it correctly to get all the right things:

``` batch
git clone --recursive git@github.com:coapp/coapp.git
```

or, if you didn't pay attention, (or have an old version of git that doesn't do recursive right)

``` batch
git clone git@github.com:coapp/coapp.git

cd coapp
git submodule init
git submodule update

```

### Building this project

Once you've cloned the repository you should be able to build it from the command line using `pTk` (included in a submodule):

``` batch
cd coapp

tools\ext\ptk build release 
```

or open one of the `.SLN` files in Visual Studio:

`coapp.sln` -- contains the projects without the tricky-to-build prerequisites (native dlls and bootstrappers)

or 

`coapp-with-prerequisites.sln` -- contains the projects **with** the tricky-to-build prerequisites (native dlls and bootstrappers)

It is not recommended that you use this project, the prerequisite DLLs must be digitally signed to work correctly (which is why the signed copies are shipped in the ext/binaries submodule)


### NOTES about committing
This project uses submodules, so it's important to make sure that you update the submodules in ./ext before you commit code to this project.

You can do this from the command prompt: `for /d %v in (ext\*) do ( pushd %v & git pull & popd )`
