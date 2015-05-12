IpAddressGetTrayApplication

This little application is a tool to get your IP address (the global one, not on your local network). 
Once the address has been retrieved, a Google Drive document is updated, and if the IP has changed an email is sent to you.
The credentials are hard coded for now in the sense that you must rebuild the solution with your own credentials in a folder
named MyCredentials. Those credentials are in json format and are provided by the Google API on the Google Developper Console.
These are embedded resources for the project.

Improvement: Use Google Plus connection page the first time a user launches the application and store the credentials.

The application is registered to launch at windows startup the first time it is executed (in release mode).
There are two simple functions :
<ul>
<li>Send mail: sends an email with the current IP</li>
<li>Check the current IP: updates the current IP</li>
</ul>

Here is a screenshot :

<image alt="screenshot" src="images/layout".png/>
