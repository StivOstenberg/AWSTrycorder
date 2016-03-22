# AWSTrycorder
Going to "release" this version as it seems to work pretty reliably.  It has some issues where sometimes after a scan, the tables are returned empty.

This version is multithreaded, so it runs much faster than the prototype.

Also, set up a website at "trycorder.stiv.com" where I can post some documentation and a precompiled executable.

Some notes on Security:
  The Trycorder Scan Engine runs as a self hosted web application inside the Trycorder application.   It runs on the 127.0.0 network of your computer so the UI can talk to it.   At some point I will achieve a better understanding of the whole WCF hosted service, and I will decouple from the UI and allow remote access.  At this point, be aware that other processes running locally on your system might be able to access the service.   Not likely, but letting you know.
  
  There is also an issue where you may want to run the Trycorder once with elevated priveleges to allow it to create a source in the Event Log so it can log its own events.  I will need to catch that error or write an installer at some point,  but letting you know now.  Once the source is created, the Trycorder can run as a regular user.
  
  The source code is all posted, and contains no hidden loggers, and does not upload any information to any external sites.  I will add a PayPal donate button at some point,  but you have my word that no external communications not directly required to collect your Amazon data are contained in any of my code.
