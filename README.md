# M-USE

The Multitask Universal Suite for Experiments (M-USE) is a plugin for the Unity video game engine that
assists with the development and control of behavioral neuroscience experiments. As the name implies, it
has a particular focus on multiple tasks, in two ways: it supports running tasks in many different
experimental setups, and it supports running multiple tasks in a single experimental session. This makes it
a powerful tool for between-group and within-subject comparisons. 

See http://m-use.psy.vanderbilt.edu for full documentation, descriptions, and a web build.

<div style="background-color: #d4edda; padding: 10px; border: 1px solid #c3e6cb;">
    :warning: <strong>Important:</strong><br>
    The Suite is currently compatible with Unity Editor versions up to and including 2021.3.31f1.
</div>

<br>

![M-USE_Banner](https://github.com/Multitask-Unified-Suite-for-Expts/M-USE/assets/71558911/5332b4ac-97f7-4e73-a44f-d767461fc803)

## Main Components of an M-USE Build

M-USE builds consist of four main components
* Unity’s Native Processes
* Hierarchical Finite State Machine
* Task Library
* Modules

The state machine is the “engine” of the build, with almost all operations stemming
from methods called from one of its control levels. The top level of the state machine is the
session level, and anything an M-USE build does can ultimately be traced back to a
command called at the session level. Some of the most important of these commands active subordinate
control levels, in particular Task and Trial control levels that govern the
operation of particular experimental tasks, and are stored in the task library. When a particular
experimental task is started, its Task and Trial levels are added to the state machine, and when it is
finished, they are removed from the state machine, and remain inert in the Task Library, ready to load
again if needed.

The state machine is entirely encapsulated by the modules, such that all interactions with Unity’s native
processes, or other CPU processes (including ones that connect to outside hardware) are mediated
through these modules. For example, in order to detect a button press, the InputBroker module’s
GetButtonDown method mirrors the output of Unity’s native GetButtonDown method, which itself
interacts with other CPU processes to obtain the button’s status.

![image](https://github.com/Multitask-Unified-Suite-for-Expts/M-USE/assets/71558911/143f31aa-8fc0-4922-9642-855728649c7a)

## Installing M-USE

### Hardware Requirements

To ensure optimal performance and a seamless experience while running MUSE, its essential to meet or
exceed the following hardware specifications:
* Processor (CPU): A multi-core processor with a clock speed of at least 2.5 GHz or higher.
* Graphics Card (GPU): A dedicated graphics card with DirectX11 or OpenGL 4.5 support is required. For optimal performance with the latest graphics features, a GPU from NVIDIA or AMD with at least 2GB of VRAM is recommended.
* Memory (RAM): A minimum of 8GB of RAM is recommended for running MUSE.
* Storage: We recommend you have at least 20GB of free disc space available for installing MUSE.
  - MUSE Repository: 5.5 GB.
  - Configs and Resources folders: 252 MB.
  - MUSE Build: 302 MB.
  - Session Data: Can be several Gigabytes for full sessions with multiple tasks.
* Operating System: MUSE is compatible with Windows and MacOS.

### Unity

Install the Unity Hub and the Unity Editor (version 2021.3.31f1) at https://unity3d.com/getunity/download.

### Installing M-USE

1. Download the repository from https://github.com/Multitask-Unified-Suite-for-Expts/M-USE onto
your machine.
2. Open the Unity Hub, click the dropdown arrow on the top right and select **Add project from
disk**, then navigate to and select the repository folder you downloaded in step 1.
3. Install Newtonsoft_Json:
    - In the Unity editor, navigate to **Window** > **Package Manager** and **Add package from git URL** with the following URL: com.unity.nuget.newtonsoft-json@3.0 
