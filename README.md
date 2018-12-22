# Insight  
Insight is a simple Socket Server for Unity. It leverages Telepathy and Mirror to create a simple net connection that can run at the same time as a NetworkManager. It can also be its own standalone network connection. The original goal of this project was to create a secondary connection to allow a MasterServer. It can be used for that or many other things. The InsightServer can run on its own or with modules.

# Requirements:  
Mirror - Download from the Asset Store or Github  
Unity - Targeting 2017.4 currently. Plans to move to 2018 soon.  

You can run both the Client and Server from the same exe but the scope of this project and examples expect that you would have a seperate Client and Server. In most cases there would be multiple Servers providing different services. Due to this design decision there is no easy way to showcase the project without first making a standalone build. The included Build script should make this process painless.

# To Build:  
Download this project and open in Unity.  
Download Mirror from one of the two locations listed above.  
Run the example scenes in editor.  
Note: The Spawner example requires the use of the build menu (Under Tools at the top) to have a seperate running Standalone to spawn.  

# Examples:  
ChatModule - Sends messages to players anywhere in the game.  
LoginModule - Simple user/pass verification  
BasicSpawner - Creates GameServer with incrementing port numbers  
ComplexSpawner - Create and register GameServer with GameManagerModule (not implemented yet)  
Database - Working on support for MySQL and SQLite (not implemented yet)  

# License:  
Follow the included Mirror license. Otherwise Insight has no specific licensing of its own and can be used/copied anyway you see fit.
