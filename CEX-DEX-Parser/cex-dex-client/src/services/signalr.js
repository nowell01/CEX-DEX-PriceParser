import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

export function createHubConnection() {
  return new HubConnectionBuilder()
      .withUrl('https://cex-dex-parser.azurewebsites.net/hubs/alerts')
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(LogLevel.Warning)
    .build();
}
