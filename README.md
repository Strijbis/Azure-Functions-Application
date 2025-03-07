# Azure Functions Application with Azure DevOps

This project was developed during a minor on cloud computing and development, specifically focused on Microsoft Azure and Azure DevOps. This application consists of two HttpTriggers and two QueueTriggers. The first HttpTrigger can be used by a client to request an ID, through several background jobs triggered by QueueTriggers an image (from the Art Institute of Chicago's API) and weather information (from Buienradar's API) is requested. Next the weather information is printed on an image, the result can then be requested by the client on the second HttpTrigger with their ID. In short, this assignment consists of:
* Interaction with two public APIs.
* Two HttpTriggers and two QueueTriggers.
* Continuous Integration and Continuous Deployment using Azure DevOps.

## Future developments

I intend to use this project to continue learning more about the Azure Cloud platform. This project is currently on hold, due to the expiration of my student subscription.