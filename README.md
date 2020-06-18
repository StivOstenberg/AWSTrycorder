# AWSTrycorder
 I have not updated this for the new API components, so not sure what state it is in.  
 
 The AWS Trycorder uses the credentials defined for AWS on your system to scan all listed profiles and regions and collect information from all of them (or the ones you select) and put all that data into a single datatable for each component.  Allows you to list ALL your EC2 instances, for example, and search for any that have reboots scheduled.  Lets you identify which machine is using an IP Address.  Lets you list all your RDS instances, along with their endpoints. List all users on all your accounts and see when they last accessed the system, and which component they accessed.  Find out which IAM users have MFA set up...  List S3 buckets and their sizes.
 
    AWS Trycorder will output to Excel.
    
Current components scanned: EBS, EC2, IAM, S3, RDS, VPC, Snapshots, SNS Subscriptions, Subnets, and ELB.
    
Requires .NET Framework 4 or later.

Going to "release" this version as it seems to work pretty reliably.  It has some issues where sometimes after a scan, the tables are returned empty.

This version is multithreaded, so it runs much faster than the prototype.

Some notes on Security:
  The Trycorder Scan Engine runs as a self hosted web application inside the Trycorder application.   It runs on the 127.0.0 network of your computer so the UI can talk to it.   At some point I will achieve a better understanding of the whole WCF hosted service, and I will decouple from the UI and allow remote access.  At this point, be aware that other processes running locally on your system might be able to access the service.   Not likely, but letting you know.
  
  There is also an issue where you may want to run the Trycorder once with elevated priveleges to allow it to create a source in the Event Log so it can log its own events.  I will need to catch that error or write an installer at some point,  but letting you know now.  Once the source is created, the Trycorder can run as a regular user.
  
  The source code is all posted, and contains no hidden loggers, and does not upload any information to any external sites.  I will add a PayPal donate button at some point,  but you have my word that no external communications not directly required to collect your Amazon data are contained in any of my code.
