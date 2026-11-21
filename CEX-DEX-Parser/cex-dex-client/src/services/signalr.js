import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

export function createHubConnection() {
  return new HubConnectionBuilder()
      .withUrl('https://cryptoscan-api.mangoriver-76c0c1d7.canadacentral.azurecontainerapps.io/hubs/alerts', {
          withCredentials: true
      })
      //.withUrl('https://localhost:5258/hubs/alerts', {
      //    withCredentials: true
      //})
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(LogLevel.Warning)
    .build();
}
