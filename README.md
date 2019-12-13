<p align="center">
<img
    src="https://repository-images.githubusercontent.com/204246907/ad79dd00-c71b-11e9-9a51-f03849ba308f"
    width="550px">
</p>

<p align="center">
 <!-- <a href="https://travis-ci.org/george-key/geo">
    <img src="https://travis-ci.org/george-keygeo.svg?branch=master" alt="Current TravisCI build status.">
  </a> -->
  <a href="https://github.com/neo-project/geo/releases">
    <img src="https://badge.fury.io/gh/george-key%2Fgeo.svg" alt="Current neo version.">
  </a>
  <a href="https://codecov.io/github/george-key/geo/branch/master/graph/badge.svg">
    <img src="https://codecov.io/github/george-key/geo/branch/master/graph/badge.svg" alt="Current Coverage Status." />
  </a>
<!--  <a href="https://github.com/george-key/geo">
    <img src="https://tokei.rs/b1/github/george-key/geo?category=lines" alt="Current total lines.">
  </a>	-->
  <a href="https://github.com/george-key/geo/blob/master/LICENSE">
    <img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="License.">
  </a>
</p>

# GEO 3.1.1.0 (just forked): A distributed Smart Ecosystem Software
================

Being forked from NEO, this will help to save the planet Earth


## Table of Contents
1. [Overview](#overview)
2. [Project structure](#project-structure)
3. [Related projects](#related-projects)
4. [Opening a new issue](#opening-a-new-issue)  
5. [Bounty program](#bounty-program)
6. [License](#license)

## Overview
This repository contain main classes of the 
[Neo](https://www.neo.org) blockchain.   
Visit the [documentation](https://docs.neo.org/docs/en-us/index.html) to get started.


*Note: This is Neo 3 branch, currently under development. For the current stable version, please [click here](https://github.com/neo-project/neo/tree/master-2.x)*



## Project structure
An overview of the project folders can be seen below.

|Folder|Content|
|---|---|
|Consensus| Classes used in the dBFT consensus algorithm, including the `ConsensusService` actor.|
|Cryptography|General cryptography classes including ECC implementation.|
|IO|Data structures used for caching and collection interaction.|
|Ledger|Classes responsible for the state control, including the `MemoryPool` and `Blockchain` classes.|
|Network|Peer-to-peer protocol implementation classes.|
|Persistence|Classes used to allow other classes to access application state.|
|Plugins|Interfaces used to extend Neo, including the storage interface.|
|SmartContract|Native contracts, `ApplicationEngine`, `InteropService` and other smart-contract related classes.|
|VM|Helper methods used to interact with the VM.|
|Wallet|Wallet and account implementation. |


## Related projects
Code references are provided for all platform building blocks. That includes the base library, the VM, a command line application and the compiler. 

* [**neo:**](https://github.com/neo-project/neo/tree/) Neo core library, contains base classes, including ledger, p2p and IO modules.
* [neo-vm:](https://github.com/neo-project/neo-vm/) Neo Virtual Machine is a decoupled VM that Neo uses to execute its scripts. It also uses the `InteropService` layer to extend its functionalities.
* [neo-node:](https://github.com/neo-project/neo-node/) Executable version of the Neo library, exposing features using a command line application or GUI.
* [neo-modules:](https://github.com/neo-project/neo-modules/) Neo modules include additional tools and plugins to be used with Neo.
* [neo-devpack-dotnet:](https://github.com/neo-project/neo-devpack-dotnet/) These are the official tools used to convert a C# smart-contract into a *neo executable file*.

## Opening a new issue
Please feel free to create new issues to suggest features or ask questions.

- [Feature request](https://github.com/neo-project/neo/issues/new?assignees=&labels=discussion&template=feature-or-enhancement-request.md&title=)
- [Bug report](https://github.com/neo-project/neo/issues/new?assignees=&labels=&template=bug_report.md&title=)
- [Questions](https://github.com/neo-project/neo/issues/new?assignees=&labels=question&template=questions.md&title=)

If you found a security issue, please refer to our [security policy](https://github.com/neo-project/neo/security/policy).

## Bounty program
You can be rewarded by finding security issues. Please refer to our [bounty program page](https://neo.org/bounty) for more information.

## License
The NEO project is licensed under the [MIT license](LICENSE).
