import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

export function createHubConnection() {
  return new HubConnectionBuilder()
      .withUrl('https://cex-dex-parser-h7b3f0gwbyfah7ft.canadacentral-01.azurewebsites.net/hubs/alerts')
      //.withUrl('https://localhost:5258/hubs/alerts')
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(LogLevel.Warning)
    .build();
}
