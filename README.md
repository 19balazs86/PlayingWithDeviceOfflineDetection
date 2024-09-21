# Playing with device offline detection using Durable Entities

- The idea of this concept came from [Device Offline detection with Durable Entities](https://dev.to/azure/device-offline-detection-with-durable-entities-e8g) by Kees Schollaart. I wanted to try out and learn from this
- I have adopted Azure Functions with Durable Entities in the style of the actor model using Orleans in [this repository](https://github.com/19balazs86/AspireOrleansDeviceOfflineDetection)

#### Useful links

- [Strongly typed SignalR ServerlessHub in Isolated worker model](https://learn.microsoft.com/en-us/azure/azure-signalr/signalr-concept-serverless-development-config?tabs=isolated-process) 📚*MS-Learn*
- [Azure Durable Entities: What are they good for?](https://markheath.net/post/durable-entities-what-are-they-good-for) 📓*Mark Heath*
- [Use Azure SignalR local emulator for serverless development](https://learn.microsoft.com/en-us/azure/azure-signalr/signalr-howto-emulator) 📚*MS-Learn*
  - [Trigger binding](https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-signalr-service-trigger)
  - [Configure upstream endpoints](https://learn.microsoft.com/en-us/azure/azure-signalr/concept-upstream)
  - [Sample chat application](https://github.com/aspnet/AzureSignalR-samples/tree/main/samples/BidirectionChat) 👤AspNet
