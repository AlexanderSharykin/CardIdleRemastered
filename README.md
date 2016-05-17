Card Idle Remastered
===========

Card Idle Remastered is a WPF remake of Idle Master, developed by jshackles (https://github.com/jshackles/idle_master). 

It offers rich UI and flexible idle management.

![](https://github.com/AlexanderSharykin/CardIdleRemastered/blob/master/Card_Idle_Main_Page.png)

Requirements
-------

OS Windows 7 and higher

.Net Framework 4.5

You should be logged into your Steam account via Steam client to start idling.

Card Idle works in a Single, Multi (Automatic) or Time Idle mode.

Time Idle Mode
-------

Open **Time Idle** tab

Click on Plus to add a new game to list

In the selection dialog enter game ID or store page url 

Confirm selection

Click Start button under game image

**Time Idle mode doesn't require login**

Card Idle saves games list before quit

Single Game Idle Mode
-------

Open **Badges** tab

Decide which game to idle and find it in the list using a grid scroll or a quick-search field

Click Start button

Wait until all cards drop and idle process stops

Automatic Game Idle Mode
-------

Open **Badges** Tab

Decide which games to idle and add them to the idle queue by clicking Enqueue button

Enqueue Selected button under the grid will add all games to the queue

Open **Queue** Tab

Change idle priority if necessary

Click Dequeue button to remove a game from the queue if necessary

Select Idle mode. Default mode is "One by One" which means that Card Idle runs one game idle process at a time and starts the next game only when all cards received. 

Other two modes ("Trial First" and "Trial Last") were introduces for games which have 2 hour delay before cards begin to drop. Card Idle will run such "trial" games together, stop those what reached 2 hour and pick next from the queue. Default number of processes is 16, max number is 255. When there is no more trial games Card Idle switches to "One by One" mode.

Launch automatic idle: click **[Start]** button

Click **[Stop]** button to interrupt automatic idle if necessary

**Card Idle stops all idle processes on quit or logout.**

**Card Idle saves idle queue before quit and clears it on logout**

License
-------

This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation.  A copy of the GNU General Public License can be found at http://www.gnu.org/licenses/. For your convenience, a copy of this license is included.